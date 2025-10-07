using KollectorScum.Api.Data;
using KollectorScum.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace KollectorScum.Tests.Services
{
    /// <summary>
    /// Unit tests for the DataSeedingService
    /// </summary>
    public class DataSeedingServiceTests : IDisposable
    {
        private readonly KollectorScumDbContext _context;
        private readonly DataSeedingService _seedingService;
        private readonly Mock<ILogger<DataSeedingService>> _mockLogger;
        private readonly Mock<IConfiguration> _mockConfiguration;

        public DataSeedingServiceTests()
        {
            var options = new DbContextOptionsBuilder<KollectorScumDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new KollectorScumDbContext(options);
            _mockLogger = new Mock<ILogger<DataSeedingService>>();
            _mockConfiguration = new Mock<IConfiguration>();

            // Setup configuration to point to the test data path
            _mockConfiguration.Setup(c => c["DataPath"])
                .Returns("/home/andy/Projects/kollector-scum/data");

            _seedingService = new DataSeedingService(_context, _mockLogger.Object, _mockConfiguration.Object);
        }

        [Fact]
        public async Task SeedCountriesAsync_ShouldSeedCountriesFromJson()
        {
            // Act
            await _seedingService.SeedCountriesAsync();

            // Assert
            var countries = await _context.Countries.ToListAsync();
            Assert.NotEmpty(countries);
            Assert.Contains(countries, c => c.Name == "Austria");
            Assert.Contains(countries, c => c.Name == "Belgium");
        }

        [Fact]
        public async Task SeedCountriesAsync_WhenDataExists_ShouldSkipSeeding()
        {
            // Arrange - Add some existing data
            _context.Countries.Add(new KollectorScum.Api.Models.Country { Id = 999, Name = "Test Country" });
            await _context.SaveChangesAsync();

            var initialCount = await _context.Countries.CountAsync();

            // Act
            await _seedingService.SeedCountriesAsync();

            // Assert - Count should not have increased
            var finalCount = await _context.Countries.CountAsync();
            Assert.Equal(initialCount, finalCount);
        }

        [Fact]
        public async Task SeedStoresAsync_ShouldSeedStoresFromJson()
        {
            // Act
            await _seedingService.SeedStoresAsync();

            // Assert
            var stores = await _context.Stores.ToListAsync();
            Assert.NotEmpty(stores);
            Assert.Contains(stores, s => s.Name.Contains("Amazon"));
        }

        [Fact]
        public async Task SeedFormatsAsync_ShouldSeedFormatsFromJson()
        {
            // Act
            await _seedingService.SeedFormatsAsync();

            // Assert
            var formats = await _context.Formats.ToListAsync();
            Assert.NotEmpty(formats);
            Assert.Contains(formats, f => f.Name.Contains("Vinyl"));
        }

        [Fact]
        public async Task SeedGenresAsync_ShouldSeedGenresFromJson()
        {
            // Act
            await _seedingService.SeedGenresAsync();

            // Assert
            var genres = await _context.Genres.ToListAsync();
            Assert.NotEmpty(genres);
            Assert.Contains(genres, g => g.Name == "Rock" || g.Name.Contains("Rock"));
        }

        [Fact]
        public async Task SeedLabelsAsync_ShouldSeedLabelsFromJson()
        {
            // Act
            await _seedingService.SeedLabelsAsync();

            // Assert
            var labels = await _context.Labels.ToListAsync();
            Assert.NotEmpty(labels);
        }

        [Fact]
        public async Task SeedArtistsAsync_ShouldSeedArtistsFromJson()
        {
            // Act
            await _seedingService.SeedArtistsAsync();

            // Assert
            var artists = await _context.Artists.ToListAsync();
            Assert.NotEmpty(artists);
        }

        [Fact]
        public async Task SeedPackagingsAsync_ShouldSeedPackagingsFromJson()
        {
            // Act
            await _seedingService.SeedPackagingsAsync();

            // Assert
            var packagings = await _context.Packagings.ToListAsync();
            Assert.NotEmpty(packagings);
        }

        [Fact]
        public async Task SeedLookupDataAsync_ShouldSeedAllLookupTables()
        {
            // Act
            await _seedingService.SeedLookupDataAsync();

            // Assert
            Assert.NotEmpty(await _context.Countries.ToListAsync());
            Assert.NotEmpty(await _context.Stores.ToListAsync());
            Assert.NotEmpty(await _context.Formats.ToListAsync());
            Assert.NotEmpty(await _context.Genres.ToListAsync());
            Assert.NotEmpty(await _context.Labels.ToListAsync());
            Assert.NotEmpty(await _context.Artists.ToListAsync());
            Assert.NotEmpty(await _context.Packagings.ToListAsync());
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
