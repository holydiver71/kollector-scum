using System.Net;
using System.Text.Json;
using KollectorScum.Api.DTOs;
using KollectorScum.Api.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Xunit;

namespace KollectorScum.Tests.Services
{
    /// <summary>
    /// Unit tests for DiscogsService
    /// </summary>
    public class DiscogsServiceTests
    {
        private readonly Mock<ILogger<DiscogsService>> _mockLogger;
        private readonly DiscogsSettings _settings;

        public DiscogsServiceTests()
        {
            _mockLogger = new Mock<ILogger<DiscogsService>>();
            _settings = new DiscogsSettings
            {
                BaseUrl = "https://api.discogs.com",
                Token = "test-token",
                UserAgent = "KollectorScum/1.0 (https://github.com/test)",
                TimeoutSeconds = 30,
                RateLimitPerMinute = 60
            };
        }

        [Fact]
        public async Task SearchByCatalogNumberAsync_WithValidCatalogNumber_ReturnsResults()
        {
            // Arrange
            var mockResponse = new
            {
                results = new[]
                {
                    new
                    {
                        id = 123456L,
                        title = "Test Album",
                        artist = new[] { "Test Artist" },
                        year = "2020",
                        format = new[] { "CD" },
                        label = new[] { "Test Label" },
                        catno = "TEST001",
                        country = "UK",
                        thumb = "http://example.com/thumb.jpg",
                        cover_image = "http://example.com/cover.jpg",
                        resource_url = "http://api.discogs.com/releases/123456"
                    }
                }
            };

            var httpMessageHandler = CreateMockHttpMessageHandler(
                HttpStatusCode.OK,
                JsonSerializer.Serialize(mockResponse));

            var httpClient = new HttpClient(httpMessageHandler.Object);
            var service = new DiscogsService(
                httpClient,
                Options.Create(_settings),
                _mockLogger.Object);

            // Act
            var results = await service.SearchByCatalogNumberAsync("TEST001");

            // Assert
            Assert.NotNull(results);
            Assert.Single(results);
            Assert.Equal("123456", results[0].Id);
            Assert.Equal("Test Album", results[0].Title);
            Assert.Equal("Test Artist", results[0].Artist);
            Assert.Equal("2020", results[0].Year);
            Assert.Equal("CD", results[0].Format);
            Assert.Equal("Test Label", results[0].Label);
            Assert.Equal("TEST001", results[0].CatalogNumber);
        }

