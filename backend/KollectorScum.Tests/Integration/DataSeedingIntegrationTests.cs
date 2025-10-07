using KollectorScum.Api.Services;
using KollectorScum.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;
using System.Text.Json;

namespace KollectorScum.Tests.Integration
{
    public class DataSeedingIntegrationTests
    {
        [Fact]
        public async Task SeedCountries_ShouldImportActualJsonData()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<KollectorScumDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    {"DataPath", "../../../../../data"} // Adjust path from test project to data folder
                })
                .Build();

            var logger = new LoggerFactory().CreateLogger<DataSeedingService>();

            using var context = new KollectorScumDbContext(options);
            var service = new DataSeedingService(context, logger, configuration);

            // Act
            await service.SeedCountriesAsync();

            // Assert
            var countries = await context.Countries.ToListAsync();
            Assert.True(countries.Count > 0, "Should import at least one country");
            Assert.Contains(countries, c => c.Name == "Austria"); // Based on the JSON data we saw
        }

        [Fact]
        public async Task SeedAllLookupData_ShouldImportAllJsonFiles()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<KollectorScumDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    {"DataPath", "../../../../../data"} // Adjust path from test project to data folder
                })
                .Build();

            var logger = new LoggerFactory().CreateLogger<DataSeedingService>();

            using var context = new KollectorScumDbContext(options);
            var service = new DataSeedingService(context, logger, configuration);

            // Act
            await service.SeedLookupDataAsync();

            // Assert
            // Verify all lookup tables have data
            Assert.True(await context.Countries.CountAsync() > 0, "Countries should be seeded");
            Assert.True(await context.Stores.CountAsync() > 0, "Stores should be seeded");
            Assert.True(await context.Formats.CountAsync() > 0, "Formats should be seeded");
            Assert.True(await context.Genres.CountAsync() > 0, "Genres should be seeded");
            Assert.True(await context.Labels.CountAsync() > 0, "Labels should be seeded");
            Assert.True(await context.Artists.CountAsync() > 0, "Artists should be seeded");
            Assert.True(await context.Packagings.CountAsync() > 0, "Packagings should be seeded");
        }

        [Fact]
        public async Task VerifyJsonDataStructure_ShouldMatchExpectedFormat()
        {
            // Arrange
            var dataPath = Path.GetFullPath("../../../../../data");
            var countriesFilePath = Path.Combine(dataPath, "countrys.json");
            
            // Act & Assert - Verify file exists and can be parsed
            Assert.True(File.Exists(countriesFilePath), $"Countries JSON file should exist at {countriesFilePath}");
            
            var jsonContent = await File.ReadAllTextAsync(countriesFilePath);
            var jsonDoc = JsonDocument.Parse(jsonContent);
            
            Assert.True(jsonDoc.RootElement.TryGetProperty("countrys", out var countriesArray));
            Assert.True(countriesArray.GetArrayLength() > 0);
            
            var firstCountry = countriesArray[0];
            Assert.True(firstCountry.TryGetProperty("id", out _));
            Assert.True(firstCountry.TryGetProperty("name", out _));
        }
    }
}
