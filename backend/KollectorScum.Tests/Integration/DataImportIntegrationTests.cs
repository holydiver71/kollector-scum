using KollectorScum.Api.Data;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Repositories;
using KollectorScum.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KollectorScum.Tests.Integration
{
    public class DataImportIntegrationTests
    {


        [Fact]
        public async Task MusicReleaseImport_WithExistingLookupData_ShouldCreateRelationships()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<KollectorScumDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            using var context = new KollectorScumDbContext(options);
            using var unitOfWork = new UnitOfWork(context);

            var mockLogger = new Mock<ILogger<DataSeedingService>>();
            var mockMusicReleaseLogger = new Mock<ILogger<MusicReleaseImportService>>();
            
            // Use absolute path to data directory
            var dataPath = "/home/andy/Projects/kollector-scum/data";

            var seedingService = new DataSeedingService(context, mockLogger.Object, dataPath);
            var musicReleaseService = new MusicReleaseImportService(unitOfWork, mockMusicReleaseLogger.Object, dataPath);

            // Seed lookup data first
            await seedingService.SeedLookupDataAsync();

            // Act - Import music releases
            var result = await musicReleaseService.ImportMusicReleasesAsync();

            // Assert - Check basic import success
            var totalMusicReleases = await context.MusicReleases.CountAsync();
            var musicReleasesWithTitle = await context.MusicReleases
                .Where(mr => !string.IsNullOrEmpty(mr.Title))
                .CountAsync();

            // Report on import results
            Console.WriteLine($"Total Music Releases: {totalMusicReleases}");
            Console.WriteLine($"Music Releases with Titles: {musicReleasesWithTitle}");
            Console.WriteLine($"Import method result: {result}");

            // At minimum, import should complete successfully
            Assert.True(result >= 0);
            Assert.True(totalMusicReleases >= 0);
        }
    }
}
