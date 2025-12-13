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


var builder = WebApplication.CreateBuilder(args);


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

builder.Services.AddHealthChecks();

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSettings["Key"];
if (!string.IsNullOrEmpty(jwtKey))
{
    // Warn if using placeholder key in production
    if (!builder.Environment.IsDevelopment() && 
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

// Register KollectorScumDbContext with PostgreSQL
// NOTE: Do not suppress EF warnings or apply migrations automatically in production.
// Migrations should be applied as part of deployment (CI/CD) using `dotnet ef database update`
builder.Services.AddDbContext<KollectorScumDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IUserRepository, KollectorScum.Api.Repositories.UserRepository>();
builder.Services.AddScoped<IUserProfileRepository, KollectorScum.Api.Repositories.UserProfileRepository>();
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
app.UseCors(policy =>
{
    policy.AllowAnyOrigin()
          .AllowAnyMethod()
          .AllowAnyHeader();
});

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

// Map health checks
app.MapHealthChecks("/health");

app.Run();

// Make Program class accessible for integration testing
public partial class Program { }
