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
    /// Unit tests for LabelsController using refactored BaseApiController pattern
    /// </summary>
    public class LabelsControllerTests
    {
        private readonly Mock<IGenericCrudService<Label, LabelDto>> _mockService;
        private readonly Mock<ILogger<LabelsController>> _mockLogger;
        private readonly LabelsController _controller;

        public LabelsControllerTests()
        {
            _mockService = new Mock<IGenericCrudService<Label, LabelDto>>();
            _mockLogger = new Mock<ILogger<LabelsController>>();
            _controller = new LabelsController(_mockService.Object, _mockLogger.Object);
        }

        #region GetLabels Tests

        [Fact]
        public async Task GetLabels_ValidParameters_ReturnsOkWithPagedResults()
        {
            // Arrange
            var pagedResult = new PagedResult<LabelDto>
            {
                Items = new List<LabelDto>
                {
                    new LabelDto { Id = 1, Name = "Blue Note" },
                    new LabelDto { Id = 2, Name = "Columbia" }
                },
                TotalCount = 2,
                Page = 1,
                PageSize = 50,
                TotalPages = 1
            };

            _mockService.Setup(s => s.GetAllAsync(1, 50, null, null, null))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetLabels(null, 1, 50);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedResult = Assert.IsType<PagedResult<LabelDto>>(okResult.Value);
            Assert.Equal(2, returnedResult.Items.Count());
            Assert.Equal(2, returnedResult.TotalCount);
        }

        [Fact]
        public async Task GetLabels_WithSearchTerm_ReturnsFilteredResults()
        {
            // Arrange
            var pagedResult = new PagedResult<LabelDto>
            {
                Items = new List<LabelDto>
                {
                    new LabelDto { Id = 1, Name = "Blue Note" }
                },
                TotalCount = 1,
                Page = 1,
                PageSize = 50,
                TotalPages = 1
            };

            _mockService.Setup(s => s.GetAllAsync(1, 50, "Blue", null, null))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetLabels("Blue", 1, 50);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedResult = Assert.IsType<PagedResult<LabelDto>>(okResult.Value);
            Assert.Single(returnedResult.Items);
            Assert.Equal("Blue Note", returnedResult.Items.ToList()[0].Name);
        }

        [Fact]
        public async Task GetLabels_InvalidPage_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetLabels(null, 0, 50);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task GetLabels_InvalidPageSize_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetLabels(null, 1, 0);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task GetLabels_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            _mockService.Setup(s => s.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), null, null))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetLabels(null, 1, 50);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        #endregion

        #region GetLabel Tests

        [Fact]
        public async Task GetLabel_ExistingId_ReturnsOkWithLabel()
        {
            // Arrange
            var labelDto = new LabelDto { Id = 1, Name = "Blue Note" };
            _mockService.Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(labelDto);

            // Act
            var result = await _controller.GetLabel(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedLabel = Assert.IsType<LabelDto>(okResult.Value);
            Assert.Equal(1, returnedLabel.Id);
            Assert.Equal("Blue Note", returnedLabel.Name);
        }

        [Fact]
        public async Task GetLabel_NonExistingId_ReturnsNotFound()
        {
            // Arrange
            _mockService.Setup(s => s.GetByIdAsync(999))
                .ReturnsAsync((LabelDto?)null);

            // Act
            var result = await _controller.GetLabel(999);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.NotNull(notFoundResult.Value);
        }

        [Fact]
        public async Task GetLabel_InvalidId_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetLabel(0);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequestResult.Value);
        }

        #endregion

        #region CreateLabel Tests

        [Fact]
        public async Task CreateLabel_ValidLabel_ReturnsCreatedAtAction()
        {
            // Arrange
            var createDto = new LabelDto { Name = "Verve Records" };
            var createdDto = new LabelDto { Id = 1, Name = "Verve Records" };

            _mockService.Setup(s => s.CreateAsync(It.IsAny<LabelDto>()))
                .ReturnsAsync(createdDto);

            // Act
            var result = await _controller.CreateLabel(createDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returnedLabel = Assert.IsType<LabelDto>(createdResult.Value);
            Assert.Equal("Verve Records", returnedLabel.Name);
            Assert.Equal(nameof(_controller.GetLabel), createdResult.ActionName);
        }

        [Fact]
        public async Task CreateLabel_InvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            _controller.ModelState.AddModelError("Name", "Name is required");

            // Act
            var result = await _controller.CreateLabel(new LabelDto());

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task CreateLabel_ServiceThrowsArgumentException_ReturnsBadRequest()
        {
            // Arrange
            var createDto = new LabelDto { Name = "Blue Note" };
            _mockService.Setup(s => s.CreateAsync(It.IsAny<LabelDto>()))
                .ThrowsAsync(new ArgumentException("Label already exists"));

            // Act
            var result = await _controller.CreateLabel(createDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequestResult.Value);
        }

        #endregion

        #region UpdateLabel Tests

        [Fact]
        public async Task UpdateLabel_ValidLabel_ReturnsOk()
        {
            // Arrange
            var updateDto = new LabelDto { Name = "Blue Note Updated" };
            var updatedDto = new LabelDto { Id = 1, Name = "Blue Note Updated" };

            _mockService.Setup(s => s.UpdateAsync(1, It.IsAny<LabelDto>()))
                .ReturnsAsync(updatedDto);

            // Act
            var result = await _controller.UpdateLabel(1, updateDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedLabel = Assert.IsType<LabelDto>(okResult.Value);
            Assert.Equal("Blue Note Updated", returnedLabel.Name);
        }

        [Fact]
        public async Task UpdateLabel_NonExistingId_ReturnsNotFound()
        {
            // Arrange
            var updateDto = new LabelDto { Name = "Blue Note Updated" };
            _mockService.Setup(s => s.UpdateAsync(999, It.IsAny<LabelDto>()))
                .ThrowsAsync(new KeyNotFoundException("Label not found"));

            // Act
            var result = await _controller.UpdateLabel(999, updateDto);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.NotNull(notFoundResult.Value);
        }

        [Fact]
        public async Task UpdateLabel_InvalidId_ReturnsBadRequest()
        {
            // Arrange
            var updateDto = new LabelDto { Name = "Blue Note" };

            // Act
            var result = await _controller.UpdateLabel(0, updateDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task UpdateLabel_InvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            _controller.ModelState.AddModelError("Name", "Name is required");

            // Act
            var result = await _controller.UpdateLabel(1, new LabelDto());

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task UpdateLabel_ServiceThrowsArgumentException_ReturnsBadRequest()
        {
            // Arrange
            var updateDto = new LabelDto { Name = "Columbia" };
            _mockService.Setup(s => s.UpdateAsync(1, It.IsAny<LabelDto>()))
                .ThrowsAsync(new ArgumentException("Duplicate label name"));

            // Act
            var result = await _controller.UpdateLabel(1, updateDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequestResult.Value);
        }

        #endregion

        #region DeleteLabel Tests

        [Fact]
        public async Task DeleteLabel_ExistingId_ReturnsNoContent()
        {
            // Arrange
            _mockService.Setup(s => s.DeleteAsync(1))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteLabel(1);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteLabel_NonExistingId_ReturnsNotFound()
        {
            // Arrange
            _mockService.Setup(s => s.DeleteAsync(999))
                .ThrowsAsync(new KeyNotFoundException("Label not found"));

            // Act
            var result = await _controller.DeleteLabel(999);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFoundResult.Value);
        }

        [Fact]
        public async Task DeleteLabel_InvalidId_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.DeleteLabel(0);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task DeleteLabel_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            _mockService.Setup(s => s.DeleteAsync(1))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.DeleteLabel(1);

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
                new LabelsController(null!, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_NullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new LabelsController(_mockService.Object, null!));
        }

        #endregion
    }
}
