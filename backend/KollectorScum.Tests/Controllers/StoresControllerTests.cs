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
    /// Unit tests for StoresController using refactored BaseApiController pattern
    /// </summary>
    public class StoresControllerTests
    {
        private readonly Mock<IGenericCrudService<Store, StoreDto>> _mockService;
        private readonly Mock<ILogger<StoresController>> _mockLogger;
        private readonly StoresController _controller;

        public StoresControllerTests()
        {
            _mockService = new Mock<IGenericCrudService<Store, StoreDto>>();
            _mockLogger = new Mock<ILogger<StoresController>>();
            _controller = new StoresController(_mockService.Object, _mockLogger.Object);
        }

        #region GetStores Tests

        [Fact]
        public async Task GetStores_ValidParameters_ReturnsOkWithPagedResults()
        {
            // Arrange
            var pagedResult = new PagedResult<StoreDto>
            {
                Items = new List<StoreDto>
                {
                    new StoreDto { Id = 1, Name = "Tower Records" },
                    new StoreDto { Id = 2, Name = "HMV" }
                },
                TotalCount = 2,
                Page = 1,
                PageSize = 50,
                TotalPages = 1
            };

            _mockService.Setup(s => s.GetAllAsync(1, 50, null, null, null))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetStores(null, 1, 50);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedResult = Assert.IsType<PagedResult<StoreDto>>(okResult.Value);
            Assert.Equal(2, returnedResult.Items.Count());
            Assert.Equal(2, returnedResult.TotalCount);
        }

        [Fact]
        public async Task GetStores_WithSearchTerm_ReturnsFilteredResults()
        {
            // Arrange
            var pagedResult = new PagedResult<StoreDto>
            {
                Items = new List<StoreDto>
                {
                    new StoreDto { Id = 1, Name = "Tower Records" }
                },
                TotalCount = 1,
                Page = 1,
                PageSize = 50,
                TotalPages = 1
            };

            _mockService.Setup(s => s.GetAllAsync(1, 50, "Tower", null, null))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetStores("Tower", 1, 50);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedResult = Assert.IsType<PagedResult<StoreDto>>(okResult.Value);
            Assert.Single(returnedResult.Items);
            Assert.Equal("Tower Records", returnedResult.Items.ToList()[0].Name);
        }

        [Fact]
        public async Task GetStores_InvalidPage_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetStores(null, 0, 50);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task GetStores_InvalidPageSize_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetStores(null, 1, 0);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task GetStores_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            _mockService.Setup(s => s.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), null, null))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetStores(null, 1, 50);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        #endregion

        #region GetStore Tests

        [Fact]
        public async Task GetStore_ExistingId_ReturnsOkWithStore()
        {
            // Arrange
            var storeDto = new StoreDto { Id = 1, Name = "Tower Records" };
            _mockService.Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(storeDto);

            // Act
            var result = await _controller.GetStore(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedStore = Assert.IsType<StoreDto>(okResult.Value);
            Assert.Equal(1, returnedStore.Id);
            Assert.Equal("Tower Records", returnedStore.Name);
        }

        [Fact]
        public async Task GetStore_NonExistingId_ReturnsNotFound()
        {
            // Arrange
            _mockService.Setup(s => s.GetByIdAsync(999))
                .ReturnsAsync((StoreDto?)null);

            // Act
            var result = await _controller.GetStore(999);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.NotNull(notFoundResult.Value);
        }

        [Fact]
        public async Task GetStore_InvalidId_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetStore(0);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequestResult.Value);
        }

        #endregion

        #region CreateStore Tests

        [Fact]
        public async Task CreateStore_ValidStore_ReturnsCreatedAtAction()
        {
            // Arrange
            var createDto = new StoreDto { Name = "Rough Trade" };
            var createdDto = new StoreDto { Id = 1, Name = "Rough Trade" };

            _mockService.Setup(s => s.CreateAsync(It.IsAny<StoreDto>()))
                .ReturnsAsync(createdDto);

            // Act
            var result = await _controller.CreateStore(createDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returnedStore = Assert.IsType<StoreDto>(createdResult.Value);
            Assert.Equal("Rough Trade", returnedStore.Name);
            Assert.Equal(nameof(_controller.GetStore), createdResult.ActionName);
        }

        [Fact]
        public async Task CreateStore_InvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            _controller.ModelState.AddModelError("Name", "Name is required");

            // Act
            var result = await _controller.CreateStore(new StoreDto());

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task CreateStore_ServiceThrowsArgumentException_ReturnsBadRequest()
        {
            // Arrange
            var createDto = new StoreDto { Name = "Tower Records" };
            _mockService.Setup(s => s.CreateAsync(It.IsAny<StoreDto>()))
                .ThrowsAsync(new ArgumentException("Store already exists"));

            // Act
            var result = await _controller.CreateStore(createDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequestResult.Value);
        }

        #endregion

        #region UpdateStore Tests

        [Fact]
        public async Task UpdateStore_ValidStore_ReturnsOk()
        {
            // Arrange
            var updateDto = new StoreDto { Name = "Tower Records Updated" };
            var updatedDto = new StoreDto { Id = 1, Name = "Tower Records Updated" };

            _mockService.Setup(s => s.UpdateAsync(1, It.IsAny<StoreDto>()))
                .ReturnsAsync(updatedDto);

            // Act
            var result = await _controller.UpdateStore(1, updateDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedStore = Assert.IsType<StoreDto>(okResult.Value);
            Assert.Equal("Tower Records Updated", returnedStore.Name);
        }

        [Fact]
        public async Task UpdateStore_NonExistingId_ReturnsNotFound()
        {
            // Arrange
            var updateDto = new StoreDto { Name = "Tower Records Updated" };
            _mockService.Setup(s => s.UpdateAsync(999, It.IsAny<StoreDto>()))
                .ThrowsAsync(new KeyNotFoundException("Store not found"));

            // Act
            var result = await _controller.UpdateStore(999, updateDto);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.NotNull(notFoundResult.Value);
        }

        [Fact]
        public async Task UpdateStore_InvalidId_ReturnsBadRequest()
        {
            // Arrange
            var updateDto = new StoreDto { Name = "Tower Records" };

            // Act
            var result = await _controller.UpdateStore(0, updateDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task UpdateStore_InvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            _controller.ModelState.AddModelError("Name", "Name is required");

            // Act
            var result = await _controller.UpdateStore(1, new StoreDto());

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task UpdateStore_ServiceThrowsArgumentException_ReturnsBadRequest()
        {
            // Arrange
            var updateDto = new StoreDto { Name = "HMV" };
            _mockService.Setup(s => s.UpdateAsync(1, It.IsAny<StoreDto>()))
                .ThrowsAsync(new ArgumentException("Duplicate store name"));

            // Act
            var result = await _controller.UpdateStore(1, updateDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequestResult.Value);
        }

        #endregion

        #region DeleteStore Tests

        [Fact]
        public async Task DeleteStore_ExistingId_ReturnsNoContent()
        {
            // Arrange
            _mockService.Setup(s => s.DeleteAsync(1))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteStore(1);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteStore_NonExistingId_ReturnsNotFound()
        {
            // Arrange
            _mockService.Setup(s => s.DeleteAsync(999))
                .ThrowsAsync(new KeyNotFoundException("Store not found"));

            // Act
            var result = await _controller.DeleteStore(999);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFoundResult.Value);
        }

        [Fact]
        public async Task DeleteStore_InvalidId_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.DeleteStore(0);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task DeleteStore_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            _mockService.Setup(s => s.DeleteAsync(1))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.DeleteStore(1);

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
                new StoresController(null!, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_NullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new StoresController(_mockService.Object, null!));
        }

        #endregion
    }
}
