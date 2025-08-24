/// <summary>
/// Entry point for the Kollector Scrum API application.
/// </summary>


using KollectorScrum.Api.Middleware;
using KollectorScrum.Api.Data;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddHealthChecks();

// Register KollectorScrumDbContext with PostgreSQL
builder.Services.AddDbContext<KollectorScrumDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() 
    { 
        Title = "Kollector Scrum API", 
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Kollector Scrum API v1");
    });
}

// Add CORS for frontend integration
app.UseCors(policy =>
{
    policy.AllowAnyOrigin()
          .AllowAnyMethod()
          .AllowAnyHeader();
});

// Map controllers
app.MapControllers();

// Map health checks
app.MapHealthChecks("/health");

app.Run();