        [Fact]
        public async Task SearchByCatalogNumberAsync_WithFilters_IncludesFiltersInRequest()
        {
            // Arrange
            var mockResponse = new { results = new object[] { } };
            var capturedUri = string.Empty;

            var httpMessageHandler = new Mock<HttpMessageHandler>();
            httpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((request, _) =>
                {
                    capturedUri = request.RequestUri?.ToString() ?? string.Empty;
                })
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(mockResponse))
                });

            var httpClient = new HttpClient(httpMessageHandler.Object)
            {
                BaseAddress = new Uri(_settings.BaseUrl)
            };

            var service = new DiscogsService(
                httpClient,
                Options.Create(_settings),
                _mockLogger.Object);

            // Act
            await service.SearchByCatalogNumberAsync("TEST001", "CD", "UK", 2020);

            // Assert
            Assert.Contains("catno=TEST001", capturedUri);
            Assert.Contains("format=CD", capturedUri);
            Assert.Contains("country=UK", capturedUri);
            Assert.Contains("year=2020", capturedUri);
        }

        [Fact]
        public async Task SearchByCatalogNumberAsync_WithNoResults_ReturnsEmptyList()
        {
            // Arrange
            var mockResponse = new { results = new object[] { } };
            var httpMessageHandler = CreateMockHttpMessageHandler(
                HttpStatusCode.OK,
                JsonSerializer.Serialize(mockResponse));

            var httpClient = new HttpClient(httpMessageHandler.Object);
            var service = new DiscogsService(
                httpClient,
                Options.Create(_settings),
                _mockLogger.Object);

            // Act
            var results = await service.SearchByCatalogNumberAsync("NOTFOUND");

            // Assert
            Assert.NotNull(results);
            Assert.Empty(results);
        }

        [Fact]
        public async Task SearchByCatalogNumberAsync_WithApiError_ReturnsEmptyList()
        {
            // Arrange
            var httpMessageHandler = CreateMockHttpMessageHandler(
                HttpStatusCode.InternalServerError,
                "Internal Server Error");

            var httpClient = new HttpClient(httpMessageHandler.Object);
            var service = new DiscogsService(
                httpClient,
                Options.Create(_settings),
                _mockLogger.Object);

            // Act
            var results = await service.SearchByCatalogNumberAsync("TEST001");

            // Assert
            Assert.NotNull(results);
            Assert.Empty(results);
        }

        [Fact]
        public async Task GetReleaseDetailsAsync_WithValidId_ReturnsReleaseDetails()
        {
            // Arrange
            var mockResponse = new
            {
                id = 123456L,
                title = "Test Album",
                year = 2020,
                country = "UK",
                released_formatted = "2020-01-15",
                resource_url = "http://api.discogs.com/releases/123456",
                uri = "http://discogs.com/release/123456",
                notes = "Test notes",
                genres = new[] { "Rock", "Metal" },
                styles = new[] { "Heavy Metal" },
                artists = new[]
                {
                    new { name = "Test Artist", id = 789L, resource_url = "http://api.discogs.com/artists/789" }
                },
                labels = new[]
                {
                    new { name = "Test Label", catno = "TEST001", id = 456L, resource_url = "http://api.discogs.com/labels/456" }
                },
                formats = new[]
                {
                    new { name = "CD", qty = "1", descriptions = new[] { "Album" } }
                },
                images = new[]
                {
                    new { type = "primary", uri = "http://example.com/image.jpg", resource_url = "http://example.com/image.jpg", width = 600, height = 600 }
                },
                tracklist = new[]
                {
                    new { position = "1", title = "Track 1", duration = "3:45" }
                },
                identifiers = new[]
                {
                    new { type = "Barcode", value = "123456789" }
                }
            };

            var httpMessageHandler = CreateMockHttpMessageHandler(
                HttpStatusCode.OK,
                JsonSerializer.Serialize(mockResponse));

            var httpClient = new HttpClient(httpMessageHandler.Object);
            var service = new DiscogsService(
                httpClient,
                Options.Create(_settings),
                _mockLogger.Object);

            // Act
            var result = await service.GetReleaseDetailsAsync("123456");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("123456", result.Id);
            Assert.Equal("Test Album", result.Title);
            Assert.Equal(2020, result.Year);
            Assert.Equal("UK", result.Country);
            Assert.Equal(2, result.Genres.Count);
            Assert.Contains("Rock", result.Genres);
            Assert.Single(result.Artists);
            Assert.Equal("Test Artist", result.Artists[0].Name);
            Assert.Single(result.Labels);
            Assert.Equal("Test Label", result.Labels[0].Name);
            Assert.Single(result.Tracklist);
            Assert.Equal("Track 1", result.Tracklist[0].Title);
        }

        [Fact]
        public async Task GetReleaseDetailsAsync_WithNotFound_ReturnsNull()
        {
            // Arrange
            var httpMessageHandler = CreateMockHttpMessageHandler(
                HttpStatusCode.NotFound,
                "Not Found");

            var httpClient = new HttpClient(httpMessageHandler.Object);
            var service = new DiscogsService(
                httpClient,
                Options.Create(_settings),
                _mockLogger.Object);

            // Act
            var result = await service.GetReleaseDetailsAsync("999999");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetReleaseDetailsAsync_WithApiError_ReturnsNull()
        {
            // Arrange
            var httpMessageHandler = CreateMockHttpMessageHandler(
                HttpStatusCode.InternalServerError,
                "Internal Server Error");

            var httpClient = new HttpClient(httpMessageHandler.Object);
            var service = new DiscogsService(
                httpClient,
                Options.Create(_settings),
                _mockLogger.Object);

            // Act
            var result = await service.GetReleaseDetailsAsync("123456");

            // Assert
            Assert.Null(result);
        }

        // Edge case tests

        [Fact]
        public async Task SearchByCatalogNumberAsync_WithMalformedJson_ReturnsEmptyList()
        {
            // Arrange
            var httpMessageHandler = CreateMockHttpMessageHandler(
                HttpStatusCode.OK,
                "{ invalid json }");

            var httpClient = new HttpClient(httpMessageHandler.Object);
            var service = new DiscogsService(
                httpClient,
                Options.Create(_settings),
                _mockLogger.Object);

            // Act
            var results = await service.SearchByCatalogNumberAsync("TEST001");

            // Assert
            Assert.NotNull(results);
            Assert.Empty(results);
        }

        [Fact]
        public async Task SearchByCatalogNumberAsync_WithMissingResultsProperty_ReturnsEmptyList()
        {
            // Arrange
            var mockResponse = new { data = new object[] { } }; // Missing "results"
            var httpMessageHandler = CreateMockHttpMessageHandler(
                HttpStatusCode.OK,
                JsonSerializer.Serialize(mockResponse));

            var httpClient = new HttpClient(httpMessageHandler.Object);
            var service = new DiscogsService(
                httpClient,
                Options.Create(_settings),
                _mockLogger.Object);

            // Act
            var results = await service.SearchByCatalogNumberAsync("TEST001");

            // Assert
            Assert.NotNull(results);
            Assert.Empty(results);
        }

        [Fact]
        public async Task SearchByCatalogNumberAsync_WithNullResultsArray_ReturnsEmptyList()
        {
            // Arrange
            var mockResponse = new { results = (object[]?)null };
            var httpMessageHandler = CreateMockHttpMessageHandler(
                HttpStatusCode.OK,
                JsonSerializer.Serialize(mockResponse));

            var httpClient = new HttpClient(httpMessageHandler.Object);
            var service = new DiscogsService(
                httpClient,
                Options.Create(_settings),
                _mockLogger.Object);

            // Act
            var results = await service.SearchByCatalogNumberAsync("TEST001");

            // Assert
            Assert.NotNull(results);
            Assert.Empty(results);
        }

        [Fact]
        public async Task SearchByCatalogNumberAsync_WithPartialData_MapsAvailableFields()
        {
            // Arrange
            var mockResponse = new
            {
                results = new[]
                {
                    new
                    {
                        id = 123456L,
                        title = "Test Album"
                        // Missing optional fields: artist, year, format, label, catno, country, thumb, cover_image
                    }
                }
            };

            var httpMessageHandler = CreateMockHttpMessageHandler(
                HttpStatusCode.OK,
                JsonSerializer.Serialize(mockResponse));

            var httpClient = new HttpClient(httpMessageHandler.Object);
            var service = new DiscogsService(
                httpClient,
                Options.Create(_settings),
                _mockLogger.Object);

            // Act
            var results = await service.SearchByCatalogNumberAsync("TEST001");

            // Assert
            Assert.NotNull(results);
            Assert.Single(results);
            Assert.Equal("123456", results[0].Id);
            Assert.Equal("Test Album", results[0].Title);
        }

        [Fact]
        public async Task GetReleaseDetailsAsync_WithMalformedJson_ReturnsNull()
        {
            // Arrange
            var httpMessageHandler = CreateMockHttpMessageHandler(
                HttpStatusCode.OK,
                "{ malformed json }");

            var httpClient = new HttpClient(httpMessageHandler.Object);
            var service = new DiscogsService(
                httpClient,
                Options.Create(_settings),
                _mockLogger.Object);

            // Act
            var result = await service.GetReleaseDetailsAsync("123456");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetReleaseDetailsAsync_WithMinimalData_MapsAvailableFields()
        {
            // Arrange
            var mockResponse = new
            {
                id = 123456L,
                title = "Minimal Album"
                // Missing optional fields
            };

            var httpMessageHandler = CreateMockHttpMessageHandler(
                HttpStatusCode.OK,
                JsonSerializer.Serialize(mockResponse));

            var httpClient = new HttpClient(httpMessageHandler.Object);
            var service = new DiscogsService(
                httpClient,
                Options.Create(_settings),
                _mockLogger.Object);

            // Act
            var result = await service.GetReleaseDetailsAsync("123456");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("123456", result.Id);
            Assert.Equal("Minimal Album", result.Title);
            Assert.NotNull(result.Genres);
            Assert.NotNull(result.Artists);
            Assert.NotNull(result.Labels);
        }

        [Fact]
        public async Task GetReleaseDetailsAsync_WithEmptyArrays_ReturnsEmptyCollections()
        {
            // Arrange
            var mockResponse = new
            {
                id = 123456L,
                title = "Test Album",
                artists = new object[] { },
                labels = new object[] { },
                formats = new object[] { },
                images = new object[] { },
                tracklist = new object[] { },
                identifiers = new object[] { },
                genres = new string[] { },
                styles = new string[] { }
            };

            var httpMessageHandler = CreateMockHttpMessageHandler(
                HttpStatusCode.OK,
                JsonSerializer.Serialize(mockResponse));

            var httpClient = new HttpClient(httpMessageHandler.Object);
            var service = new DiscogsService(
                httpClient,
                Options.Create(_settings),
                _mockLogger.Object);

            // Act
            var result = await service.GetReleaseDetailsAsync("123456");

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Artists);
            Assert.Empty(result.Labels);
            Assert.Empty(result.Formats);
            Assert.Empty(result.Images);
            Assert.Empty(result.Tracklist);
            Assert.Empty(result.Identifiers);
            Assert.Empty(result.Genres);
        }

        [Fact]
        public async Task SearchByCatalogNumberAsync_With401Unauthorized_ReturnsEmptyList()
        {
            // Arrange
            var httpMessageHandler = CreateMockHttpMessageHandler(
                HttpStatusCode.Unauthorized,
                "Unauthorized");

            var httpClient = new HttpClient(httpMessageHandler.Object);
            var service = new DiscogsService(
                httpClient,
                Options.Create(_settings),
                _mockLogger.Object);

            // Act
            var results = await service.SearchByCatalogNumberAsync("TEST001");

            // Assert
            Assert.NotNull(results);
            Assert.Empty(results);
        }

        [Fact]
        public async Task GetReleaseDetailsAsync_With429RateLimitExceeded_ReturnsNull()
        {
            // Arrange
            var httpMessageHandler = CreateMockHttpMessageHandler(
                (HttpStatusCode)429, // Too Many Requests
                "Rate limit exceeded");

            var httpClient = new HttpClient(httpMessageHandler.Object);
            var service = new DiscogsService(
                httpClient,
                Options.Create(_settings),
                _mockLogger.Object);

            // Act
            var result = await service.GetReleaseDetailsAsync("123456");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task SearchByCatalogNumberAsync_WithVeryLongCatalogNumber_CallsService()
        {
            // Arrange
            var longCatalogNumber = new string('A', 200);
            var mockResponse = new { results = new object[] { } };
            var httpMessageHandler = CreateMockHttpMessageHandler(
                HttpStatusCode.OK,
                JsonSerializer.Serialize(mockResponse));

            var httpClient = new HttpClient(httpMessageHandler.Object);
            var service = new DiscogsService(
                httpClient,
                Options.Create(_settings),
                _mockLogger.Object);

            // Act
            var results = await service.SearchByCatalogNumberAsync(longCatalogNumber);

            // Assert
            Assert.NotNull(results);
            Assert.Empty(results);
        }

        [Fact]
        public async Task SearchByCatalogNumberAsync_WithSpecialCharacters_EncodesCorrectly()
        {
            // Arrange
            var catalogNumber = "TEST&123/456";
            var mockResponse = new { results = new object[] { } };
            var capturedUri = string.Empty;

            var httpMessageHandler = new Mock<HttpMessageHandler>();
            httpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((request, _) =>
                {
                    capturedUri = request.RequestUri?.ToString() ?? string.Empty;
                })
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(mockResponse))
                });

            var httpClient = new HttpClient(httpMessageHandler.Object)
            {
                BaseAddress = new Uri(_settings.BaseUrl)
            };

            var service = new DiscogsService(
                httpClient,
                Options.Create(_settings),
                _mockLogger.Object);

            // Act
            await service.SearchByCatalogNumberAsync(catalogNumber);

            // Assert
            // URL encoding should convert & to %26 and / to %2F
            Assert.Contains("catno=", capturedUri);
        }

        private Mock<HttpMessageHandler> CreateMockHttpMessageHandler(
            HttpStatusCode statusCode,
            string content)
        {
            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(content)
                });

            return mockHandler;
        }
    }
}
