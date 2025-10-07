using System.Text.Json;
using KollectorScum.Api.Data;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KollectorScum.DataSeeder
{
    /// <summary>
    /// Console application for seeding the database with lookup data
    /// </summary>
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Kollector Scum Data Seeder");
            Console.WriteLine("=========================");

            var host = CreateHostBuilder(args).Build();
            
            using var scope = host.Services.CreateScope();
            var seedingService = scope.ServiceProvider.GetRequiredService<IDataSeedingService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            try
            {
                logger.LogInformation("Starting data seeding process...");
                
                await seedingService.SeedLookupDataAsync();
                
                logger.LogInformation("Data seeding completed successfully!");
                Console.WriteLine("\n✅ Data seeding completed successfully!");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred during data seeding");
                Console.WriteLine($"\n❌ Error: {ex.Message}");
                Environment.Exit(1);
            }
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                })
                .ConfigureServices((context, services) =>
                {
                    // Add Entity Framework
                    services.AddDbContext<KollectorScumDbContext>(options =>
                        options.UseNpgsql(context.Configuration.GetConnectionString("DefaultConnection")));

                    // Add services
                    services.AddScoped<IDataSeedingService, DataSeedingService>();
                    
                    // Add logging
                    services.AddLogging(builder =>
                    {
                        builder.AddConsole();
                        builder.SetMinimumLevel(LogLevel.Information);
                    });
                });
    }
}
