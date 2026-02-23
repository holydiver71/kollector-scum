/// <summary>
/// Entry point for the Kollector Scrum API application.
/// </summary>


using System.Text;
using KollectorScum.Api.Middleware;
using KollectorScum.Api.Data;
using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using KollectorScum.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;


// Load .env file from the root of the repo (one level up from backend/Api -> backend -> root)
// Assuming PWD is where the .sln or project usually is, or we find it relative to current dir.
var root = Directory.GetCurrentDirectory();
// Determine runtime environment (ASPNETCORE_ENVIRONMENT or DOTNET_ENVIRONMENT)
var runtimeEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ??
                 Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ??
                 "Production";

// Only load local .env files when NOT running in Production or Staging. This
// avoids accidentally overwriting platform-provided secrets (e.g., Render
// environment variables) with repository .env placeholders.
if (!string.Equals(runtimeEnv, "Production", StringComparison.OrdinalIgnoreCase) &&
    !string.Equals(runtimeEnv, "Staging", StringComparison.OrdinalIgnoreCase))
{
    var dotenv = Path.Combine(root, "../../.env");
    if (!File.Exists(dotenv))
    {
        // Try one level up if we are in backend/
        dotenv = Path.Combine(root, "../.env");
    }

    if (File.Exists(dotenv))
    {
        DotNetEnv.Env.Load(dotenv);
        Console.WriteLine($"Loaded environment variables from {dotenv}");
    }
    else
    {
        // try default loading which looks in current dir
        DotNetEnv.Env.Load();
    }
}
else
{
    Console.WriteLine($"Skipping .env load in {runtimeEnv} environment to preserve platform secrets.");
}


var builder = WebApplication.CreateBuilder(args);

// Many hosting platforms (e.g., Render) set a PORT environment variable that the app must bind to.
// Ensure Kestrel listens on 0.0.0.0:$PORT when provided.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port) && int.TryParse(port, out var parsedPort) && parsedPort > 0)
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{parsedPort}");
}

// Configure Kestrel for long-running operations (e.g., Discogs import)
builder.WebHost.ConfigureKestrel(options =>
{
    // Increase request body read timeout for large imports
    options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(30);
    // Keep connection alive during long operations
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(30);
});

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Use camelCase for JSON serialization to match frontend convention
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Add FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Add response caching for improved performance
builder.Services.AddResponseCaching(options =>
{
    options.MaximumBodySize = 1024 * 1024 * 10; // 10MB cache size
    options.UseCaseSensitivePaths = false;
});

builder.Services.AddHealthChecks();

// Configure CORS for frontend integration
// - Development: allow all origins if no explicit origins are configured
// - Staging/Production: require explicit allowed origins
var frontendOriginsRaw =
    builder.Configuration["Frontend:Origins"] ??
    builder.Configuration["Frontend:Origin"] ??
    builder.Configuration["FRONTEND_ORIGINS"] ??
    builder.Configuration["FRONTEND_ORIGIN"];

static string[] ParseCorsOrigins(string? raw)
{
    if (string.IsNullOrWhiteSpace(raw))
    {
        return Array.Empty<string>();
    }

    return raw
        .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();
}

var allowedFrontendOrigins = ParseCorsOrigins(frontendOriginsRaw);

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendCorsPolicy", policy =>
    {
        if (allowedFrontendOrigins.Length == 0)
        {
            if (!builder.Environment.IsProduction() && !builder.Environment.IsStaging())
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
                return;
            }

            throw new InvalidOperationException(
                "CORS is not configured. Set Frontend:Origin(s) (e.g. env var Frontend__Origin or Frontend__Origins) " +
                "to your frontend URL(s) for staging/production.");
        }

        policy.WithOrigins(allowedFrontendOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSettings["Key"];
if (!string.IsNullOrEmpty(jwtKey))
{
    // Warn if using placeholder key in production
    if ((builder.Environment.IsProduction() || builder.Environment.IsStaging()) &&
        (jwtKey.Contains("YourSecureKeyHere") || jwtKey.Contains("ChangeInProduction")))
    {
        throw new InvalidOperationException(
            "JWT Key must be changed from default value in production. " +
            "Set the Jwt:Key configuration to a secure random string of at least 32 characters.");
    }

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });
    
    builder.Services.AddAuthorization();
}

// Register IHttpClientFactory for outbound HTTP calls (e.g. Google token exchange)
builder.Services.AddHttpClient();

// Register HTTP context accessor for user context
builder.Services.AddHttpContextAccessor();

// Register user context service
builder.Services.AddScoped<IUserContext, UserContext>();

// Register storage service
var r2EndpointConfig = builder.Configuration["R2:Endpoint"] ?? builder.Configuration["R2__Endpoint"];
if (!string.IsNullOrWhiteSpace(r2EndpointConfig))
{
    // Use Cloudflare R2 (S3-compatible) when configured for staging/production
    builder.Services.AddScoped<IStorageService, CloudflareR2StorageService>();
}
else
{
    // Fallback to local filesystem for development and tests
    builder.Services.AddScoped<IStorageService, LocalFileSystemStorageService>();
}

