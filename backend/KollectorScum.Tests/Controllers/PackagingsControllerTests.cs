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
    /// Unit tests for PackagingsController using refactored BaseApiController pattern
    /// </summary>
    public class PackagingsControllerTests
    {
        private readonly Mock<IGenericCrudService<Packaging, PackagingDto>> _mockService;
        private readonly Mock<ILogger<PackagingsController>> _mockLogger;
        private readonly PackagingsController _controller;

        public PackagingsControllerTests()
        {
            _mockService = new Mock<IGenericCrudService<Packaging, PackagingDto>>();
            _mockLogger = new Mock<ILogger<PackagingsController>>();
            _controller = new PackagingsController(_mockService.Object, _mockLogger.Object);
        }

        #region GetPackagings Tests

        [Fact]
        public async Task GetPackagings_ValidParameters_ReturnsOkWithPagedResults()
        {
            // Arrange
            var pagedResult = new PagedResult<PackagingDto>
            {
                Items = new List<PackagingDto>
                {
                    new PackagingDto { Id = 1, Name = "Jewel Case" },
                    new PackagingDto { Id = 2, Name = "Digipak" }
                },
                TotalCount = 2,
                Page = 1,
                PageSize = 50,
                TotalPages = 1
            };

            _mockService.Setup(s => s.GetAllAsync(1, 50, null, null, null))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetPackagings(null, 1, 50);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedResult = Assert.IsType<PagedResult<PackagingDto>>(okResult.Value);
            Assert.Equal(2, returnedResult.Items.Count());
            Assert.Equal(2, returnedResult.TotalCount);
        }

        [Fact]
        public async Task GetPackagings_WithSearchTerm_ReturnsFilteredResults()
        {
            // Arrange
            var pagedResult = new PagedResult<PackagingDto>
            {
                Items = new List<PackagingDto>
                {
                    new PackagingDto { Id = 1, Name = "Jewel Case" }
                },
                TotalCount = 1,
                Page = 1,
                PageSize = 50,
                TotalPages = 1
            };

            _mockService.Setup(s => s.GetAllAsync(1, 50, "Jewel", null, null))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetPackagings("Jewel", 1, 50);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedResult = Assert.IsType<PagedResult<PackagingDto>>(okResult.Value);
            Assert.Single(returnedResult.Items);
            Assert.Equal("Jewel Case", returnedResult.Items.ToList()[0].Name);
        }

        [Fact]
        public async Task GetPackagings_InvalidPage_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetPackagings(null, 0, 50);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task GetPackagings_InvalidPageSize_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetPackagings(null, 1, 0);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task GetPackagings_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            _mockService.Setup(s => s.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), null, null))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetPackagings(null, 1, 50);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        #endregion

        #region GetPackaging Tests

        [Fact]
        public async Task GetPackaging_ExistingId_ReturnsOkWithPackaging()
        {
            // Arrange
            var packagingDto = new PackagingDto { Id = 1, Name = "Jewel Case" };
            _mockService.Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(packagingDto);

            // Act
            var result = await _controller.GetPackaging(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedPackaging = Assert.IsType<PackagingDto>(okResult.Value);
            Assert.Equal(1, returnedPackaging.Id);
            Assert.Equal("Jewel Case", returnedPackaging.Name);
        }

        [Fact]
        public async Task GetPackaging_NonExistingId_ReturnsNotFound()
        {
            // Arrange
            _mockService.Setup(s => s.GetByIdAsync(999))
                .ReturnsAsync((PackagingDto?)null);

            // Act
            var result = await _controller.GetPackaging(999);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.NotNull(notFoundResult.Value);
        }

        [Fact]
        public async Task GetPackaging_InvalidId_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetPackaging(0);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequestResult.Value);
        }

        #endregion

        #region CreatePackaging Tests

        [Fact]
        public async Task CreatePackaging_ValidPackaging_ReturnsCreatedAtAction()
        {
            // Arrange
            var createDto = new PackagingDto { Name = "Gatefold" };
            var createdDto = new PackagingDto { Id = 1, Name = "Gatefold" };

            _mockService.Setup(s => s.CreateAsync(It.IsAny<PackagingDto>()))
                .ReturnsAsync(createdDto);

            // Act
            var result = await _controller.CreatePackaging(createDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returnedPackaging = Assert.IsType<PackagingDto>(createdResult.Value);
            Assert.Equal("Gatefold", returnedPackaging.Name);
            Assert.Equal(nameof(_controller.GetPackaging), createdResult.ActionName);
        }

        [Fact]
        public async Task CreatePackaging_InvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            _controller.ModelState.AddModelError("Name", "Name is required");

            // Act
            var result = await _controller.CreatePackaging(new PackagingDto());

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task CreatePackaging_ServiceThrowsArgumentException_ReturnsBadRequest()
        {
            // Arrange
            var createDto = new PackagingDto { Name = "Jewel Case" };
            _mockService.Setup(s => s.CreateAsync(It.IsAny<PackagingDto>()))
                .ThrowsAsync(new ArgumentException("Packaging already exists"));

            // Act
            var result = await _controller.CreatePackaging(createDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequestResult.Value);
        }

        #endregion

        #region UpdatePackaging Tests

        [Fact]
        public async Task UpdatePackaging_ValidPackaging_ReturnsOk()
        {
            // Arrange
            var updateDto = new PackagingDto { Name = "Jewel Case Updated" };
            var updatedDto = new PackagingDto { Id = 1, Name = "Jewel Case Updated" };

            _mockService.Setup(s => s.UpdateAsync(1, It.IsAny<PackagingDto>()))
                .ReturnsAsync(updatedDto);

            // Act
            var result = await _controller.UpdatePackaging(1, updateDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedPackaging = Assert.IsType<PackagingDto>(okResult.Value);
            Assert.Equal("Jewel Case Updated", returnedPackaging.Name);
        }

        [Fact]
        public async Task UpdatePackaging_NonExistingId_ReturnsNotFound()
        {
            // Arrange
            var updateDto = new PackagingDto { Name = "Jewel Case Updated" };
            _mockService.Setup(s => s.UpdateAsync(999, It.IsAny<PackagingDto>()))
                .ThrowsAsync(new KeyNotFoundException("Packaging not found"));

            // Act
            var result = await _controller.UpdatePackaging(999, updateDto);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.NotNull(notFoundResult.Value);
        }

        [Fact]
        public async Task UpdatePackaging_InvalidId_ReturnsBadRequest()
        {
            // Arrange
            var updateDto = new PackagingDto { Name = "Jewel Case" };

            // Act
            var result = await _controller.UpdatePackaging(0, updateDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task UpdatePackaging_InvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            _controller.ModelState.AddModelError("Name", "Name is required");

            // Act
            var result = await _controller.UpdatePackaging(1, new PackagingDto());

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task UpdatePackaging_ServiceThrowsArgumentException_ReturnsBadRequest()
        {
            // Arrange
            var updateDto = new PackagingDto { Name = "Digipak" };
            _mockService.Setup(s => s.UpdateAsync(1, It.IsAny<PackagingDto>()))
                .ThrowsAsync(new ArgumentException("Duplicate packaging name"));

            // Act
            var result = await _controller.UpdatePackaging(1, updateDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequestResult.Value);
        }

        #endregion

        #region DeletePackaging Tests

        [Fact]
        public async Task DeletePackaging_ExistingId_ReturnsNoContent()
        {
            // Arrange
            _mockService.Setup(s => s.DeleteAsync(1))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeletePackaging(1);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeletePackaging_NonExistingId_ReturnsNotFound()
        {
            // Arrange
            _mockService.Setup(s => s.DeleteAsync(999))
                .ThrowsAsync(new KeyNotFoundException("Packaging not found"));

            // Act
            var result = await _controller.DeletePackaging(999);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFoundResult.Value);
        }

        [Fact]
        public async Task DeletePackaging_InvalidId_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.DeletePackaging(0);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task DeletePackaging_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            _mockService.Setup(s => s.DeleteAsync(1))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.DeletePackaging(1);

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
                new PackagingsController(null!, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_NullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new PackagingsController(_mockService.Object, null!));
        }

        #endregion
    }
}
