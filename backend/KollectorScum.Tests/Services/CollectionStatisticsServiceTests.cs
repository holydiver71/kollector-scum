using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using KollectorScum.Api.Services;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using KollectorScum.Api.Models.ValueObjects;
using KollectorScum.Api.DTOs;
using System.Text.Json;

namespace KollectorScum.Tests.Services
{
    public class CollectionStatisticsServiceTests
    {
        private readonly Mock<IRepository<MusicRelease>> _mockMusicReleaseRepo;
        private readonly Mock<IRepository<Format>> _mockFormatRepo;
        private readonly Mock<IRepository<Country>> _mockCountryRepo;
        private readonly Mock<IRepository<Genre>> _mockGenreRepo;
        private readonly Mock<IMusicReleaseMapperService> _mockMapperService;
        private readonly Mock<ILogger<CollectionStatisticsService>> _mockLogger;
        private readonly CollectionStatisticsService _service;

        public CollectionStatisticsServiceTests()
        {
            _mockMusicReleaseRepo = new Mock<IRepository<MusicRelease>>();
            _mockFormatRepo = new Mock<IRepository<Format>>();
            _mockCountryRepo = new Mock<IRepository<Country>>();
            _mockGenreRepo = new Mock<IRepository<Genre>>();
            _mockMapperService = new Mock<IMusicReleaseMapperService>();
            _mockLogger = new Mock<ILogger<CollectionStatisticsService>>();

            _service = new CollectionStatisticsService(
                _mockMusicReleaseRepo.Object,
                _mockFormatRepo.Object,
                _mockCountryRepo.Object,
                _mockGenreRepo.Object,
                _mockMapperService.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task GetCollectionStatisticsAsync_WithNoReleases_ReturnsEmptyStatistics()
        {
            // Arrange
            _mockMusicReleaseRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<MusicRelease>());

            // Act
            var result = await _service.GetCollectionStatisticsAsync();

            // Assert
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
            // Arrange
            var releases = new List<MusicRelease>
            {
                new MusicRelease { Id = 1, Title = "Album 1", Artists = "[1]", Genres = "[1]", LabelId = 1, DateAdded = DateTime.UtcNow },
                new MusicRelease { Id = 2, Title = "Album 2", Artists = "[2]", Genres = "[2]", LabelId = 2, DateAdded = DateTime.UtcNow },
                new MusicRelease { Id = 3, Title = "Album 3", Artists = "[1,2]", Genres = "[1,2]", LabelId = 1, DateAdded = DateTime.UtcNow }
            };

            _mockMusicReleaseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(releases);
            
            _mockMapperService.Setup(m => m.MapToSummaryDto(It.IsAny<MusicRelease>()))
                .Returns((MusicRelease mr) => new MusicReleaseSummaryDto { Id = mr.Id, Title = mr.Title });

            // Act
            var result = await _service.GetCollectionStatisticsAsync();

            // Assert
            Assert.Equal(3, result.TotalReleases);
            Assert.Equal(2, result.TotalArtists);
            Assert.Equal(2, result.TotalGenres);
            Assert.Equal(2, result.TotalLabels);
        }

        [Fact]
        public async Task GetCollectionStatisticsAsync_WithPurchaseInfo_CalculatesTotalValue()
        {
            // Arrange
            var purchaseInfo1 = new PurchaseInfo { Price = 10.99m };
            var purchaseInfo2 = new PurchaseInfo { Price = 15.99m };

            var releases = new List<MusicRelease>
            {
                new MusicRelease 
                { 
                    Id = 1, 
                    Title = "Album 1", 
                    Artists = "[1]",
                    PurchaseInfo = JsonSerializer.Serialize(purchaseInfo1),
                    DateAdded = DateTime.UtcNow 
                },
                new MusicRelease 
                { 
                    Id = 2, 
                    Title = "Album 2", 
                    Artists = "[1]",
                    PurchaseInfo = JsonSerializer.Serialize(purchaseInfo2),
                    DateAdded = DateTime.UtcNow 
                }
            };

            _mockMusicReleaseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(releases);
            
            _mockMapperService.Setup(m => m.MapToSummaryDto(It.IsAny<MusicRelease>()))
                .Returns((MusicRelease mr) => new MusicReleaseSummaryDto { Id = mr.Id, Title = mr.Title });

            // Act
            var result = await _service.GetCollectionStatisticsAsync();

            // Assert
            Assert.NotNull(result.TotalValue);
            Assert.Equal(26.98m, result.TotalValue);
            Assert.NotNull(result.AveragePrice);
            Assert.Equal(13.49m, result.AveragePrice);
        }

        [Fact]
        public async Task GetCollectionStatisticsAsync_WithPurchaseInfo_FindsMostExpensiveRelease()
        {
            // Arrange
            var purchaseInfo1 = new PurchaseInfo { Price = 10.99m };
            var purchaseInfo2 = new PurchaseInfo { Price = 25.99m };
            var purchaseInfo3 = new PurchaseInfo { Price = 15.99m };

            var releases = new List<MusicRelease>
            {
                new MusicRelease 
                { 
                    Id = 1, 
                    Title = "Cheap Album", 
                    Artists = "[1]",
                    PurchaseInfo = JsonSerializer.Serialize(purchaseInfo1),
                    DateAdded = DateTime.UtcNow 
                },
                new MusicRelease 
                { 
                    Id = 2, 
                    Title = "Expensive Album", 
                    Artists = "[1]",
                    PurchaseInfo = JsonSerializer.Serialize(purchaseInfo2),
                    DateAdded = DateTime.UtcNow 
                },
                new MusicRelease 
                { 
                    Id = 3, 
                    Title = "Mid Album", 
                    Artists = "[1]",
                    PurchaseInfo = JsonSerializer.Serialize(purchaseInfo3),
                    DateAdded = DateTime.UtcNow 
                }
            };

            _mockMusicReleaseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(releases);
            
            _mockMapperService.Setup(m => m.MapToSummaryDto(It.IsAny<MusicRelease>()))
                .Returns((MusicRelease mr) => new MusicReleaseSummaryDto { Id = mr.Id, Title = mr.Title });

            // Act
            var result = await _service.GetCollectionStatisticsAsync();

            // Assert
            Assert.NotNull(result.MostExpensiveRelease);
            Assert.Equal(2, result.MostExpensiveRelease.Id);
            Assert.Equal("Expensive Album", result.MostExpensiveRelease.Title);
        }

        [Fact]
        public async Task GetCollectionStatisticsAsync_WithReleasesByYear_CalculatesDistribution()
        {
            // Arrange
            var releases = new List<MusicRelease>
            {
                new MusicRelease { Id = 1, Title = "Album 1", ReleaseYear = new DateTime(2020, 1, 1), Artists = "[1]", DateAdded = DateTime.UtcNow },
                new MusicRelease { Id = 2, Title = "Album 2", ReleaseYear = new DateTime(2020, 1, 1), Artists = "[1]", DateAdded = DateTime.UtcNow },
                new MusicRelease { Id = 3, Title = "Album 3", ReleaseYear = new DateTime(2021, 1, 1), Artists = "[1]", DateAdded = DateTime.UtcNow }
            };

            _mockMusicReleaseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(releases);
            
            _mockMapperService.Setup(m => m.MapToSummaryDto(It.IsAny<MusicRelease>()))
                .Returns((MusicRelease mr) => new MusicReleaseSummaryDto { Id = mr.Id, Title = mr.Title });

            // Act
            var result = await _service.GetCollectionStatisticsAsync();

            // Assert
            Assert.NotNull(result.ReleasesByYear);
            Assert.Equal(2, result.ReleasesByYear.Count);
            
            var year2020 = result.ReleasesByYear.FirstOrDefault(y => y.Year == 2020);
            Assert.NotNull(year2020);
            Assert.Equal(2, year2020.Count);

            var year2021 = result.ReleasesByYear.FirstOrDefault(y => y.Year == 2021);
            Assert.NotNull(year2021);
            Assert.Equal(1, year2021.Count);
        }

        [Fact]
        public async Task GetCollectionStatisticsAsync_WithFormats_CalculatesFormatDistribution()
        {
            // Arrange
            var releases = new List<MusicRelease>
            {
                new MusicRelease { Id = 1, Title = "Album 1", FormatId = 1, Artists = "[1]", DateAdded = DateTime.UtcNow },
                new MusicRelease { Id = 2, Title = "Album 2", FormatId = 1, Artists = "[1]", DateAdded = DateTime.UtcNow },
                new MusicRelease { Id = 3, Title = "Album 3", FormatId = 2, Artists = "[1]", DateAdded = DateTime.UtcNow }
            };

            var formats = new List<Format>
            {
                new Format { Id = 1, Name = "CD" },
                new Format { Id = 2, Name = "Vinyl" }
            };

            _mockMusicReleaseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(releases);
            _mockFormatRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(formats);
            
            _mockMapperService.Setup(m => m.MapToSummaryDto(It.IsAny<MusicRelease>()))
                .Returns((MusicRelease mr) => new MusicReleaseSummaryDto { Id = mr.Id, Title = mr.Title });

            // Act
            var result = await _service.GetCollectionStatisticsAsync();

            // Assert
            Assert.NotNull(result.ReleasesByFormat);
            Assert.Equal(2, result.ReleasesByFormat.Count);
            
            var cdStats = result.ReleasesByFormat.FirstOrDefault(f => f.FormatName == "CD");
            Assert.NotNull(cdStats);
            Assert.Equal(2, cdStats.Count);
            Assert.Equal(66.67m, Math.Round(cdStats.Percentage, 2));

            var vinylStats = result.ReleasesByFormat.FirstOrDefault(f => f.FormatName == "Vinyl");
            Assert.NotNull(vinylStats);
            Assert.Equal(1, vinylStats.Count);
            Assert.Equal(33.33m, Math.Round(vinylStats.Percentage, 2));
        }

        [Fact]
        public async Task GetCollectionStatisticsAsync_WithCountries_CalculatesCountryDistribution()
        {
            // Arrange
            var releases = new List<MusicRelease>
            {
                new MusicRelease { Id = 1, Title = "Album 1", CountryId = 1, Artists = "[1]", DateAdded = DateTime.UtcNow },
                new MusicRelease { Id = 2, Title = "Album 2", CountryId = 1, Artists = "[1]", DateAdded = DateTime.UtcNow },
                new MusicRelease { Id = 3, Title = "Album 3", CountryId = 2, Artists = "[1]", DateAdded = DateTime.UtcNow }
            };

            var countries = new List<Country>
            {
                new Country { Id = 1, Name = "UK" },
                new Country { Id = 2, Name = "US" }
            };

            _mockMusicReleaseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(releases);
            _mockCountryRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(countries);
            
            _mockMapperService.Setup(m => m.MapToSummaryDto(It.IsAny<MusicRelease>()))
                .Returns((MusicRelease mr) => new MusicReleaseSummaryDto { Id = mr.Id, Title = mr.Title });

            // Act
            var result = await _service.GetCollectionStatisticsAsync();

            // Assert
            Assert.NotNull(result.ReleasesByCountry);
            Assert.Equal(2, result.ReleasesByCountry.Count);
            
            var ukStats = result.ReleasesByCountry.FirstOrDefault(c => c.CountryName == "UK");
            Assert.NotNull(ukStats);
            Assert.Equal(2, ukStats.Count);

            var usStats = result.ReleasesByCountry.FirstOrDefault(c => c.CountryName == "US");
            Assert.NotNull(usStats);
            Assert.Equal(1, usStats.Count);
        }

        [Fact]
        public async Task GetCollectionStatisticsAsync_WithGenres_CalculatesGenreDistribution()
        {
            // Arrange
            var releases = new List<MusicRelease>
            {
                new MusicRelease { Id = 1, Title = "Album 1", Genres = "[1]", Artists = "[1]", DateAdded = DateTime.UtcNow },
                new MusicRelease { Id = 2, Title = "Album 2", Genres = "[1]", Artists = "[1]", DateAdded = DateTime.UtcNow },
                new MusicRelease { Id = 3, Title = "Album 3", Genres = "[2]", Artists = "[1]", DateAdded = DateTime.UtcNow },
                new MusicRelease { Id = 4, Title = "Album 4", Genres = "[1,2]", Artists = "[1]", DateAdded = DateTime.UtcNow }
            };

            var genres = new List<Genre>
            {
                new Genre { Id = 1, Name = "Rock" },
                new Genre { Id = 2, Name = "Pop" }
            };

            _mockMusicReleaseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(releases);
            _mockGenreRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(genres);
            
            _mockMapperService.Setup(m => m.MapToSummaryDto(It.IsAny<MusicRelease>()))
                .Returns((MusicRelease mr) => new MusicReleaseSummaryDto { Id = mr.Id, Title = mr.Title });

            // Act
            var result = await _service.GetCollectionStatisticsAsync();

            // Assert
            Assert.NotNull(result.ReleasesByGenre);
            Assert.Equal(2, result.ReleasesByGenre.Count);
            
            var rockStats = result.ReleasesByGenre.FirstOrDefault(g => g.GenreName == "Rock");
            Assert.NotNull(rockStats);
            Assert.Equal(3, rockStats.Count); // Albums 1, 2, 4

            var popStats = result.ReleasesByGenre.FirstOrDefault(g => g.GenreName == "Pop");
            Assert.NotNull(popStats);
            Assert.Equal(2, popStats.Count); // Albums 3, 4
        }

        [Fact]
        public async Task GetCollectionStatisticsAsync_WithRecentReleases_ReturnsTop10()
        {
            // Arrange
            var releases = new List<MusicRelease>();
            for (int i = 1; i <= 15; i++)
            {
                releases.Add(new MusicRelease 
                { 
                    Id = i, 
                    Title = $"Album {i}", 
                    Artists = "[1]",
                    DateAdded = DateTime.UtcNow.AddDays(-i) 
                });
            }

            _mockMusicReleaseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(releases);
            
            _mockMapperService.Setup(m => m.MapToSummaryDto(It.IsAny<MusicRelease>()))
                .Returns((MusicRelease mr) => new MusicReleaseSummaryDto { Id = mr.Id, Title = mr.Title });

            // Act
            var result = await _service.GetCollectionStatisticsAsync();

            // Assert
            Assert.NotNull(result.RecentlyAdded);
            Assert.Equal(10, result.RecentlyAdded.Count);
            Assert.Equal(1, result.RecentlyAdded[0].Id); // Most recent
            Assert.Equal(10, result.RecentlyAdded[9].Id); // 10th most recent
        }

        [Fact]
        public async Task GetCollectionStatisticsAsync_WithNullReleaseYear_HandlesGracefully()
        {
            // Arrange
            var releases = new List<MusicRelease>
            {
                new MusicRelease { Id = 1, Title = "Album 1", ReleaseYear = null, Artists = "[1]", DateAdded = DateTime.UtcNow },
                new MusicRelease { Id = 2, Title = "Album 2", ReleaseYear = new DateTime(2020, 1, 1), Artists = "[1]", DateAdded = DateTime.UtcNow }
            };

            _mockMusicReleaseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(releases);
            
            _mockMapperService.Setup(m => m.MapToSummaryDto(It.IsAny<MusicRelease>()))
                .Returns((MusicRelease mr) => new MusicReleaseSummaryDto { Id = mr.Id, Title = mr.Title });

            // Act
            var result = await _service.GetCollectionStatisticsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalReleases);
            Assert.NotNull(result.ReleasesByYear);
            Assert.Single(result.ReleasesByYear); // Only one with valid year
        }

