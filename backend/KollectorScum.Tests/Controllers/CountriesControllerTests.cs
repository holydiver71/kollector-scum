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
    /// Unit tests for CountriesController using refactored BaseApiController pattern
    /// </summary>
    public class CountriesControllerTests
    {
        private readonly Mock<IGenericCrudService<Country, CountryDto>> _mockService;
        private readonly Mock<ILogger<CountriesController>> _mockLogger;
        private readonly CountriesController _controller;

        public CountriesControllerTests()
        {
            _mockService = new Mock<IGenericCrudService<Country, CountryDto>>();
            _mockLogger = new Mock<ILogger<CountriesController>>();
            _controller = new CountriesController(_mockService.Object, _mockLogger.Object);
        }

        #region GetCountries Tests

        [Fact]
        public async Task GetCountries_ValidParameters_ReturnsOkWithPagedResults()
        {
            // Arrange
            var pagedResult = new PagedResult<CountryDto>
            {
                Items = new List<CountryDto>
                {
                    new CountryDto { Id = 1, Name = "USA" },
                    new CountryDto { Id = 2, Name = "UK" }
                },
                TotalCount = 2,
                Page = 1,
                PageSize = 50,
                TotalPages = 1
            };

            _mockService.Setup(s => s.GetAllAsync(1, 50, null, null, null))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetCountries(null, 1, 50);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedResult = Assert.IsType<PagedResult<CountryDto>>(okResult.Value);
            Assert.Equal(2, returnedResult.Items.Count());
            Assert.Equal(2, returnedResult.TotalCount);
        }

        [Fact]
        public async Task GetCountries_WithSearchTerm_ReturnsFilteredResults()
        {
            // Arrange
            var pagedResult = new PagedResult<CountryDto>
            {
                Items = new List<CountryDto>
                {
                    new CountryDto { Id = 1, Name = "USA" }
                },
                TotalCount = 1,
                Page = 1,
                PageSize = 50,
                TotalPages = 1
            };

            _mockService.Setup(s => s.GetAllAsync(1, 50, "USA", null, null))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetCountries("USA", 1, 50);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedResult = Assert.IsType<PagedResult<CountryDto>>(okResult.Value);
            Assert.Single(returnedResult.Items);
            Assert.Equal("USA", returnedResult.Items.ToList()[0].Name);
        }

        [Fact]
        public async Task GetCountries_InvalidPage_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetCountries(null, 0, 50);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task GetCountries_InvalidPageSize_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetCountries(null, 1, 0);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task GetCountries_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            _mockService.Setup(s => s.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), null, null))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetCountries(null, 1, 50);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        #endregion

        #region GetCountry Tests

        [Fact]
        public async Task GetCountry_ExistingId_ReturnsOkWithCountry()
        {
            // Arrange
            var countryDto = new CountryDto { Id = 1, Name = "USA" };
            _mockService.Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(countryDto);

            // Act
            var result = await _controller.GetCountry(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedCountry = Assert.IsType<CountryDto>(okResult.Value);
            Assert.Equal(1, returnedCountry.Id);
            Assert.Equal("USA", returnedCountry.Name);
        }

        [Fact]
        public async Task GetCountry_NonExistingId_ReturnsNotFound()
        {
            // Arrange
            _mockService.Setup(s => s.GetByIdAsync(999))
                .ReturnsAsync((CountryDto?)null);

            // Act
            var result = await _controller.GetCountry(999);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.NotNull(notFoundResult.Value);
        }

        [Fact]
        public async Task GetCountry_InvalidId_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetCountry(0);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequestResult.Value);
        }

        #endregion

        #region CreateCountry Tests

        [Fact]
        public async Task CreateCountry_ValidCountry_ReturnsCreatedAtAction()
        {
            // Arrange
            var createDto = new CountryDto { Name = "Germany" };
            var createdDto = new CountryDto { Id = 1, Name = "Germany" };

            _mockService.Setup(s => s.CreateAsync(It.IsAny<CountryDto>()))
                .ReturnsAsync(createdDto);

            // Act
            var result = await _controller.CreateCountry(createDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returnedCountry = Assert.IsType<CountryDto>(createdResult.Value);
            Assert.Equal("Germany", returnedCountry.Name);
            Assert.Equal(nameof(_controller.GetCountry), createdResult.ActionName);
        }

        [Fact]
        public async Task CreateCountry_InvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            _controller.ModelState.AddModelError("Name", "Name is required");

            // Act
            var result = await _controller.CreateCountry(new CountryDto());

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task CreateCountry_ServiceThrowsArgumentException_ReturnsBadRequest()
        {
            // Arrange
            var createDto = new CountryDto { Name = "USA" };
            _mockService.Setup(s => s.CreateAsync(It.IsAny<CountryDto>()))
                .ThrowsAsync(new ArgumentException("Country already exists"));

            // Act
            var result = await _controller.CreateCountry(createDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequestResult.Value);
        }

        #endregion

        #region UpdateCountry Tests

        [Fact]
        public async Task UpdateCountry_ValidCountry_ReturnsOk()
        {
            // Arrange
            var updateDto = new CountryDto { Name = "USA Updated" };
            var updatedDto = new CountryDto { Id = 1, Name = "USA Updated" };

            _mockService.Setup(s => s.UpdateAsync(1, It.IsAny<CountryDto>()))
                .ReturnsAsync(updatedDto);

            // Act
            var result = await _controller.UpdateCountry(1, updateDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedCountry = Assert.IsType<CountryDto>(okResult.Value);
            Assert.Equal("USA Updated", returnedCountry.Name);
        }

        [Fact]
        public async Task UpdateCountry_NonExistingId_ReturnsNotFound()
        {
            // Arrange
            var updateDto = new CountryDto { Name = "USA Updated" };
            _mockService.Setup(s => s.UpdateAsync(999, It.IsAny<CountryDto>()))
                .ThrowsAsync(new KeyNotFoundException("Country not found"));

            // Act
            var result = await _controller.UpdateCountry(999, updateDto);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.NotNull(notFoundResult.Value);
        }

        [Fact]
        public async Task UpdateCountry_InvalidId_ReturnsBadRequest()
        {
            // Arrange
            var updateDto = new CountryDto { Name = "USA" };

            // Act
            var result = await _controller.UpdateCountry(0, updateDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task UpdateCountry_InvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            _controller.ModelState.AddModelError("Name", "Name is required");

            // Act
            var result = await _controller.UpdateCountry(1, new CountryDto());

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task UpdateCountry_ServiceThrowsArgumentException_ReturnsBadRequest()
        {
            // Arrange
            var updateDto = new CountryDto { Name = "UK" };
            _mockService.Setup(s => s.UpdateAsync(1, It.IsAny<CountryDto>()))
                .ThrowsAsync(new ArgumentException("Duplicate country name"));

            // Act
            var result = await _controller.UpdateCountry(1, updateDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequestResult.Value);
        }

        #endregion

        #region DeleteCountry Tests

        [Fact]
        public async Task DeleteCountry_ExistingId_ReturnsNoContent()
        {
            // Arrange
            _mockService.Setup(s => s.DeleteAsync(1))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteCountry(1);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteCountry_NonExistingId_ReturnsNotFound()
        {
            // Arrange
            _mockService.Setup(s => s.DeleteAsync(999))
                .ThrowsAsync(new KeyNotFoundException("Country not found"));

            // Act
            var result = await _controller.DeleteCountry(999);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFoundResult.Value);
        }

        [Fact]
        public async Task DeleteCountry_InvalidId_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.DeleteCountry(0);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task DeleteCountry_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            _mockService.Setup(s => s.DeleteAsync(1))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.DeleteCountry(1);

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
                new CountriesController(null!, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_NullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new CountriesController(_mockService.Object, null!));
        }

        #endregion
    }
}