// Register KollectorScumDbContext with PostgreSQL
// NOTE: Do not suppress EF warnings or apply migrations automatically in production.
// Migrations should be applied as part of deployment (CI/CD) using `dotnet ef database update`
if (!builder.Environment.IsEnvironment("Test"))
{
    builder.Services.AddDbContext<KollectorScumDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
}

// Register split seeding services (Phase 1.4 refactoring)
builder.Services.AddScoped<ILookupSeeder<Country, CountryJsonDto>, CountrySeeder>();
builder.Services.AddScoped<ILookupSeeder<Store, StoreJsonDto>, StoreSeeder>();
builder.Services.AddScoped<ILookupSeeder<Format, FormatJsonDto>, FormatSeeder>();
builder.Services.AddScoped<ILookupSeeder<Genre, GenreJsonDto>, GenreSeeder>();
builder.Services.AddScoped<ILookupSeeder<Label, LabelJsonDto>, LabelSeeder>();
builder.Services.AddScoped<ILookupSeeder<Artist, ArtistJsonDto>, ArtistSeeder>();
builder.Services.AddScoped<ILookupSeeder<Packaging, PackagingJsonDto>, PackagingSeeder>();
builder.Services.AddScoped<IDataSeedingOrchestrator, DataSeedingOrchestrator>();

// Keep old service temporarily for backward compatibility (will be removed after testing)
builder.Services.AddScoped<IDataSeedingService>(serviceProvider =>
{
    var context = serviceProvider.GetRequiredService<KollectorScumDbContext>();
    var logger = serviceProvider.GetRequiredService<ILogger<DataSeedingService>>();
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    return new DataSeedingService(context, logger, configuration);
});

// Register split import services (Phase 1.3 refactoring)
builder.Services.AddScoped<IJsonFileReader, JsonFileReader>();
builder.Services.AddScoped<IMusicReleaseBatchProcessor, MusicReleaseBatchProcessor>();
builder.Services.AddScoped<IMusicReleaseImportOrchestrator>(serviceProvider =>
{
    var fileReader = serviceProvider.GetRequiredService<IJsonFileReader>();
    var batchProcessor = serviceProvider.GetRequiredService<IMusicReleaseBatchProcessor>();
    var unitOfWork = serviceProvider.GetRequiredService<IUnitOfWork>();
    var logger = serviceProvider.GetRequiredService<ILogger<MusicReleaseImportOrchestrator>>();
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    return new MusicReleaseImportOrchestrator(fileReader, batchProcessor, unitOfWork, logger, configuration);
});

// Keep old service temporarily for compatibility (will be removed after testing)
builder.Services.AddScoped<IMusicReleaseImportService>(serviceProvider =>
{
    var unitOfWork = serviceProvider.GetRequiredService<IUnitOfWork>();
    var logger = serviceProvider.GetRequiredService<ILogger<MusicReleaseImportService>>();
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    return new MusicReleaseImportService(unitOfWork, logger, configuration);
});

// Register repository layer
builder.Services.AddScoped(typeof(IRepository<>), typeof(KollectorScum.Api.Repositories.Repository<>));
builder.Services.AddScoped<IUnitOfWork, KollectorScum.Api.Repositories.UnitOfWork>();

// Register authentication repositories and services
builder.Services.AddScoped<IUserRepository, KollectorScum.Api.Repositories.UserRepository>();
builder.Services.AddScoped<IUserProfileRepository, KollectorScum.Api.Repositories.UserProfileRepository>();
builder.Services.AddScoped<IUserInvitationRepository, KollectorScum.Api.Repositories.UserInvitationRepository>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();

// Register generic CRUD services
builder.Services.AddScoped<IGenericCrudService<KollectorScum.Api.Models.Artist, KollectorScum.Api.DTOs.ArtistDto>, ArtistService>();
builder.Services.AddScoped<IGenericCrudService<KollectorScum.Api.Models.Genre, KollectorScum.Api.DTOs.GenreDto>, GenreService>();
builder.Services.AddScoped<IGenericCrudService<KollectorScum.Api.Models.Label, KollectorScum.Api.DTOs.LabelDto>, LabelService>();
builder.Services.AddScoped<IGenericCrudService<KollectorScum.Api.Models.Country, KollectorScum.Api.DTOs.CountryDto>, CountryService>();
builder.Services.AddScoped<IGenericCrudService<KollectorScum.Api.Models.Format, KollectorScum.Api.DTOs.FormatDto>, FormatService>();
builder.Services.AddScoped<IGenericCrudService<KollectorScum.Api.Models.Packaging, KollectorScum.Api.DTOs.PackagingDto>, PackagingService>();
builder.Services.AddScoped<IGenericCrudService<KollectorScum.Api.Models.Store, KollectorScum.Api.DTOs.StoreDto>, StoreService>();
builder.Services.AddScoped<IKollectionService, KollectionService>();
builder.Services.AddScoped<IListService, ListService>();

