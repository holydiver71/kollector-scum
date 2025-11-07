using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KollectorScum.Tests.Services
{
    /// <summary>
    /// Unit tests for DiscogsService (orchestrator)
    /// Tests delegation to HttpClient and Mapper
    /// </summary>
    public class DiscogsServiceTests
    {
        private readonly Mock<IDiscogsHttpClient> _mockHttpClient;
        private readonly Mock<IDiscogsResponseMapper> _mockMapper;
        private readonly Mock<ILogger<DiscogsService>> _mockLogger;

        public DiscogsServiceTests()
        {
            _mockHttpClient = new Mock<IDiscogsHttpClient>();
            _mockMapper = new Mock<IDiscogsResponseMapper>();
            _mockLogger = new Mock<ILogger<DiscogsService>>();
        }

        private DiscogsService CreateService()
        {
            return new DiscogsService(
                _mockHttpClient.Object,
                _mockMapper.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task SearchByCatalogNumberAsync_WithValidCatalogNumber_ReturnsResults()
        {
            // Arrange
            var service = CreateService();
            var jsonResponse = "{}";
            var expectedResults = new List<DiscogsSearchResultDto>
            {
                new DiscogsSearchResultDto
                {
                    Id = "123456",
                    Title = "Test Album",
                    Artist = "Test Artist",
                    CatalogNumber = "TEST001"
                }
            };

            _mockHttpClient.Setup(c => c.SearchReleasesAsync("TEST001", null, null, null))
                .ReturnsAsync(jsonResponse);
            _mockMapper.Setup(m => m.MapSearchResults(jsonResponse))
                .Returns(expectedResults);

            // Act
            var results = await service.SearchByCatalogNumberAsync("TEST001");

            // Assert
            Assert.NotNull(results);
            Assert.Single(results);
            Assert.Equal("123456", results[0].Id);
            Assert.Equal("Test Album", results[0].Title);
            _mockHttpClient.Verify(c => c.SearchReleasesAsync("TEST001", null, null, null), Times.Once);
            _mockMapper.Verify(m => m.MapSearchResults(jsonResponse), Times.Once);
        }

        [Fact]
        public async Task SearchByCatalogNumberAsync_WithFilters_PassesFiltersToHttpClient()
        {
            // Arrange
            var service = CreateService();
            var jsonResponse = "{}";
            var results = new List<DiscogsSearchResultDto>();

            _mockHttpClient.Setup(c => c.SearchReleasesAsync("TEST001", "CD", "US", 2020))
                .ReturnsAsync(jsonResponse);
            _mockMapper.Setup(m => m.MapSearchResults(jsonResponse))
                .Returns(results);

            // Act
            await service.SearchByCatalogNumberAsync("TEST001", format: "CD", country: "US", year: 2020);

            // Assert
            _mockHttpClient.Verify(c => c.SearchReleasesAsync("TEST001", "CD", "US", 2020), Times.Once);
        }

        [Fact]
        public async Task SearchByCatalogNumberAsync_WhenHttpClientReturnsNull_ReturnsEmptyList()
        {
            // Arrange
            var service = CreateService();
            _mockHttpClient.Setup(c => c.SearchReleasesAsync(It.IsAny<string>(), null, null, null))
                .ReturnsAsync((string?)null);
            _mockMapper.Setup(m => m.MapSearchResults(null))
                .Returns(new List<DiscogsSearchResultDto>());

            // Act
            var results = await service.SearchByCatalogNumberAsync("TEST001");

            // Assert
            Assert.NotNull(results);
            Assert.Empty(results);
        }

        [Fact]
        public async Task SearchByCatalogNumberAsync_WhenHttpClientThrowsException_ThrowsException()
        {
            // Arrange
            var service = CreateService();
            _mockHttpClient.Setup(c => c.SearchReleasesAsync(It.IsAny<string>(), null, null, null))
                .ThrowsAsync(new Exception("Network error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(async () => 
                await service.SearchByCatalogNumberAsync("TEST001"));
        }

        [Fact]
        public async Task GetReleaseDetailsAsync_WithValidId_ReturnsReleaseDto()
        {
            // Arrange
            var service = CreateService();
            var jsonResponse = "{}";
            var expectedRelease = new DiscogsReleaseDto
            {
                Id = "123456",
                Title = "Test Album",
                Year = 2020
            };

            _mockHttpClient.Setup(c => c.GetReleaseDetailsAsync("123456"))
                .ReturnsAsync(jsonResponse);
            _mockMapper.Setup(m => m.MapReleaseDetails(jsonResponse))
                .Returns(expectedRelease);

            // Act
            var result = await service.GetReleaseDetailsAsync("123456");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("123456", result.Id);
            Assert.Equal("Test Album", result.Title);
            Assert.Equal(2020, result.Year);
            _mockHttpClient.Verify(c => c.GetReleaseDetailsAsync("123456"), Times.Once);
            _mockMapper.Verify(m => m.MapReleaseDetails(jsonResponse), Times.Once);
        }

        [Fact]
        public async Task GetReleaseDetailsAsync_WhenHttpClientReturnsNull_ReturnsNull()
        {
            // Arrange
            var service = CreateService();
            _mockHttpClient.Setup(c => c.GetReleaseDetailsAsync(It.IsAny<string>()))
                .ReturnsAsync((string?)null);
            _mockMapper.Setup(m => m.MapReleaseDetails(null))
                .Returns((DiscogsReleaseDto?)null);

            // Act
            var result = await service.GetReleaseDetailsAsync("123456");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetReleaseDetailsAsync_WhenMapperReturnsNull_ReturnsNull()
        {
            // Arrange
            var service = CreateService();
            _mockHttpClient.Setup(c => c.GetReleaseDetailsAsync(It.IsAny<string>()))
                .ReturnsAsync("{}");
            _mockMapper.Setup(m => m.MapReleaseDetails(It.IsAny<string>()))
                .Returns((DiscogsReleaseDto?)null);

            // Act
            var result = await service.GetReleaseDetailsAsync("123456");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetReleaseDetailsAsync_WhenHttpClientThrowsException_ThrowsException()
        {
            // Arrange
            var service = CreateService();
            _mockHttpClient.Setup(c => c.GetReleaseDetailsAsync(It.IsAny<string>()))
                .ThrowsAsync(new Exception("Network error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(async () => 
                await service.GetReleaseDetailsAsync("123456"));
        }

        [Fact]
        public void Constructor_WithNullHttpClient_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new DiscogsService(null!, _mockMapper.Object, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullMapper_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new DiscogsService(_mockHttpClient.Object, null!, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new DiscogsService(_mockHttpClient.Object, _mockMapper.Object, null!));
        }
    }
}

