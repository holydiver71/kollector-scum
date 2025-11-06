using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using KollectorScum.Api.Controllers;
using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KollectorScum.Tests.Controllers
{
    /// <summary>
    /// Unit tests for ArtistsController using refactored BaseApiController pattern
    /// </summary>
    public class ArtistsControllerTests
    {
        private readonly Mock<IGenericCrudService<Artist, ArtistDto>> _mockService;
        private readonly Mock<ILogger<ArtistsController>> _mockLogger;
        private readonly ArtistsController _controller;

        public ArtistsControllerTests()
        {
            _mockService = new Mock<IGenericCrudService<Artist, ArtistDto>>();
            _mockLogger = new Mock<ILogger<ArtistsController>>();
            _controller = new ArtistsController(_mockService.Object, _mockLogger.Object);
        }

        #region GetArtists Tests

        [Fact]
        public async Task GetArtists_ValidParameters_ReturnsOkWithPagedResults()
        {
            // Arrange
            var pagedResult = new PagedResult<ArtistDto>
            {
                Items = new List<ArtistDto>
                {
                    new ArtistDto { Id = 1, Name = "Artist 1" },
                    new ArtistDto { Id = 2, Name = "Artist 2" }
                },
                TotalCount = 2,
                Page = 1,
                PageSize = 50,
                TotalPages = 1
            };

            _mockService.Setup(s => s.GetAllAsync(1, 50, null, null, null))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetArtists(null, 1, 50);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<PagedResult<ArtistDto>>(okResult.Value);
            Assert.Equal(2, returnValue.TotalCount);
            Assert.Equal(2, returnValue.Items.Count());
        }

        [Fact]
        public async Task GetArtists_WithSearchTerm_ReturnsFilteredResults()
        {
            // Arrange
            var pagedResult = new PagedResult<ArtistDto>
            {
                Items = new List<ArtistDto>
                {
                    new ArtistDto { Id = 1, Name = "The Beatles" }
                },
                TotalCount = 1,
                Page = 1,
                PageSize = 50,
                TotalPages = 1
            };

            _mockService.Setup(s => s.GetAllAsync(1, 50, "beatles", null, null))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetArtists("beatles", 1, 50);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<PagedResult<ArtistDto>>(okResult.Value);
            Assert.Equal(1, returnValue.TotalCount);
            Assert.Single(returnValue.Items);
        }

        [Fact]
        public async Task GetArtists_InvalidPage_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetArtists(null, 0, 50);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Page must be greater than 0", badRequestResult.Value);
            _mockService.Verify(s => s.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), null, null), Times.Never);
        }

        [Fact]
        public async Task GetArtists_InvalidPageSize_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetArtists(null, 1, 0);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("Page size must be between", badRequestResult.Value?.ToString());
            _mockService.Verify(s => s.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), null, null), Times.Never);
        }

        [Fact]
        public async Task GetArtists_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            _mockService.Setup(s => s.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), null, null))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetArtists(null, 1, 50);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        #endregion

        #region GetArtist Tests

        [Fact]
        public async Task GetArtist_ExistingId_ReturnsOkWithArtist()
        {
            // Arrange
            var artistDto = new ArtistDto { Id = 1, Name = "Test Artist" };
            _mockService.Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(artistDto);

            // Act
            var result = await _controller.GetArtist(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<ArtistDto>(okResult.Value);
            Assert.Equal(1, returnValue.Id);
            Assert.Equal("Test Artist", returnValue.Name);
        }

        [Fact]
        public async Task GetArtist_NonExistingId_ReturnsNotFound()
        {
            // Arrange
            _mockService.Setup(s => s.GetByIdAsync(999))
                .ReturnsAsync((ArtistDto?)null);

            // Act
            var result = await _controller.GetArtist(999);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Contains("999", notFoundResult.Value?.ToString());
        }

        [Fact]
        public async Task GetArtist_InvalidId_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetArtist(0);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("must be greater than 0", badRequestResult.Value?.ToString());
            _mockService.Verify(s => s.GetByIdAsync(It.IsAny<int>()), Times.Never);
        }

        #endregion

        #region CreateArtist Tests

        [Fact]
        public async Task CreateArtist_ValidDto_ReturnsCreatedAtAction()
        {
            // Arrange
            var createDto = new ArtistDto { Name = "New Artist" };
            var createdDto = new ArtistDto { Id = 1, Name = "New Artist" };

            _mockService.Setup(s => s.CreateAsync(createDto))
                .ReturnsAsync(createdDto);

            // Act
            var result = await _controller.CreateArtist(createDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(nameof(_controller.GetArtist), createdResult.ActionName);
            var returnValue = Assert.IsType<ArtistDto>(createdResult.Value);
            Assert.Equal(1, returnValue.Id);
            Assert.Equal("New Artist", returnValue.Name);
        }

        [Fact]
        public async Task CreateArtist_InvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            var createDto = new ArtistDto { Name = "New Artist" };
            _controller.ModelState.AddModelError("Name", "Name is required");

            // Act
            var result = await _controller.CreateArtist(createDto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
            _mockService.Verify(s => s.CreateAsync(It.IsAny<ArtistDto>()), Times.Never);
        }

        [Fact]
        public async Task CreateArtist_ServiceThrowsArgumentException_ReturnsBadRequest()
        {
            // Arrange
            var createDto = new ArtistDto { Name = "" };
            _mockService.Setup(s => s.CreateAsync(createDto))
                .ThrowsAsync(new ArgumentException("Artist name is required"));

            // Act
            var result = await _controller.CreateArtist(createDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("Artist name is required", badRequestResult.Value?.ToString());
        }

        #endregion

        #region UpdateArtist Tests

        [Fact]
        public async Task UpdateArtist_ValidDto_ReturnsOkWithUpdatedArtist()
        {
            // Arrange
            var updateDto = new ArtistDto { Id = 1, Name = "Updated Artist" };
            var updatedDto = new ArtistDto { Id = 1, Name = "Updated Artist" };

            _mockService.Setup(s => s.UpdateAsync(1, updateDto))
                .ReturnsAsync(updatedDto);

            // Act
            var result = await _controller.UpdateArtist(1, updateDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<ArtistDto>(okResult.Value);
            Assert.Equal(1, returnValue.Id);
            Assert.Equal("Updated Artist", returnValue.Name);
        }

        [Fact]
        public async Task UpdateArtist_NonExistingId_ReturnsNotFound()
        {
            // Arrange
            var updateDto = new ArtistDto { Id = 999, Name = "Updated Artist" };
            _mockService.Setup(s => s.UpdateAsync(999, updateDto))
                .ReturnsAsync((ArtistDto?)null);

            // Act
            var result = await _controller.UpdateArtist(999, updateDto);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Contains("999", notFoundResult.Value?.ToString());
        }

        [Fact]
        public async Task UpdateArtist_InvalidId_ReturnsBadRequest()
        {
            // Arrange
            var updateDto = new ArtistDto { Id = 1, Name = "Updated Artist" };

            // Act
            var result = await _controller.UpdateArtist(0, updateDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("must be greater than 0", badRequestResult.Value?.ToString());
            _mockService.Verify(s => s.UpdateAsync(It.IsAny<int>(), It.IsAny<ArtistDto>()), Times.Never);
        }

        [Fact]
        public async Task UpdateArtist_InvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            var updateDto = new ArtistDto { Id = 1, Name = "Updated Artist" };
            _controller.ModelState.AddModelError("Name", "Name is invalid");

            // Act
            var result = await _controller.UpdateArtist(1, updateDto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
            _mockService.Verify(s => s.UpdateAsync(It.IsAny<int>(), It.IsAny<ArtistDto>()), Times.Never);
        }

        [Fact]
        public async Task UpdateArtist_ServiceThrowsArgumentException_ReturnsBadRequest()
        {
            // Arrange
            var updateDto = new ArtistDto { Id = 1, Name = "" };
            _mockService.Setup(s => s.UpdateAsync(1, updateDto))
                .ThrowsAsync(new ArgumentException("Artist name is required"));

            // Act
            var result = await _controller.UpdateArtist(1, updateDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("Artist name is required", badRequestResult.Value?.ToString());
        }

        #endregion

        #region DeleteArtist Tests

        [Fact]
        public async Task DeleteArtist_ExistingId_ReturnsNoContent()
        {
            // Arrange
            _mockService.Setup(s => s.DeleteAsync(1))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteArtist(1);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteArtist_NonExistingId_ReturnsNotFound()
        {
            // Arrange
            _mockService.Setup(s => s.DeleteAsync(999))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteArtist(999);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("999", notFoundResult.Value?.ToString());
        }

        [Fact]
        public async Task DeleteArtist_InvalidId_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.DeleteArtist(0);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("must be greater than 0", badRequestResult.Value?.ToString());
            _mockService.Verify(s => s.DeleteAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task DeleteArtist_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            _mockService.Setup(s => s.DeleteAsync(1))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.DeleteArtist(1);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        #endregion

        #region Constructor Tests

        [Fact]
        public void Constructor_NullService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new ArtistsController(null!, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_NullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new ArtistsController(_mockService.Object, null!));
        }

        #endregion
    }
}
