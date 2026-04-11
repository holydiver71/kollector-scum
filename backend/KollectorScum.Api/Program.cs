/// <summary>
/// Entry point for the Kollector Scum API application.
/// </summary>

using KollectorScum.Api.Extensions;

// ── Environment setup ─────────────────────────────────────────────────────────

var root = Directory.GetCurrentDirectory();
var runtimeEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ??
                 Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ??
                 "Production";

// Only load local .env files outside Production/Staging to avoid overwriting
// platform-provided secrets (e.g. Render environment variables).
if (!string.Equals(runtimeEnv, "Production", StringComparison.OrdinalIgnoreCase) &&
    !string.Equals(runtimeEnv, "Staging", StringComparison.OrdinalIgnoreCase))
{
    var dotenv = Path.Combine(root, "../../.env");
    if (!File.Exists(dotenv)) dotenv = Path.Combine(root, "../.env");

    if (File.Exists(dotenv))
    {
        DotNetEnv.Env.Load(dotenv);
        Console.WriteLine($"Loaded environment variables from {dotenv}");
    }
    else
    {
        DotNetEnv.Env.Load();
    }
}
else
{
    Console.WriteLine($"Skipping .env load in {runtimeEnv} environment to preserve platform secrets.");
}

// ── Host & Kestrel ────────────────────────────────────────────────────────────

var builder = WebApplication.CreateBuilder(args);

// Many hosting platforms (e.g. Render) set PORT; bind to it when present.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port) && int.TryParse(port, out var parsedPort) && parsedPort > 0)
    builder.WebHost.UseUrls($"http://0.0.0.0:{parsedPort}");

// Increase timeouts for long-running operations (e.g. Discogs import).
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(30);
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(30);
});

// ── Logging ───────────────────────────────────────────────────────────────────

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddFilter(
    "Microsoft.EntityFrameworkCore.Database.Command",
    Microsoft.Extensions.Logging.LogLevel.Warning);

// ── Service registration ──────────────────────────────────────────────────────

builder.Services
    .AddCorePipeline()
    .AddCachingServices()
    .AddRateLimiting()
    .AddCorsPolicy(builder.Configuration, builder.Environment)
    .AddJwtAuthentication(builder.Configuration, builder.Environment)
    .AddDatabaseServices(builder.Configuration, builder.Environment)
    .AddRepositories()
    .AddAuthServices()
    .AddLookupCrudServices()
    .AddMusicReleaseServices()
    .AddDiscogsServices(builder.Configuration)
    .AddImageServices()
    .AddNaturalLanguageQueryServices()
    .AddDataSeedingServices()
    .AddImportServices()
    .AddInfrastructureServices(builder.Configuration);

// ── Middleware pipeline ───────────────────────────────────────────────────────

var app = builder.Build();

// Startup diagnostic: confirm Discogs token is loaded
var discogsToken = app.Configuration["Discogs:Token"] ?? app.Configuration["Discogs__Token"];
if (string.IsNullOrEmpty(discogsToken))
    app.Logger.LogWarning("STARTUP: Discogs:Token is EMPTY — collection import will return 403.");
else
    app.Logger.LogInformation("STARTUP: Discogs:Token loaded (length={Len}).", discogsToken.Length);

app.UseKollectorApiPipeline();

// ── Endpoints ─────────────────────────────────────────────────────────────────

app.MapControllers();

app.MapGet("/runtime-info", (IConfiguration configuration, IHostEnvironment environment) =>
{
    var configuredTarget =
        configuration["Database:Target"] ?? configuration["Database__Target"];
    var normalizedTarget = NormalizeDatabaseTarget(configuredTarget)
        ?? InferDatabaseTarget(configuration, environment);

    return Results.Ok(new
    {
        environment = environment.EnvironmentName,
        databaseTarget = normalizedTarget,
    });
});

app.MapHealthChecks("/health");

app.Run();

// ── Local helpers (minimal surface; keep Program.cs thin) ─────────────────────

static string? NormalizeDatabaseTarget(string? raw)
{
    if (string.IsNullOrWhiteSpace(raw)) return null;
    return raw.Trim().ToLowerInvariant() switch
    {
        "local" or "dev" or "development" => "local",
        "staging" or "stage"              => "staging",
        "prod" or "production"            => "production",
        _                                 => null,
    };
}

static string InferDatabaseTarget(IConfiguration configuration, IHostEnvironment environment)
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrWhiteSpace(connectionString))
    {
        try
        {
            var csb = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
            var host = csb.Host?.Trim();
            if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(host, "::1", StringComparison.OrdinalIgnoreCase))
                return "local";
        }
        catch
        {
            // Best-effort only.
        }
    }

    if (environment.IsStaging())   return "staging";
    if (environment.IsProduction()) return "production";
    return "unknown";
}

// Make Program class accessible for integration testing.
public partial class Program { }
