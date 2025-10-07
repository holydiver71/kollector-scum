using KollectorScum.Api.Data;
using KollectorScum.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace KollectorScum.Tests.Data
{
    /// <summary>
    /// Unit tests for the KollectorScumDbContext
    /// </summary>
    public class KollectorScumDbContextTests : IDisposable
    {
        private readonly KollectorScumDbContext _context;

        public KollectorScumDbContextTests()
        {
            var options = new DbContextOptionsBuilder<KollectorScumDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new KollectorScumDbContext(options);
        }

        [Fact]
        public void DbContext_ShouldHaveAllRequiredDbSets()
        {
            // Assert
            Assert.NotNull(_context.Countries);
            Assert.NotNull(_context.Stores);
            Assert.NotNull(_context.Formats);
            Assert.NotNull(_context.Genres);
            Assert.NotNull(_context.Labels);
            Assert.NotNull(_context.Artists);
            Assert.NotNull(_context.Packagings);
            Assert.NotNull(_context.MusicReleases);
        }

        [Fact]
        public void DbContext_CanAddAndRetrieveCountry()
        {
            // Arrange
            var country = new Country
            {
                Name = "Test Country"
            };

            // Act
            _context.Countries.Add(country);
            _context.SaveChanges();

            var retrievedCountry = _context.Countries.First();

            // Assert
            Assert.Equal("Test Country", retrievedCountry.Name);
        }

        [Fact]
        public void DbContext_CanAddMusicReleaseWithRelationships()
        {
            // Arrange
            var country = new Country { Name = "USA" };
            var label = new Label { Name = "Test Label" };
            var format = new Format { Name = "CD" };
            var packaging = new Packaging { Name = "Jewel Case" };

            _context.Countries.Add(country);
            _context.Labels.Add(label);
            _context.Formats.Add(format);
            _context.Packagings.Add(packaging);
            _context.SaveChanges();

            var musicRelease = new MusicRelease
            {
                Title = "Test Album",
                CountryId = country.Id,
                LabelId = label.Id,
                FormatId = format.Id,
                PackagingId = packaging.Id
            };

            // Act
            _context.MusicReleases.Add(musicRelease);
            _context.SaveChanges();

            var retrievedRelease = _context.MusicReleases
                .Include(mr => mr.Country)
                .Include(mr => mr.Label)
                .Include(mr => mr.Format)
                .Include(mr => mr.Packaging)
                .First();

            // Assert
            Assert.Equal("Test Album", retrievedRelease.Title);
            Assert.Equal("USA", retrievedRelease.Country!.Name);
            Assert.Equal("Test Label", retrievedRelease.Label!.Name);
            Assert.Equal("CD", retrievedRelease.Format!.Name);
            Assert.Equal("Jewel Case", retrievedRelease.Packaging!.Name);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
