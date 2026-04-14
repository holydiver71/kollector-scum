using System.Text;
using System.Threading.RateLimiting;
using FluentValidation;
using FluentValidation.AspNetCore;
using KollectorScum.Api.Application.Queries;
using KollectorScum.Api.Controllers;
using KollectorScum.Api.Data;
using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using KollectorScum.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace KollectorScum.Api.Extensions;

/// <summary>
/// Extension methods that modularise service registration in Program.cs.
/// Each method groups a single domain or infrastructure concern.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers controllers, FluentValidation, JSON serialisation, response
    /// caching, and response compression.
    /// </summary>
    public static IServiceCollection AddCorePipeline(this IServiceCollection services)
    {
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy =
                    System.Text.Json.JsonNamingPolicy.CamelCase;
            });

        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssemblyContaining<Program>();

        services.AddResponseCaching(options =>
        {
            options.MaximumBodySize = 1024 * 1024 * 10; // 10 MB
            options.UseCaseSensitivePaths = false;
        });

        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
            options.MimeTypes = ResponseCompressionDefaults.MimeTypes
                .Concat(new[] { "application/json" })
                .Distinct(StringComparer.OrdinalIgnoreCase);
        });

        services.Configure<BrotliCompressionProviderOptions>(o =>
            o.Level = System.IO.Compression.CompressionLevel.Fastest);
        services.Configure<GzipCompressionProviderOptions>(o =>
            o.Level = System.IO.Compression.CompressionLevel.Fastest);

        return services;
    }

    /// <summary>
    /// Registers in-memory caching services used for lookup data.
    /// </summary>
    public static IServiceCollection AddCachingServices(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddSingleton<ICacheService, MemoryCacheService>();
        return services;
    }

    /// <summary>
    /// Registers rate-limiting policies: a global fixed-window limiter and a
    /// stricter named "auth" policy for authentication endpoints.
    /// </summary>
    public static IServiceCollection AddRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var path = context.Request.Path.Value ?? string.Empty;
                var isImageRequest =
                    path.StartsWith("/api/images", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("/cover-art", StringComparison.OrdinalIgnoreCase);

                return RateLimitPartition.GetFixedWindowLimiter(remoteIp, _ => new FixedWindowRateLimiterOptions
                {
                    Window = TimeSpan.FromMinutes(1),
                    PermitLimit = isImageRequest ? 500 : 300,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0,
                });
            });

            options.AddFixedWindowLimiter("auth", limiterOptions =>
            {
                limiterOptions.Window = TimeSpan.FromMinutes(1);
                limiterOptions.PermitLimit = 10;
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = 0;
            });

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.Headers["Retry-After"] = "60";
                await Task.CompletedTask;
            };
        });

        return services;
    }

    /// <summary>
    /// Configures CORS policy for frontend origins.
    /// Allows any origin in non-production environments when no origins are
    /// configured; throws in production if origins are missing.
    /// </summary>
    public static IServiceCollection AddCorsPolicy(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var frontendOriginsRaw =
            configuration["Frontend:Origins"] ??
            configuration["Frontend:Origin"] ??
            configuration["FRONTEND_ORIGINS"] ??
            configuration["FRONTEND_ORIGIN"];

        var allowedOrigins = ParseCorsOrigins(frontendOriginsRaw);

        services.AddCors(options =>
        {
            options.AddPolicy("FrontendCorsPolicy", policy =>
            {
                if (allowedOrigins.Length == 0)
                {
                    if (!environment.IsProduction() && !environment.IsStaging())
                    {
                        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
                        return;
                    }

                    throw new InvalidOperationException(
                        "CORS is not configured. Set Frontend:Origin(s) to your frontend URL(s) " +
                        "for staging/production.");
                }

                policy.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader();
            });
        });

        return services;
    }

    /// <summary>
    /// Configures JWT bearer authentication and authorization.
    /// Validates the key is not a placeholder in production/staging.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var jwtSettings = configuration.GetSection("Jwt");
        var jwtKey = jwtSettings["Key"];

        if (string.IsNullOrEmpty(jwtKey)) return services;

        if ((environment.IsProduction() || environment.IsStaging()) &&
            (jwtKey.Contains("YourSecureKeyHere") || jwtKey.Contains("ChangeInProduction")))
        {
            throw new InvalidOperationException(
                "JWT Key must be changed from default value in production. " +
                "Set Jwt:Key to a secure random string of at least 32 characters.");
        }

        services
            .AddAuthentication(options =>
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
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtKey)),
                };
            });

        services.AddAuthorization();

        return services;
    }

    /// <summary>
    /// Registers infrastructure services: HTTP client factory, HTTP context
    /// accessor, user context, storage service, health checks, Swagger, and
    /// structured logging filters.
    /// </summary>
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpClient();
        services.AddHttpContextAccessor();
        services.AddScoped<IUserContext, UserContext>();

        // Storage: Cloudflare R2 when configured, local filesystem otherwise.
        var r2Endpoint =
            configuration["R2:Endpoint"] ?? configuration["R2__Endpoint"];
        if (!string.IsNullOrWhiteSpace(r2Endpoint))
            services.AddScoped<IStorageService, CloudflareR2StorageService>();
        else
            services.AddScoped<IStorageService, LocalFileSystemStorageService>();

        services.AddHealthChecks().AddDbContextCheck<KollectorScumDbContext>("database");

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new()
            {
                Title = "Kollector Scum API",
                Version = "v1",
                Description = "API for managing music collection data",
            });
        });

        return services;
    }

    /// <summary>
    /// Registers the EF Core DbContext with PostgreSQL.
    /// Skipped in the "Test" environment (replaced by the test fixture).
    /// </summary>
    public static IServiceCollection AddDatabaseServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        if (!environment.IsEnvironment("Test"))
        {
            services.AddDbContext<KollectorScumDbContext>(options =>
                options.UseNpgsql(
                    configuration.GetConnectionString("DefaultConnection")));
        }

        return services;
    }

    /// <summary>
    /// Registers generic and domain-specific repository implementations.
    /// </summary>
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped(
            typeof(IRepository<>),
            typeof(KollectorScum.Api.Repositories.Repository<>));
        services.AddScoped<IUnitOfWork, KollectorScum.Api.Repositories.UnitOfWork>();

        services.AddScoped<IMusicReleaseRepository, KollectorScum.Api.Repositories.MusicReleaseRepository>();
        services.AddScoped<IUserRepository, KollectorScum.Api.Repositories.UserRepository>();
        services.AddScoped<IUserProfileRepository, KollectorScum.Api.Repositories.UserProfileRepository>();
        services.AddScoped<IUserInvitationRepository, KollectorScum.Api.Repositories.UserInvitationRepository>();
        services.AddScoped<IMagicLinkTokenRepository, KollectorScum.Api.Repositories.MagicLinkTokenRepository>();

        return services;
    }

    /// <summary>
    /// Registers authentication and user-management services (token, Google
    /// OAuth, email, magic link).
    /// </summary>
    public static IServiceCollection AddAuthServices(this IServiceCollection services)
    {
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddScoped<IMagicLinkService, MagicLinkService>();
        services.AddScoped<IUserAuthenticationService, UserAuthenticationService>();
        services.AddScoped<IUserImpersonationService, UserImpersonationService>();

        return services;
    }

    /// <summary>
    /// Registers the seven per-user lookup CRUD services (Artist, Genre, Label,
    /// Country, Format, Packaging, Store) and collection/list services.
    /// </summary>
    public static IServiceCollection AddLookupCrudServices(this IServiceCollection services)
    {
        services.AddScoped<IGenericCrudService<Artist, ArtistDto>, ArtistService>();
        services.AddScoped<IGenericCrudService<Genre, GenreDto>, GenreService>();
        services.AddScoped<IGenericCrudService<Label, LabelDto>, LabelService>();
        services.AddScoped<IGenericCrudService<Country, CountryDto>, CountryService>();
        services.AddScoped<IGenericCrudService<Format, FormatDto>, FormatService>();
        services.AddScoped<IGenericCrudService<Packaging, PackagingDto>, PackagingService>();
        services.AddScoped<IGenericCrudService<Store, StoreDto>, StoreService>();
        services.AddScoped<IKollectionService, KollectionService>();
        services.AddScoped<IListService, ListService>();

        return services;
    }

    /// <summary>
    /// Registers music-release business logic and query/command services.
    /// </summary>
    public static IServiceCollection AddMusicReleaseServices(this IServiceCollection services)
    {
        services.AddScoped<IEntityResolverService, EntityResolverService>();
        services.AddScoped<IMusicReleaseMapperService, MusicReleaseMapperService>();
        services.AddScoped<ICollectionStatisticsService, CollectionStatisticsService>();
        services.AddScoped<IMusicReleaseSearchService, MusicReleaseSearchService>();
        services.AddScoped<IMusicReleaseDuplicateService, MusicReleaseDuplicateService>();

        services.AddScoped<IMusicReleaseDuplicateDetector, MusicReleaseDuplicateDetector>();
        services.AddScoped<IMusicReleaseValidator, MusicReleaseValidator>();
        services.AddScoped<IGetMusicReleasesQueryHandler, GetMusicReleasesQueryHandler>();
        services.AddScoped<IMusicReleaseQueryService, MusicReleaseQueryService>();
        services.AddScoped<IMusicReleaseCommandService, MusicReleaseCommandService>();

        return services;
    }

    /// <summary>
    /// Registers the Discogs integration: settings, HTTP clients, mapper,
    /// async import background service, and collection import service.
    /// </summary>
    public static IServiceCollection AddDiscogsServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<DiscogsSettings>(
            configuration.GetSection("Discogs"));
        services.AddHttpClient<IDiscogsHttpClient, DiscogsHttpClient>();
        services.AddScoped<IDiscogsResponseMapper, DiscogsResponseMapper>();
        services.AddScoped<IDiscogsService, DiscogsService>();
        services.AddHttpClient<DiscogsImageService>();
        services.AddScoped<IDiscogsImageService, DiscogsImageService>();
        services.AddSingleton<IDiscogsImportJobQueue, DiscogsImportJobQueue>();
        services.AddHostedService<DiscogsImportBackgroundService>();
        services.AddScoped<IDiscogsImportJobService, DiscogsImportJobService>();
        services.AddScoped<IDiscogsCollectionImportService, DiscogsCollectionImportService>();
        services.AddScoped<IStorageMigrationService, StorageMigrationService>();

        return services;
    }

    /// <summary>
    /// Registers image resizing, cover art search, and the named HTTP clients
    /// for MusicBrainz, Cover Art Archive, and image downloads.
    /// </summary>
    public static IServiceCollection AddImageServices(this IServiceCollection services)
    {
        services.AddScoped<IImageResizerService, ImageResizerService>();
        services.AddScoped<ICoverArtSearchService, CoverArtSearchService>();

        services.AddHttpClient(
            CoverArtSearchService.MusicBrainzClientName, client =>
            {
                client.BaseAddress = new Uri("https://musicbrainz.org/ws/2/");
                client.DefaultRequestHeaders.Add(
                    "User-Agent",
                    "KollectorScum/1.0 (https://github.com/holydiver71/kollector-scum; support@kollector.app)");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.Timeout = TimeSpan.FromSeconds(10);
            });

        services.AddHttpClient(
            CoverArtSearchService.CoverArtArchiveClientName, client =>
            {
                client.BaseAddress = new Uri("https://coverartarchive.org/");
                client.DefaultRequestHeaders.Add(
                    "User-Agent",
                    "KollectorScum/1.0 (https://github.com/holydiver71/kollector-scum; support@kollector.app)");
                client.Timeout = TimeSpan.FromSeconds(10);
            });

        services.AddHttpClient(
            ImagesController.ImageDownloadClientName, client =>
            {
                client.DefaultRequestHeaders.Add("User-Agent", "KollectorScum/1.0");
                client.Timeout = TimeSpan.FromSeconds(30);
            });

        return services;
    }

    /// <summary>
    /// Registers the natural language query services (schema, SQL validation,
    /// LLM query).
    /// </summary>
    public static IServiceCollection AddNaturalLanguageQueryServices(
        this IServiceCollection services)
    {
        services.AddSingleton<IDatabaseSchemaService, DatabaseSchemaService>();
        services.AddScoped<ISqlValidationService, SqlValidationService>();
        services.AddScoped<IQueryLLMService, NaturalLanguageQueryService>();

        return services;
    }

    /// <summary>
    /// Registers data-seeding orchestrator and per-entity seeders.
    /// Includes a legacy compatibility registration pending removal.
    /// </summary>
    public static IServiceCollection AddDataSeedingServices(
        this IServiceCollection services)
    {
        services.AddScoped<ILookupSeeder<Country, CountryJsonDto>, CountrySeeder>();
        services.AddScoped<ILookupSeeder<Store, StoreJsonDto>, StoreSeeder>();
        services.AddScoped<ILookupSeeder<Format, FormatJsonDto>, FormatSeeder>();
        services.AddScoped<ILookupSeeder<Genre, GenreJsonDto>, GenreSeeder>();
        services.AddScoped<ILookupSeeder<Label, LabelJsonDto>, LabelSeeder>();
        services.AddScoped<ILookupSeeder<Artist, ArtistJsonDto>, ArtistSeeder>();
        services.AddScoped<ILookupSeeder<Packaging, PackagingJsonDto>, PackagingSeeder>();
        services.AddScoped<IDataSeedingOrchestrator, DataSeedingOrchestrator>();

        // Legacy compatibility — remove after test migration.
        services.AddScoped<IDataSeedingService>(sp =>
        {
            var ctx = sp.GetRequiredService<KollectorScumDbContext>();
            var logger = sp.GetRequiredService<ILogger<DataSeedingService>>();
            var cfg = sp.GetRequiredService<IConfiguration>();
            return new DataSeedingService(ctx, logger, cfg);
        });

        return services;
    }

    /// <summary>
    /// Registers import pipeline services (JSON reader, batch processor,
    /// import orchestrator).  Includes legacy compatibility registration.
    /// </summary>
    public static IServiceCollection AddImportServices(this IServiceCollection services)
    {
        services.AddScoped<IJsonFileReader, JsonFileReader>();
        services.AddScoped<IMusicReleaseBatchProcessor, MusicReleaseBatchProcessor>();
        services.AddScoped<IMusicReleaseImportOrchestrator>(sp =>
        {
            var reader = sp.GetRequiredService<IJsonFileReader>();
            var processor = sp.GetRequiredService<IMusicReleaseBatchProcessor>();
            var uow = sp.GetRequiredService<IUnitOfWork>();
            var logger = sp.GetRequiredService<ILogger<MusicReleaseImportOrchestrator>>();
            var cfg = sp.GetRequiredService<IConfiguration>();
            return new MusicReleaseImportOrchestrator(reader, processor, uow, logger, cfg);
        });

        // Legacy compatibility — remove after test migration.
        services.AddScoped<IMusicReleaseImportService>(sp =>
        {
            var uow = sp.GetRequiredService<IUnitOfWork>();
            var logger = sp.GetRequiredService<ILogger<MusicReleaseImportService>>();
            var cfg = sp.GetRequiredService<IConfiguration>();
            return new MusicReleaseImportService(uow, logger, cfg);
        });

        return services;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static string[] ParseCorsOrigins(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return Array.Empty<string>();

        return raw
            .Split(new[] { ',', ';', ' ' },
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
