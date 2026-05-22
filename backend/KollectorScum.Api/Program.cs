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

// ── Startup diagnostics ───────────────────────────────────────────────────────

app.Logger.LogInformation("STARTUP: Environment={Env}", app.Environment.EnvironmentName);

// JWT — misconfig here silently disables auth (all [Authorize] endpoints → 401)
var jwtKey      = app.Configuration["Jwt:Key"]      ?? app.Configuration["Jwt__Key"];
var jwtIssuer   = app.Configuration["Jwt:Issuer"]   ?? app.Configuration["Jwt__Issuer"]   ?? "(appsettings default)";
var jwtAudience = app.Configuration["Jwt:Audience"]  ?? app.Configuration["Jwt__Audience"] ?? "(appsettings default)";

if (string.IsNullOrEmpty(jwtKey))
    app.Logger.LogError("STARTUP: Jwt:Key is EMPTY — JWT auth middleware is DISABLED. Every [Authorize] endpoint will return 401.");
else
    app.Logger.LogInformation("STARTUP: Jwt:Key loaded (length={Len}).", jwtKey.Length);

app.Logger.LogInformation("STARTUP: Jwt:Issuer={Issuer}  Jwt:Audience={Audience}", jwtIssuer, jwtAudience);

// Google OAuth — misconfig here breaks the sign-in redirect flow
var googleClientId     = app.Configuration["Google:ClientId"]     ?? app.Configuration["Google__ClientId"];
var googleClientSecret = app.Configuration["Google:ClientSecret"] ?? app.Configuration["Google__ClientSecret"];
var googleRedirectUri  = app.Configuration["Google:RedirectUri"]  ?? app.Configuration["Google__RedirectUri"];

if (string.IsNullOrEmpty(googleClientId))
    app.Logger.LogError("STARTUP: Google:ClientId is EMPTY — Google OAuth will not work.");
else
    app.Logger.LogInformation("STARTUP: Google:ClientId loaded (length={Len}).", googleClientId.Length);

if (string.IsNullOrEmpty(googleClientSecret))
    app.Logger.LogError("STARTUP: Google:ClientSecret is EMPTY — authorization code exchange will fail (error_auth_failed).");
else
    app.Logger.LogInformation("STARTUP: Google:ClientSecret loaded (length={Len}).", googleClientSecret.Length);

if (string.IsNullOrEmpty(googleRedirectUri))
    app.Logger.LogError("STARTUP: Google:RedirectUri is EMPTY — backend will send a blank redirect_uri to Google.");
else
    app.Logger.LogInformation("STARTUP: Google:RedirectUri={RedirectUri}", googleRedirectUri);

// Frontend origins — misconfig here sends the post-auth JWT to the wrong URL
var frontendOrigins =
    app.Configuration["Frontend:Origins"] ??
    app.Configuration["Frontend:Origin"]  ??
    app.Configuration["FRONTEND_ORIGINS"] ??
    app.Configuration["FRONTEND_ORIGIN"];

if (string.IsNullOrEmpty(frontendOrigins))
    app.Logger.LogError("STARTUP: Frontend origins not set — post-auth redirect will go to http://localhost:3000 (wrong host).");
else
    app.Logger.LogInformation("STARTUP: Frontend origins={Origins}", frontendOrigins);

// Discogs
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
