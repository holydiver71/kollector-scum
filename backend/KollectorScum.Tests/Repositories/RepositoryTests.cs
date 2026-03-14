using KollectorScum.Api.Data;
using KollectorScum.Api.Models;
using KollectorScum.Api.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KollectorScum.Tests.Repositories
{
    /// <summary>
    /// Tests for generic Repository read operations.
    /// </summary>
    public class RepositoryTests
    {
        private static KollectorScumDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<KollectorScumDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            return new KollectorScumDbContext(options);
        }

        [Fact]
        public async Task GetAllAsync_DoesNotTrackEntities()
        {
            // Arrange
            using var context = CreateContext(nameof(GetAllAsync_DoesNotTrackEntities));
            var userId = Guid.NewGuid();
            context.Artists.Add(new Artist { Name = "Artist A", UserId = userId });
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            var repository = new Repository<Artist>(context);

            // Act
            var artists = await repository.GetAllAsync();

            // Assert
            Assert.Single(artists);
            Assert.Empty(context.ChangeTracker.Entries<Artist>());
        }

        [Fact]
        public async Task GetAsync_DoesNotTrackEntities()
        {
            // Arrange
            using var context = CreateContext(nameof(GetAsync_DoesNotTrackEntities));
            var userId = Guid.NewGuid();
            context.Artists.AddRange(
                new Artist { Name = "Metallica", UserId = userId },
                new Artist { Name = "Megadeth", UserId = userId });
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            var repository = new Repository<Artist>(context);

            // Act
            var artists = await repository.GetAsync(a => a.Name.StartsWith("M"));

            // Assert
            Assert.Equal(2, artists.Count());
            Assert.Empty(context.ChangeTracker.Entries<Artist>());
        }

        [Fact]
        public async Task GetFirstOrDefaultAsync_DoesNotTrackEntity()
        {
            // Arrange
            using var context = CreateContext(nameof(GetFirstOrDefaultAsync_DoesNotTrackEntity));
            var userId = Guid.NewGuid();
            context.Artists.Add(new Artist { Name = "Opeth", UserId = userId });
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            var repository = new Repository<Artist>(context);

            // Act
            var artist = await repository.GetFirstOrDefaultAsync(a => a.Name == "Opeth");

            // Assert
            Assert.NotNull(artist);
            Assert.Equal("Opeth", artist.Name);
            Assert.Empty(context.ChangeTracker.Entries<Artist>());
        }

        [Fact]
        public async Task GetPagedAsync_DoesNotTrackEntities()
        {
            // Arrange
            using var context = CreateContext(nameof(GetPagedAsync_DoesNotTrackEntities));
            var userId = Guid.NewGuid();
            context.Artists.AddRange(
                new Artist { Name = "A", UserId = userId },
                new Artist { Name = "B", UserId = userId },
                new Artist { Name = "C", UserId = userId });
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            var repository = new Repository<Artist>(context);

            // Act
            var pagedResult = await repository.GetPagedAsync(
                pageNumber: 1,
                pageSize: 2,
                orderBy: q => q.OrderBy(a => a.Name));

            // Assert
            Assert.Equal(3, pagedResult.TotalCount);
            Assert.Equal(2, pagedResult.Items.Count());
            Assert.Empty(context.ChangeTracker.Entries<Artist>());
        }
    }
}