// Register business logic services
builder.Services.AddScoped<IEntityResolverService, EntityResolverService>();
builder.Services.AddScoped<IMusicReleaseMapperService, MusicReleaseMapperService>();
builder.Services.AddScoped<ICollectionStatisticsService, CollectionStatisticsService>();
builder.Services.AddScoped<IMusicReleaseSearchService, MusicReleaseSearchService>();
builder.Services.AddScoped<IMusicReleaseDuplicateService, MusicReleaseDuplicateService>();

// Register split music release services (Phase 1.2 refactoring)
builder.Services.AddScoped<IMusicReleaseDuplicateDetector, MusicReleaseDuplicateDetector>();
builder.Services.AddScoped<IMusicReleaseValidator, MusicReleaseValidator>();
builder.Services.AddScoped<IMusicReleaseQueryService, MusicReleaseQueryService>();
builder.Services.AddScoped<IMusicReleaseCommandService, MusicReleaseCommandService>();

// Keep old service temporarily for compatibility (will be removed after test migration)
builder.Services.AddScoped<IMusicReleaseService, MusicReleaseService>();

// Register Discogs services (Phase 1.5 refactoring)
builder.Services.Configure<DiscogsSettings>(builder.Configuration.GetSection("Discogs"));
builder.Services.AddHttpClient<IDiscogsHttpClient, DiscogsHttpClient>();
builder.Services.AddScoped<IDiscogsResponseMapper, DiscogsResponseMapper>();
builder.Services.AddScoped<IDiscogsService, DiscogsService>();
builder.Services.AddHttpClient<DiscogsCollectionImportService>();
builder.Services.AddScoped<IDiscogsCollectionImportService, DiscogsCollectionImportService>();

// Register Natural Language Query services
builder.Services.AddSingleton<IDatabaseSchemaService, DatabaseSchemaService>();
builder.Services.AddScoped<ISqlValidationService, SqlValidationService>();
builder.Services.AddScoped<IQueryLLMService, NaturalLanguageQueryService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Kollector Scum API",
        Version = "v1",
        Description = "API for managing music collection data"
    });
});

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Reduce noisy EF Core SQL command logs (they run at Information level by default)
// Keep warnings/errors visible but suppress routine executed command output.
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", Microsoft.Extensions.Logging.LogLevel.Warning);

var app = builder.Build();

// Add global error handling middleware
app.UseMiddleware<ErrorHandlingMiddleware>();

// IMPORTANT: Do not apply migrations automatically at startup in production.
// Applying migrations at runtime can lead to unexpected schema changes and
// startup failures. Apply migrations explicitly during deployment using
// `dotnet ef database update` or equivalent migration scripts.

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Kollector Scum API v1");
    });
}

// Add CORS for frontend integration
app.UseCors("FrontendCorsPolicy");

// Enable response caching
app.UseResponseCaching();

// Enable static files for serving cover art
app.UseStaticFiles();

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Add middleware to validate users still exist (after authentication, before authorization)
app.UseValidateUser();

// Map controllers
app.MapControllers();

// Runtime info (used by frontend footer indicator)
app.MapGet("/runtime-info", (IConfiguration configuration, IHostEnvironment environment) =>
{
    var configuredTarget = configuration["Database:Target"] ?? configuration["Database__Target"]; // allow both forms
    var normalizedTarget = NormalizeDatabaseTarget(configuredTarget);
    if (normalizedTarget is null)
    {
        normalizedTarget = InferDatabaseTarget(configuration, environment);
    }

    return Results.Ok(new
    {
        environment = environment.EnvironmentName,
        databaseTarget = normalizedTarget
    });
});

static string? NormalizeDatabaseTarget(string? raw)
{
    if (string.IsNullOrWhiteSpace(raw))
    {
        return null;
    }

    raw = raw.Trim();
    return raw.ToLowerInvariant() switch
    {
        "local" => "local",
        "dev" => "local",
        "development" => "local",
        "staging" => "staging",
        "stage" => "staging",
        "prod" => "production",
        "production" => "production",
        _ => null
    };
}

static string InferDatabaseTarget(IConfiguration configuration, IHostEnvironment environment)
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");

    if (!string.IsNullOrWhiteSpace(connectionString))
    {
        try
        {
            var builder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
            var host = builder.Host?.Trim();
            if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(host, "::1", StringComparison.OrdinalIgnoreCase))
            {
                return "local";
            }
        }
        catch
        {
            // Best-effort only.
        }
    }

    if (environment.IsStaging())
    {
        return "staging";
    }

    if (environment.IsProduction())
    {
        return "production";
    }

    return "unknown";
}

// Map health checks
app.MapHealthChecks("/health");

app.Run();

// Make Program class accessible for integration testing
public partial class Program { }