        [Fact]
        public async Task GetCollectionStatisticsAsync_WithMixedPurchaseInfo_CalculatesCorrectly()
        {
            // Arrange
            var purchaseInfo = new PurchaseInfo { Price = 10.99m };

            var releases = new List<MusicRelease>
            {
                new MusicRelease 
                { 
                    Id = 1, 
                    Title = "Purchased Album", 
                    Artists = "[1]",
                    PurchaseInfo = JsonSerializer.Serialize(purchaseInfo),
                    DateAdded = DateTime.UtcNow 
                },
                new MusicRelease 
                { 
                    Id = 2, 
                    Title = "Free Album", 
                    Artists = "[1]",
                    PurchaseInfo = null,
                    DateAdded = DateTime.UtcNow 
                }
            };

            _mockMusicReleaseRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(releases);
            
            _mockMapperService.Setup(m => m.MapToSummaryDto(It.IsAny<MusicRelease>()))
                .Returns((MusicRelease mr) => new MusicReleaseSummaryDto { Id = mr.Id, Title = mr.Title });

            // Act
            var result = await _service.GetCollectionStatisticsAsync();

            // Assert
            Assert.NotNull(result.TotalValue);
            Assert.Equal(10.99m, result.TotalValue);
            Assert.NotNull(result.AveragePrice);
            Assert.Equal(10.99m, result.AveragePrice); // Average only counts purchased items
        }
    }
}
