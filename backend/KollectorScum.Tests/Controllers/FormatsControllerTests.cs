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
    /// Unit tests for FormatsController using refactored BaseApiController pattern
    /// </summary>
    public class FormatsControllerTests
    {
        private readonly Mock<IGenericCrudService<Format, FormatDto>> _mockService;
        private readonly Mock<ILogger<FormatsController>> _mockLogger;
        private readonly FormatsController _controller;

        public FormatsControllerTests()
        {
            _mockService = new Mock<IGenericCrudService<Format, FormatDto>>();
            _mockLogger = new Mock<ILogger<FormatsController>>();
            _controller = new FormatsController(_mockService.Object, _mockLogger.Object);
        }

        #region GetFormats Tests

        [Fact]
        public async Task GetFormats_ValidParameters_ReturnsOkWithPagedResults()
        {
            // Arrange
            var pagedResult = new PagedResult<FormatDto>
            {
                Items = new List<FormatDto>
                {
                    new FormatDto { Id = 1, Name = "Vinyl" },
                    new FormatDto { Id = 2, Name = "CD" }
                },
                TotalCount = 2,
                Page = 1,
                PageSize = 50,
                TotalPages = 1
            };

            _mockService.Setup(s => s.GetAllAsync(1, 50, null, null, null))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetFormats(null, 1, 50);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedResult = Assert.IsType<PagedResult<FormatDto>>(okResult.Value);
            Assert.Equal(2, returnedResult.Items.Count());
            Assert.Equal(2, returnedResult.TotalCount);
        }

        [Fact]
        public async Task GetFormats_WithSearchTerm_ReturnsFilteredResults()
        {
            // Arrange
            var pagedResult = new PagedResult<FormatDto>
            {
                Items = new List<FormatDto>
                {
                    new FormatDto { Id = 1, Name = "Vinyl" }
                },
                TotalCount = 1,
                Page = 1,
                PageSize = 50,
                TotalPages = 1
            };

            _mockService.Setup(s => s.GetAllAsync(1, 50, "Vinyl", null, null))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetFormats("Vinyl", 1, 50);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedResult = Assert.IsType<PagedResult<FormatDto>>(okResult.Value);
            Assert.Single(returnedResult.Items);
            Assert.Equal("Vinyl", returnedResult.Items.ToList()[0].Name);
        }

        [Fact]
        public async Task GetFormats_InvalidPage_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetFormats(null, 0, 50);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task GetFormats_InvalidPageSize_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetFormats(null, 1, 0);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task GetFormats_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            _mockService.Setup(s => s.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), null, null))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetFormats(null, 1, 50);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        #endregion

        #region GetFormat Tests

        [Fact]
        public async Task GetFormat_ExistingId_ReturnsOkWithFormat()
        {
            // Arrange
            var formatDto = new FormatDto { Id = 1, Name = "Vinyl" };
            _mockService.Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(formatDto);

            // Act
            var result = await _controller.GetFormat(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedFormat = Assert.IsType<FormatDto>(okResult.Value);
            Assert.Equal(1, returnedFormat.Id);
            Assert.Equal("Vinyl", returnedFormat.Name);
        }

        [Fact]
        public async Task GetFormat_NonExistingId_ReturnsNotFound()
        {
            // Arrange
            _mockService.Setup(s => s.GetByIdAsync(999))
                .ReturnsAsync((FormatDto?)null);

            // Act
            var result = await _controller.GetFormat(999);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.NotNull(notFoundResult.Value);
        }

        [Fact]
        public async Task GetFormat_InvalidId_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetFormat(0);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequestResult.Value);
        }

        #endregion

        #region CreateFormat Tests

        [Fact]
        public async Task CreateFormat_ValidFormat_ReturnsCreatedAtAction()
        {
            // Arrange
            var createDto = new FormatDto { Name = "Cassette" };
            var createdDto = new FormatDto { Id = 1, Name = "Cassette" };

            _mockService.Setup(s => s.CreateAsync(It.IsAny<FormatDto>()))
                .ReturnsAsync(createdDto);

            // Act
            var result = await _controller.CreateFormat(createDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returnedFormat = Assert.IsType<FormatDto>(createdResult.Value);
            Assert.Equal("Cassette", returnedFormat.Name);
            Assert.Equal(nameof(_controller.GetFormat), createdResult.ActionName);
        }

        [Fact]
        public async Task CreateFormat_InvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            _controller.ModelState.AddModelError("Name", "Name is required");

            // Act
            var result = await _controller.CreateFormat(new FormatDto());

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task CreateFormat_ServiceThrowsArgumentException_ReturnsBadRequest()
        {
            // Arrange
            var createDto = new FormatDto { Name = "Vinyl" };
            _mockService.Setup(s => s.CreateAsync(It.IsAny<FormatDto>()))
                .ThrowsAsync(new ArgumentException("Format already exists"));

            // Act
            var result = await _controller.CreateFormat(createDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequestResult.Value);
        }

        #endregion

        #region UpdateFormat Tests

        [Fact]
        public async Task UpdateFormat_ValidFormat_ReturnsOk()
        {
            // Arrange
            var updateDto = new FormatDto { Name = "Vinyl Updated" };
            var updatedDto = new FormatDto { Id = 1, Name = "Vinyl Updated" };

            _mockService.Setup(s => s.UpdateAsync(1, It.IsAny<FormatDto>()))
                .ReturnsAsync(updatedDto);

            // Act
            var result = await _controller.UpdateFormat(1, updateDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedFormat = Assert.IsType<FormatDto>(okResult.Value);
            Assert.Equal("Vinyl Updated", returnedFormat.Name);
        }

        [Fact]
        public async Task UpdateFormat_NonExistingId_ReturnsNotFound()
        {
            // Arrange
            var updateDto = new FormatDto { Name = "Vinyl Updated" };
            _mockService.Setup(s => s.UpdateAsync(999, It.IsAny<FormatDto>()))
                .ThrowsAsync(new KeyNotFoundException("Format not found"));

            // Act
            var result = await _controller.UpdateFormat(999, updateDto);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.NotNull(notFoundResult.Value);
        }

        [Fact]
        public async Task UpdateFormat_InvalidId_ReturnsBadRequest()
        {
            // Arrange
            var updateDto = new FormatDto { Name = "Vinyl" };

            // Act
            var result = await _controller.UpdateFormat(0, updateDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task UpdateFormat_InvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            _controller.ModelState.AddModelError("Name", "Name is required");

            // Act
            var result = await _controller.UpdateFormat(1, new FormatDto());

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task UpdateFormat_ServiceThrowsArgumentException_ReturnsBadRequest()
        {
            // Arrange
            var updateDto = new FormatDto { Name = "CD" };
            _mockService.Setup(s => s.UpdateAsync(1, It.IsAny<FormatDto>()))
                .ThrowsAsync(new ArgumentException("Duplicate format name"));

            // Act
            var result = await _controller.UpdateFormat(1, updateDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequestResult.Value);
        }

        #endregion

        #region DeleteFormat Tests

        [Fact]
        public async Task DeleteFormat_ExistingId_ReturnsNoContent()
        {
            // Arrange
            _mockService.Setup(s => s.DeleteAsync(1))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteFormat(1);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteFormat_NonExistingId_ReturnsNotFound()
        {
            // Arrange
            _mockService.Setup(s => s.DeleteAsync(999))
                .ThrowsAsync(new KeyNotFoundException("Format not found"));

            // Act
            var result = await _controller.DeleteFormat(999);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFoundResult.Value);
        }

        [Fact]
        public async Task DeleteFormat_InvalidId_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.DeleteFormat(0);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task DeleteFormat_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            _mockService.Setup(s => s.DeleteAsync(1))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.DeleteFormat(1);

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
                new FormatsController(null!, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_NullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new FormatsController(_mockService.Object, null!));
        }

        #endregion
    }
}
