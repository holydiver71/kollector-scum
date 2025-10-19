using System.Collections.Generic;
using System.Threading.Tasks;
using KollectorScum.Api.Controllers;
using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KollectorScum.Tests.Controllers
{
    /// <summary>
    /// Unit tests for DiscogsController
    /// </summary>
    public class DiscogsControllerTests
    {
        private readonly Mock<IDiscogsService> _mockDiscogsService;
        private readonly Mock<ILogger<DiscogsController>> _mockLogger;
        private readonly DiscogsController _controller;

        public DiscogsControllerTests()
        {
            _mockDiscogsService = new Mock<IDiscogsService>();
            _mockLogger = new Mock<ILogger<DiscogsController>>();
            _controller = new DiscogsController(_mockDiscogsService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task SearchByCatalogNumber_WithEmptyCatalogNumber_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.SearchByCatalogNumber(string.Empty);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task SearchByCatalogNumber_WhenServiceReturnsResults_ReturnsOkWithResults()
        {
            // Arrange
            var sample = new List<DiscogsSearchResultDto>
            {
                new DiscogsSearchResultDto { Id = "1", Title = "Album", Artist = "Artist", CatalogNumber = "CAT1" }
            };
            _mockDiscogsService
                .Setup(s => s.SearchByCatalogNumberAsync("CAT1", null, null, null))
                .ReturnsAsync(sample);

            // Act
            var actionResult = await _controller.SearchByCatalogNumber("CAT1");

            // Assert
            var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            var value = Assert.IsType<List<DiscogsSearchResultDto>>(ok.Value);
            Assert.Single(value);
            Assert.Equal("CAT1", value[0].CatalogNumber);
        }

        [Fact]
        public async Task GetReleaseDetails_WithEmptyId_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetReleaseDetails(string.Empty);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetReleaseDetails_WhenNotFound_ReturnsNotFound()
        {
            // Arrange
            _mockDiscogsService
                .Setup(s => s.GetReleaseDetailsAsync("999"))
                .ReturnsAsync((DiscogsReleaseDto?)null);

            // Act
            var actionResult = await _controller.GetReleaseDetails("999");

            // Assert
            Assert.IsType<NotFoundObjectResult>(actionResult.Result);
        }

        [Fact]
        public async Task GetReleaseDetails_WhenFound_ReturnsOkWithRelease()
        {
            // Arrange
            var release = new DiscogsReleaseDto { Id = "123", Title = "Album" };
            _mockDiscogsService
                .Setup(s => s.GetReleaseDetailsAsync("123"))
                .ReturnsAsync(release);

            // Act
            var actionResult = await _controller.GetReleaseDetails("123");

            // Assert
            var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            var value = Assert.IsType<DiscogsReleaseDto>(ok.Value);
            Assert.Equal("123", value.Id);
        }

        // Edge case tests

        [Fact]
        public async Task SearchByCatalogNumber_WithNullCatalogNumber_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.SearchByCatalogNumber(null!);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task SearchByCatalogNumber_WithWhitespaceCatalogNumber_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.SearchByCatalogNumber("   ");

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task SearchByCatalogNumber_WhenServiceThrowsException_Returns500()
        {
            // Arrange
            _mockDiscogsService
                .Setup(s => s.SearchByCatalogNumberAsync(It.IsAny<string>(), null, null, null))
                .ThrowsAsync(new HttpRequestException("Network error"));

            // Act
            var actionResult = await _controller.SearchByCatalogNumber("CAT1");

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(actionResult.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task SearchByCatalogNumber_WhenServiceReturnsEmptyList_ReturnsOkWithEmptyList()
        {
            // Arrange
            _mockDiscogsService
                .Setup(s => s.SearchByCatalogNumberAsync("NOTFOUND", null, null, null))
                .ReturnsAsync(new List<DiscogsSearchResultDto>());

            // Act
            var actionResult = await _controller.SearchByCatalogNumber("NOTFOUND");

            // Assert
            var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            var value = Assert.IsType<List<DiscogsSearchResultDto>>(ok.Value);
            Assert.Empty(value);
        }

        [Fact]
        public async Task SearchByCatalogNumber_WithAllFilters_PassesFiltersToService()
        {
            // Arrange
            var sample = new List<DiscogsSearchResultDto>
            {
                new DiscogsSearchResultDto { Id = "1", Title = "Album", Artist = "Artist", CatalogNumber = "CAT1" }
            };
            _mockDiscogsService
                .Setup(s => s.SearchByCatalogNumberAsync("CAT1", "CD", "US", 2020))
                .ReturnsAsync(sample);

            // Act
            var actionResult = await _controller.SearchByCatalogNumber("CAT1", "CD", "US", 2020);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            var value = Assert.IsType<List<DiscogsSearchResultDto>>(ok.Value);
            Assert.Single(value);
            _mockDiscogsService.Verify(s => s.SearchByCatalogNumberAsync("CAT1", "CD", "US", 2020), Times.Once);
        }

        [Fact]
        public async Task GetReleaseDetails_WithNullId_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetReleaseDetails(null!);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetReleaseDetails_WithWhitespaceId_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetReleaseDetails("   ");

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetReleaseDetails_WhenServiceThrowsException_Returns500()
        {
            // Arrange
            _mockDiscogsService
                .Setup(s => s.GetReleaseDetailsAsync(It.IsAny<string>()))
                .ThrowsAsync(new HttpRequestException("Network error"));

            // Act
            var actionResult = await _controller.GetReleaseDetails("123");

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(actionResult.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task GetReleaseDetails_WithSpecialCharactersInId_CallsService()
        {
            // Arrange
            var release = new DiscogsReleaseDto { Id = "123-456", Title = "Album" };
            _mockDiscogsService
                .Setup(s => s.GetReleaseDetailsAsync("123-456"))
                .ReturnsAsync(release);

            // Act
            var actionResult = await _controller.GetReleaseDetails("123-456");

            // Assert
            var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            var value = Assert.IsType<DiscogsReleaseDto>(ok.Value);
            Assert.Equal("123-456", value.Id);
            _mockDiscogsService.Verify(s => s.GetReleaseDetailsAsync("123-456"), Times.Once);
        }
    }
}
