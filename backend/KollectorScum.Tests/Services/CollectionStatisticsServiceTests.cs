using System.Text.Json;
using KollectorScum.Api.Data;
using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using KollectorScum.Api.Models.ValueObjects;
using KollectorScum.Api.Repositories;
using KollectorScum.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KollectorScum.Tests.Services
{
    public class CollectionStatisticsServiceTests
    {
        private static readonly Guid DefaultUserId = Guid.Parse("12337b39-c346-449c-b269-33b2e820d74f");

        private static KollectorScumDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<KollectorScumDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new KollectorScumDbContext(options);
        }

        private static Mock<IUserContext> CreateUserContext()
        {
            var mockUserContext = new Mock<IUserContext>();
            mockUserContext.Setup(u => u.GetActingUserId()).Returns(DefaultUserId);
            mockUserContext.Setup(u => u.GetUserId()).Returns(DefaultUserId);
            mockUserContext.Setup(u => u.IsAdmin()).Returns(false);
            return mockUserContext;
        }

        private static CollectionStatisticsService CreateService(
            KollectorScumDbContext context,
            Mock<IMusicReleaseMapperService> mapperMock,
            Mock<IUserContext>? userContextMock = null)
        {
            return new CollectionStatisticsService(
                new Repository<MusicRelease>(context),
                new Repository<Format>(context),
                new Repository<Country>(context),
                new Repository<Genre>(context),
                mapperMock.Object,
                Mock.Of<ILogger<CollectionStatisticsService>>(),
                (userContextMock ?? CreateUserContext()).Object);
        }

        [Fact]
        public async Task GetCollectionStatisticsAsync_WithNoReleases_ReturnsEmptyStatistics()
        {
            using var context = CreateContext();
            var mapperMock = new Mock<IMusicReleaseMapperService>();
            var service = CreateService(context, mapperMock);

            var result = await service.GetCollectionStatisticsAsync();

            Assert.NotNull(result);
            Assert.Equal(0, result.TotalReleases);
            Assert.Equal(0, result.TotalArtists);
            Assert.Equal(0, result.TotalGenres);
            Assert.Equal(0, result.TotalLabels);
            Assert.Null(result.TotalValue);
            Assert.Null(result.AveragePrice);
            Assert.Null(result.MostExpensiveRelease);
        }

        [Fact]
        public async Task GetCollectionStatisticsAsync_WithMultipleReleases_CountsTotalCorrectly()
        {
            using var context = CreateContext();
            var mapperMock = CreateSummaryMapperMock();
            context.MusicReleases.AddRange(
                new MusicRelease { Id = 1, Title = "Album 1", UserId = DefaultUserId, Artists = "[1]", Genres = "[1]", LabelId = 1, DateAdded = DateTime.UtcNow },
                new MusicRelease { Id = 2, Title = "Album 2", UserId = DefaultUserId, Artists = "[2]", Genres = "[2]", LabelId = 2, DateAdded = DateTime.UtcNow },
                new MusicRelease { Id = 3, Title = "Album 3", UserId = DefaultUserId, Artists = "[1,2]", Genres = "[1,2]", LabelId = 1, DateAdded = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var service = CreateService(context, mapperMock);

            var result = await service.GetCollectionStatisticsAsync();

            Assert.Equal(3, result.TotalReleases);
            Assert.Equal(2, result.TotalArtists);
            Assert.Equal(2, result.TotalGenres);
            Assert.Equal(2, result.TotalLabels);
        }

        [Fact]
        public async Task GetCollectionStatisticsAsync_WithPurchaseInfo_CalculatesTotalValue()
        {
            using var context = CreateContext();
            var mapperMock = CreateSummaryMapperMock();
            context.MusicReleases.AddRange(
                new MusicRelease
                {
                    Id = 1,
                    Title = "Album 1",
                    UserId = DefaultUserId,
                    Artists = "[1]",
                    PurchaseInfo = JsonSerializer.Serialize(new PurchaseInfo { Price = 10.99m }),
                    DateAdded = DateTime.UtcNow
                },
                new MusicRelease
                {
                    Id = 2,
                    Title = "Album 2",
                    UserId = DefaultUserId,
                    Artists = "[1]",
                    PurchaseInfo = JsonSerializer.Serialize(new PurchaseInfo { Price = 15.99m }),
                    DateAdded = DateTime.UtcNow
                });
            await context.SaveChangesAsync();

            var service = CreateService(context, mapperMock);

            var result = await service.GetCollectionStatisticsAsync();

            Assert.Equal(26.98m, result.TotalValue);
            Assert.Equal(13.49m, result.AveragePrice);
        }

        [Fact]
        public async Task GetCollectionStatisticsAsync_WithPurchaseInfo_FindsMostExpensiveRelease()
        {
            using var context = CreateContext();
            var mapperMock = CreateSummaryMapperMock();
            context.MusicReleases.AddRange(
                new MusicRelease
                {
                    Id = 1,
                    Title = "Cheap Album",
                    UserId = DefaultUserId,
                    Artists = "[1]",
                    PurchaseInfo = JsonSerializer.Serialize(new PurchaseInfo { Price = 10.99m }),
                    DateAdded = DateTime.UtcNow
                },
                new MusicRelease
                {
                    Id = 2,
                    Title = "Expensive Album",
                    UserId = DefaultUserId,
                    Artists = "[1]",
                    PurchaseInfo = JsonSerializer.Serialize(new PurchaseInfo { Price = 25.99m }),
                    DateAdded = DateTime.UtcNow
                },
                new MusicRelease
                {
                    Id = 3,
                    Title = "Mid Album",
                    UserId = DefaultUserId,
                    Artists = "[1]",
                    PurchaseInfo = JsonSerializer.Serialize(new PurchaseInfo { Price = 15.99m }),
                    DateAdded = DateTime.UtcNow
                });
            await context.SaveChangesAsync();

            var service = CreateService(context, mapperMock);

            var result = await service.GetCollectionStatisticsAsync();

            Assert.NotNull(result.MostExpensiveRelease);
            Assert.Equal(2, result.MostExpensiveRelease!.Id);
            Assert.Equal("Expensive Album", result.MostExpensiveRelease.Title);
        }

        [Fact]
        public async Task GetCollectionStatisticsAsync_WithReleasesByYear_CalculatesDistribution()
        {
            using var context = CreateContext();
            var mapperMock = CreateSummaryMapperMock();
            context.MusicReleases.AddRange(
                new MusicRelease { Id = 1, Title = "Album 1", UserId = DefaultUserId, ReleaseYear = new DateTime(2020, 1, 1), Artists = "[1]", DateAdded = DateTime.UtcNow },
                new MusicRelease { Id = 2, Title = "Album 2", UserId = DefaultUserId, ReleaseYear = new DateTime(2020, 1, 1), Artists = "[1]", DateAdded = DateTime.UtcNow },
                new MusicRelease { Id = 3, Title = "Album 3", UserId = DefaultUserId, ReleaseYear = new DateTime(2021, 1, 1), Artists = "[1]", DateAdded = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var service = CreateService(context, mapperMock);

            var result = await service.GetCollectionStatisticsAsync();

            Assert.Equal(2, result.ReleasesByYear.Count);
            Assert.Equal(2, result.ReleasesByYear.First(y => y.Year == 2020).Count);
            Assert.Equal(1, result.ReleasesByYear.First(y => y.Year == 2021).Count);
        }

        [Fact]
        public async Task GetCollectionStatisticsAsync_WithFormats_CalculatesFormatDistribution()
        {
            using var context = CreateContext();
            var mapperMock = CreateSummaryMapperMock();
            context.Formats.AddRange(
                new Format { Id = 1, Name = "CD", UserId = DefaultUserId },
                new Format { Id = 2, Name = "Vinyl", UserId = DefaultUserId });
            context.MusicReleases.AddRange(
                new MusicRelease { Id = 1, Title = "Album 1", UserId = DefaultUserId, FormatId = 1, Artists = "[1]", DateAdded = DateTime.UtcNow },
                new MusicRelease { Id = 2, Title = "Album 2", UserId = DefaultUserId, FormatId = 1, Artists = "[1]", DateAdded = DateTime.UtcNow },
                new MusicRelease { Id = 3, Title = "Album 3", UserId = DefaultUserId, FormatId = 2, Artists = "[1]", DateAdded = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var service = CreateService(context, mapperMock);

            var result = await service.GetCollectionStatisticsAsync();

            Assert.Equal(2, result.ReleasesByFormat.Count);
            Assert.Equal(2, result.ReleasesByFormat.First(f => f.FormatName == "CD").Count);
            Assert.Equal(66.67m, Math.Round(result.ReleasesByFormat.First(f => f.FormatName == "CD").Percentage, 2));
            Assert.Equal(1, result.ReleasesByFormat.First(f => f.FormatName == "Vinyl").Count);
        }

        [Fact]
        public async Task GetCollectionStatisticsAsync_WithCountries_CalculatesCountryDistribution()
        {
            using var context = CreateContext();
            var mapperMock = CreateSummaryMapperMock();
            context.Countries.AddRange(
                new Country { Id = 1, Name = "UK", UserId = DefaultUserId },
                new Country { Id = 2, Name = "US", UserId = DefaultUserId });
            context.MusicReleases.AddRange(
                new MusicRelease { Id = 1, Title = "Album 1", UserId = DefaultUserId, CountryId = 1, Artists = "[1]", DateAdded = DateTime.UtcNow },
                new MusicRelease { Id = 2, Title = "Album 2", UserId = DefaultUserId, CountryId = 1, Artists = "[1]", DateAdded = DateTime.UtcNow },
                new MusicRelease { Id = 3, Title = "Album 3", UserId = DefaultUserId, CountryId = 2, Artists = "[1]", DateAdded = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var service = CreateService(context, mapperMock);

            var result = await service.GetCollectionStatisticsAsync();

            Assert.Equal(2, result.ReleasesByCountry.Count);
            Assert.Equal(2, result.ReleasesByCountry.First(c => c.CountryName == "UK").Count);
            Assert.Equal(1, result.ReleasesByCountry.First(c => c.CountryName == "US").Count);
        }

        [Fact]
        public async Task GetCollectionStatisticsAsync_WithGenres_CalculatesGenreDistribution()
        {
            using var context = CreateContext();
            var mapperMock = CreateSummaryMapperMock();
            context.Genres.AddRange(
                new Genre { Id = 1, Name = "Rock", UserId = DefaultUserId },
                new Genre { Id = 2, Name = "Pop", UserId = DefaultUserId });
            context.MusicReleases.AddRange(
                new MusicRelease { Id = 1, Title = "Album 1", UserId = DefaultUserId, Genres = "[1]", Artists = "[1]", DateAdded = DateTime.UtcNow },
                new MusicRelease { Id = 2, Title = "Album 2", UserId = DefaultUserId, Genres = "[1]", Artists = "[1]", DateAdded = DateTime.UtcNow },
                new MusicRelease { Id = 3, Title = "Album 3", UserId = DefaultUserId, Genres = "[2]", Artists = "[1]", DateAdded = DateTime.UtcNow },
                new MusicRelease { Id = 4, Title = "Album 4", UserId = DefaultUserId, Genres = "[1,2]", Artists = "[1]", DateAdded = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var service = CreateService(context, mapperMock);

            var result = await service.GetCollectionStatisticsAsync();

            Assert.Equal(2, result.ReleasesByGenre.Count);
            Assert.Equal(3, result.ReleasesByGenre.First(g => g.GenreName == "Rock").Count);
            Assert.Equal(2, result.ReleasesByGenre.First(g => g.GenreName == "Pop").Count);
        }

        [Fact]
        public async Task GetCollectionStatisticsAsync_WithRecentReleases_ReturnsTop10()
        {
            using var context = CreateContext();
            var mapperMock = CreateSummaryMapperMock();
            for (int index = 1; index <= 15; index++)
            {
                context.MusicReleases.Add(new MusicRelease
                {
                    Id = index,
                    Title = $"Album {index}",
                    UserId = DefaultUserId,
                    Artists = "[1]",
                    DateAdded = DateTime.UtcNow.AddDays(-index)
                });
            }
            await context.SaveChangesAsync();

            var service = CreateService(context, mapperMock);

            var result = await service.GetCollectionStatisticsAsync();

            Assert.Equal(10, result.RecentlyAdded.Count);
            Assert.Equal(1, result.RecentlyAdded[0].Id);
            Assert.Equal(10, result.RecentlyAdded[9].Id);
        }

        [Fact]
        public async Task GetCollectionStatisticsAsync_WithNullReleaseYear_HandlesGracefully()
        {
            using var context = CreateContext();
            var mapperMock = CreateSummaryMapperMock();
            context.MusicReleases.AddRange(
                new MusicRelease { Id = 1, Title = "Album 1", UserId = DefaultUserId, ReleaseYear = null, Artists = "[1]", DateAdded = DateTime.UtcNow },
                new MusicRelease { Id = 2, Title = "Album 2", UserId = DefaultUserId, ReleaseYear = new DateTime(2020, 1, 1), Artists = "[1]", DateAdded = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var service = CreateService(context, mapperMock);

            var result = await service.GetCollectionStatisticsAsync();

            Assert.Equal(2, result.TotalReleases);
            Assert.Single(result.ReleasesByYear);
        }

        [Fact]
        public async Task GetCollectionStatisticsAsync_WithMixedPurchaseInfo_CalculatesCorrectly()
        {
            using var context = CreateContext();
            var mapperMock = CreateSummaryMapperMock();
            context.MusicReleases.AddRange(
                new MusicRelease
                {
                    Id = 1,
                    Title = "Purchased Album",
                    UserId = DefaultUserId,
                    Artists = "[1]",
                    PurchaseInfo = JsonSerializer.Serialize(new PurchaseInfo { Price = 10.99m }),
                    DateAdded = DateTime.UtcNow
                },
                new MusicRelease
                {
                    Id = 2,
                    Title = "Free Album",
                    UserId = DefaultUserId,
                    Artists = "[1]",
                    PurchaseInfo = null,
                    DateAdded = DateTime.UtcNow
                });
            await context.SaveChangesAsync();

            var service = CreateService(context, mapperMock);

            var result = await service.GetCollectionStatisticsAsync();

            Assert.Equal(10.99m, result.TotalValue);
            Assert.Equal(10.99m, result.AveragePrice);
        }

        [Fact]
        public async Task GetCollectionStatisticsAsync_WithoutActingUser_ReturnsEmptyStatistics()
        {
            using var context = CreateContext();
            var mapperMock = CreateSummaryMapperMock();
            var userContextMock = new Mock<IUserContext>();
            userContextMock.Setup(u => u.GetActingUserId()).Returns((Guid?)null);

            var service = CreateService(context, mapperMock, userContextMock);

            var result = await service.GetCollectionStatisticsAsync();

            Assert.Equal(0, result.TotalReleases);
            Assert.Empty(result.ReleasesByYear);
            Assert.Empty(result.RecentlyAdded);
        }

        [Fact]
        public async Task GetCollectionStatisticsAsync_UsesOnlyActingUserReleases()
        {
            using var context = CreateContext();
            var mapperMock = CreateSummaryMapperMock();
            context.MusicReleases.AddRange(
                new MusicRelease { Id = 1, Title = "Mine", UserId = DefaultUserId, Artists = "[1]", Genres = "[1]", LabelId = 1, DateAdded = DateTime.UtcNow },
                new MusicRelease { Id = 2, Title = "Not Mine", UserId = Guid.NewGuid(), Artists = "[2]", Genres = "[2]", LabelId = 2, DateAdded = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var service = CreateService(context, mapperMock);

            var result = await service.GetCollectionStatisticsAsync();

            Assert.Equal(1, result.TotalReleases);
            Assert.Single(result.RecentlyAdded);
            Assert.Equal("Mine", result.RecentlyAdded[0].Title);
        }

        private static Mock<IMusicReleaseMapperService> CreateSummaryMapperMock()
        {
            var mapperMock = new Mock<IMusicReleaseMapperService>();
            mapperMock.Setup(m => m.MapToSummaryDto(It.IsAny<MusicRelease>()))
                .Returns((MusicRelease mr) => new MusicReleaseSummaryDto
                {
                    Id = mr.Id,
                    Title = mr.Title,
                    DateAdded = mr.DateAdded,
                    ReleaseYear = mr.ReleaseYear
                });

            return mapperMock;
        }
    }
}
