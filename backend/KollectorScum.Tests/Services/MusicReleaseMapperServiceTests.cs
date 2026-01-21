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
    public class MusicReleaseMapperServiceTests
    {
        private readonly Mock<IRepository<Artist>> _mockArtistRepo;
        private readonly Mock<IRepository<Genre>> _mockGenreRepo;
        private readonly Mock<IRepository<Store>> _mockStoreRepo;
        private readonly Mock<ILogger<MusicReleaseMapperService>> _mockLogger;
        private readonly MusicReleaseMapperService _service;

        public MusicReleaseMapperServiceTests()
        {
            _mockArtistRepo = new Mock<IRepository<Artist>>();
            _mockGenreRepo = new Mock<IRepository<Genre>>();
            _mockStoreRepo = new Mock<IRepository<Store>>();
            _mockLogger = new Mock<ILogger<MusicReleaseMapperService>>();

            _service = new MusicReleaseMapperService(
                _mockArtistRepo.Object,
                _mockGenreRepo.Object,
                _mockStoreRepo.Object,
                _mockLogger.Object
            );
        }

        #region MapToSummaryDto Tests

        [Fact]
        public void MapToSummaryDto_WithBasicData_ReturnsSummaryDto()
        {
            // Arrange
            var musicRelease = new MusicRelease
            {
                Id = 1,
                Title = "Test Album",
                ReleaseYear = new DateTime(2020, 1, 1),
                Artists = "[1,2]",
                Genres = "[1]",
                LabelId = 1,
                DateAdded = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            _mockArtistRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new Artist { Id = 1, Name = "Artist 1" });
            _mockArtistRepo.Setup(r => r.GetByIdAsync(2))
                .ReturnsAsync(new Artist { Id = 2, Name = "Artist 2" });
            _mockGenreRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new Genre { Id = 1, Name = "Rock" });

            // Act
            var result = _service.MapToSummaryDto(musicRelease);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("Test Album", result.Title);
            Assert.Equal(new DateTime(2020, 1, 1), result.ReleaseYear);
        }

        [Fact]
        public void MapToSummaryDto_WithNullArtists_HandlesGracefully()
        {
            // Arrange
            var musicRelease = new MusicRelease
            {
                Id = 1,
                Title = "Test Album",
                ReleaseYear = new DateTime(2020, 1, 1),
                Artists = null,
                Genres = null,
                DateAdded = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            // Act
            var result = _service.MapToSummaryDto(musicRelease);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Null(result.ArtistNames);
            Assert.Null(result.GenreNames);
        }

        [Fact]
        public void MapToSummaryDto_WithEmptyArtistsString_HandlesGracefully()
        {
            // Arrange
            var musicRelease = new MusicRelease
            {
                Id = 1,
                Title = "Test Album",
                ReleaseYear = new DateTime(2020, 1, 1),
                Artists = "",
                Genres = "",
                DateAdded = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            // Act
            var result = _service.MapToSummaryDto(musicRelease);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.ArtistNames);
            Assert.Null(result.GenreNames);
        }

        [Fact]
        public void MapToSummaryDto_WithImages_MapsCoverImageUrl()
        {
            // Arrange
            var images = new MusicReleaseImageDto
            {
                Thumbnail = "thumb.jpg",
                CoverFront = "cover.jpg"
            };

            var musicRelease = new MusicRelease
            {
                Id = 1,
                Title = "Test Album",
                ReleaseYear = new DateTime(2020, 1, 1),
                Images = JsonSerializer.Serialize(images),
                DateAdded = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            // Act
            var result = _service.MapToSummaryDto(musicRelease);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("cover.jpg", result.CoverImageUrl);
        }

        #endregion

        #region MapToFullDtoAsync Tests

        [Fact]
        public async Task MapToFullDtoAsync_WithCompleteData_ReturnsFullDto()
        {
            // Arrange
            var musicRelease = new MusicRelease
            {
                Id = 1,
                Title = "Test Album",
                ReleaseYear = new DateTime(2020, 1, 1),
                Artists = "[1]",
                Genres = "[1]",
                LabelId = 1,
                CountryId = 1,
                FormatId = 1,
                Label = new Label { Id = 1, Name = "Test Label" },
                Country = new Country { Id = 1, Name = "UK" },
                Format = new Format { Id = 1, Name = "CD" },
                DateAdded = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            _mockArtistRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new Artist { Id = 1, Name = "Test Artist" });
            _mockGenreRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new Genre { Id = 1, Name = "Rock" });

            // Act
            var result = await _service.MapToFullDtoAsync(musicRelease);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("Test Album", result.Title);
            Assert.NotNull(result.Label);
            Assert.Equal("Test Label", result.Label.Name);
            Assert.NotNull(result.Country);
            Assert.Equal("UK", result.Country.Name);
            Assert.NotNull(result.Format);
            Assert.Equal("CD", result.Format.Name);
        }

        [Fact]
        public async Task MapToFullDtoAsync_WithPurchaseInfo_MapsPurchaseInfo()
        {
            // Arrange
            var purchaseInfo = new PurchaseInfo
            {
                StoreID = 1,
                Price = 12.99m,
                Date = new DateTime(2023, 1, 1),
                Notes = "Test purchase"
            };

            var musicRelease = new MusicRelease
            {
                Id = 1,
                Title = "Test Album",
                ReleaseYear = new DateTime(2020, 1, 1),
                PurchaseInfo = JsonSerializer.Serialize(purchaseInfo),
                DateAdded = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            var store = new Store { Id = 1, Name = "Test Store" };
            _mockStoreRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(store);

            // Act
            var result = await _service.MapToFullDtoAsync(musicRelease);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.PurchaseInfo);
            Assert.Equal(1, result.PurchaseInfo.StoreId);
            Assert.Equal("Test Store", result.PurchaseInfo.StoreName);
            Assert.Equal(12.99m, result.PurchaseInfo.Price);
            Assert.Equal(new DateTime(2023, 1, 1), result.PurchaseInfo.PurchaseDate);
            Assert.Equal("Test purchase", result.PurchaseInfo.Notes);
        }

        [Fact]
        public async Task MapToFullDtoAsync_WithNullPurchaseInfo_HandlesGracefully()
        {
            // Arrange
            var musicRelease = new MusicRelease
            {
                Id = 1,
                Title = "Test Album",
                ReleaseYear = new DateTime(2020, 1, 1),
                PurchaseInfo = null,
                DateAdded = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            // Act
            var result = await _service.MapToFullDtoAsync(musicRelease);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.PurchaseInfo);
        }

        [Fact]
        public async Task MapToFullDtoAsync_WithMedia_MapsMediaTracks()
        {
            // Arrange
            var mediaInput = new List<MusicReleaseMediaDto>
            {
                new MusicReleaseMediaDto
                {
                    Name = "CD 1",
                    Tracks = new List<MusicReleaseTrackDto>
                    {
                        new MusicReleaseTrackDto { Title = "Track 1", Index = 1, LengthSecs = 180 }
                    }
                }
            };

            var musicRelease = new MusicRelease
            {
                Id = 1,
                Title = "Test Album",
                ReleaseYear = new DateTime(2020, 1, 1),
                Media = JsonSerializer.Serialize(mediaInput),
                DateAdded = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            // Act
            var result = await _service.MapToFullDtoAsync(musicRelease);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Media);
            var media = result.Media!;
            Assert.Single(media);
            Assert.Equal("CD 1", media[0].Name);
            Assert.NotNull(media[0].Tracks);
            var tracks = media[0].Tracks!;
            Assert.Single(tracks);
            Assert.Equal("Track 1", tracks[0].Title);
            Assert.Equal(180, tracks[0].LengthSecs);
        }

        [Fact]
        public async Task MapToFullDtoAsync_WithMediaArtists_ResolvesArtistNames()
        {
            // Arrange
            var mediaInput = new List<MusicReleaseMediaDto>
            {
                new MusicReleaseMediaDto
                {
                    Name = "CD 1",
                    Tracks = new List<MusicReleaseTrackDto>
                    {
                        new MusicReleaseTrackDto 
                        { 
                            Title = "Track 1",
                            Index = 1,
                            Artists = new List<string> { "1" }
                        }
                    }
                }
            };

            var musicRelease = new MusicRelease
            {
                Id = 1,
                Title = "Test Album",
                ReleaseYear = new DateTime(2020, 1, 1),
                Media = JsonSerializer.Serialize(mediaInput),
                DateAdded = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            _mockArtistRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new Artist { Id = 1, Name = "Track Artist" });

            // Act
            var result = await _service.MapToFullDtoAsync(musicRelease);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Media);
            var media = result.Media!;
            Assert.Single(media);
            Assert.NotNull(media[0].Tracks);
            var tracks = media[0].Tracks!;
            Assert.Single(tracks);
            Assert.NotNull(tracks[0].Artists);
            var artists = tracks[0].Artists!;
            Assert.Single(artists);
            Assert.Equal("Track Artist", artists[0]);
        }

        [Fact]
        public async Task MapToFullDtoAsync_WithMediaGenres_ResolvesGenreNames()
        {
            // Arrange
            var mediaInput = new List<MusicReleaseMediaDto>
            {
                new MusicReleaseMediaDto
                {
                    Name = "CD 1",
                    Tracks = new List<MusicReleaseTrackDto>
                    {
                        new MusicReleaseTrackDto 
                        { 
                            Title = "Track 1",
                            Index = 1,
                            Genres = new List<string> { "1", "2" }
                        }
                    }
                }
            };

            var musicRelease = new MusicRelease
            {
                Id = 1,
                Title = "Test Album",
                ReleaseYear = new DateTime(2020, 1, 1),
                Media = JsonSerializer.Serialize(mediaInput),
                DateAdded = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            _mockGenreRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new Genre { Id = 1, Name = "Rock" });
            _mockGenreRepo.Setup(r => r.GetByIdAsync(2))
                .ReturnsAsync(new Genre { Id = 2, Name = "Pop" });

            // Act
            var result = await _service.MapToFullDtoAsync(musicRelease);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Media);
            var media = result.Media!;
            Assert.Single(media);
            Assert.NotNull(media[0].Tracks);
            var tracks = media[0].Tracks!;
            Assert.Single(tracks);
            Assert.NotNull(tracks[0].Genres);
            var genres = tracks[0].Genres!;
            Assert.Equal(2, genres.Count);
            Assert.Contains("Rock", genres);
            Assert.Contains("Pop", genres);
        }

        [Fact]
        public async Task MapToFullDtoAsync_WithNullMedia_HandlesGracefully()
        {
            // Arrange
            var musicRelease = new MusicRelease
            {
                Id = 1,
                Title = "Test Album",
                ReleaseYear = new DateTime(2020, 1, 1),
                Media = null,
                DateAdded = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            // Act
            var result = await _service.MapToFullDtoAsync(musicRelease);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Media);
        }

        [Fact]
        public async Task MapToFullDtoAsync_WithAllFields_MapsAllFields()
        {
            // Arrange
            var musicRelease = new MusicRelease
            {
                Id = 1,
                Title = "Complete Album",
                ReleaseYear = new DateTime(2020, 1, 1),
                Artists = "[1]",
                Genres = "[1]",
                LabelId = 1,
                Label = new Label { Id = 1, Name = "Label" },
                CountryId = 1,
                Country = new Country { Id = 1, Name = "UK" },
                FormatId = 1,
                Format = new Format { Id = 1, Name = "CD" },
                PackagingId = 1,
                Packaging = new Packaging { Id = 1, Name = "Jewel Case" },
                LabelNumber = "CAT001",
                Upc = "123456789",
                DateAdded = DateTime.UtcNow.AddDays(-10),
                LastModified = DateTime.UtcNow
            };

            _mockArtistRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new Artist { Id = 1, Name = "Artist" });
            _mockGenreRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new Genre { Id = 1, Name = "Rock" });

            // Act
            var result = await _service.MapToFullDtoAsync(musicRelease);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("Complete Album", result.Title);
            Assert.NotNull(result.Packaging);
            Assert.Equal(1, result.Packaging.Id);
            Assert.Equal("Jewel Case", result.Packaging.Name);
            Assert.Equal("CAT001", result.LabelNumber);
            Assert.Equal("123456789", result.Upc);
            Assert.NotNull(result.Label);
            Assert.Equal("Label", result.Label.Name);
            Assert.NotNull(result.Format);
            Assert.Equal("CD", result.Format.Name);
            Assert.NotNull(result.Country);
            Assert.Equal("UK", result.Country.Name);
        }

        #endregion
    }
}
