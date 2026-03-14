using KollectorScum.Api.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KollectorScum.Tests.Services
{
    /// <summary>
    /// Unit tests for DiscogsResponseMapper
    /// Tests JSON deserialization and mapping of Discogs API responses to DTOs
    /// </summary>
    public class DiscogsResponseMapperTests
    {
        private readonly DiscogsResponseMapper _mapper;

        public DiscogsResponseMapperTests()
        {
            var logger = new Mock<ILogger<DiscogsResponseMapper>>();
            _mapper = new DiscogsResponseMapper(logger.Object);
        }

        [Fact]
        public void MapSearchResults_WithValidJson_ReturnsMappedResults()
        {
            // Arrange
            var json = """
                {
                  "results": [
                    {
                      "id": 12345,
                      "title": "Test Artist - Test Album",
                      "year": "2020",
                      "format": ["CD"],
                      "label": ["Test Label"],
                      "catno": "TEST001",
                      "country": "UK",
                      "thumb": "https://i.discogs.com/thumb.jpg",
                      "cover_image": "https://i.discogs.com/cover.jpg",
                      "resource_url": "https://api.discogs.com/releases/12345"
                    }
                  ]
                }
                """;

            // Act
            var results = _mapper.MapSearchResults(json);

            // Assert
            Assert.Single(results);
            var result = results[0];
            Assert.Equal("12345", result.Id);
            Assert.Equal("Test Artist - Test Album", result.Title);
            Assert.Equal("2020", result.Year);
            Assert.Equal("CD", result.Format);
            Assert.Equal("Test Label", result.Label);
            Assert.Equal("TEST001", result.CatalogNumber);
            Assert.Equal("UK", result.Country);
            Assert.Equal("https://i.discogs.com/thumb.jpg", result.ThumbUrl);
            Assert.Equal("https://i.discogs.com/cover.jpg", result.CoverImageUrl);
            Assert.Equal("https://api.discogs.com/releases/12345", result.ResourceUrl);
        }

        [Fact]
        public void MapSearchResults_WithCoverImage_MapsCoverImageUrl()
        {
            // Arrange - Verifies the snake_case cover_image field is deserialized correctly
            var json = """
                {
                  "results": [
                    {
                      "id": 99999,
                      "title": "Album With Cover",
                      "cover_image": "https://i.discogs.com/some-cover-art.jpg"
                    }
                  ]
                }
                """;

            // Act
            var results = _mapper.MapSearchResults(json);

            // Assert
            Assert.Single(results);
            Assert.Equal("https://i.discogs.com/some-cover-art.jpg", results[0].CoverImageUrl);
        }

        [Fact]
        public void MapSearchResults_WithThumb_MapsThumbUrl()
        {
            // Arrange
            var json = """
                {
                  "results": [
                    {
                      "id": 88888,
                      "title": "Album With Thumb",
                      "thumb": "https://i.discogs.com/some-thumb.jpg"
                    }
                  ]
                }
                """;

            // Act
            var results = _mapper.MapSearchResults(json);

            // Assert
            Assert.Single(results);
            Assert.Equal("https://i.discogs.com/some-thumb.jpg", results[0].ThumbUrl);
        }

        [Fact]
        public void MapSearchResults_WithResourceUrl_MapsResourceUrl()
        {
            // Arrange - Verifies the snake_case resource_url field is deserialized correctly
            var json = """
                {
                  "results": [
                    {
                      "id": 77777,
                      "title": "Some Release",
                      "resource_url": "https://api.discogs.com/releases/77777"
                    }
                  ]
                }
                """;

            // Act
            var results = _mapper.MapSearchResults(json);

            // Assert
            Assert.Single(results);
            Assert.Equal("https://api.discogs.com/releases/77777", results[0].ResourceUrl);
        }

        [Fact]
        public void MapSearchResults_WithNullJson_ReturnsEmptyList()
        {
            // Act
            var results = _mapper.MapSearchResults(null);

            // Assert
            Assert.Empty(results);
        }

        [Fact]
        public void MapSearchResults_WithEmptyJson_ReturnsEmptyList()
        {
            // Act
            var results = _mapper.MapSearchResults(string.Empty);

            // Assert
            Assert.Empty(results);
        }

        [Fact]
        public void MapSearchResults_WithNoResults_ReturnsEmptyList()
        {
            // Arrange
            var json = """{ "results": [] }""";

            // Act
            var results = _mapper.MapSearchResults(json);

            // Assert
            Assert.Empty(results);
        }

        [Fact]
        public void MapSearchResults_WithMultipleResults_ReturnsAllResults()
        {
            // Arrange
            var json = """
                {
                  "results": [
                    { "id": 1, "title": "Release One" },
                    { "id": 2, "title": "Release Two" },
                    { "id": 3, "title": "Release Three" }
                  ]
                }
                """;

            // Act
            var results = _mapper.MapSearchResults(json);

            // Assert
            Assert.Equal(3, results.Count);
            Assert.Equal("1", results[0].Id);
            Assert.Equal("2", results[1].Id);
            Assert.Equal("3", results[2].Id);
        }

        [Fact]
        public void MapSearchResults_WithInvalidJson_ReturnsEmptyList()
        {
            // Arrange
            var json = "{ invalid json }";

            // Act
            var results = _mapper.MapSearchResults(json);

            // Assert
            Assert.Empty(results);
        }

        [Fact]
        public void MapReleaseDetails_WithReleasedFormatted_MapsReleasedDate()
        {
            // Arrange - Verifies the snake_case released_formatted field is deserialized correctly
            var json = """
                {
                  "id": 12345,
                  "title": "Test Release",
                  "year": 2020,
                  "released_formatted": "15 Jan 2020",
                  "resource_url": "https://api.discogs.com/releases/12345",
                  "genres": [],
                  "styles": [],
                  "artists": [],
                  "labels": [],
                  "formats": [],
                  "images": [],
                  "tracklist": [],
                  "identifiers": []
                }
                """;

            // Act
            var result = _mapper.MapReleaseDetails(json);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("15 Jan 2020", result.ReleasedDate);
            Assert.Equal("https://api.discogs.com/releases/12345", result.ResourceUrl);
        }
    }
}
