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
    /// Unit tests for GenresController using refactored BaseApiController pattern
    /// </summary>
    public class GenresControllerTests
    {
        private readonly Mock<IGenericCrudService<Genre, GenreDto>> _mockService;
        private readonly Mock<ILogger<GenresController>> _mockLogger;
        private readonly GenresController _controller;

        public GenresControllerTests()
        {
            _mockService = new Mock<IGenericCrudService<Genre, GenreDto>>();
            _mockLogger = new Mock<ILogger<GenresController>>();
            _controller = new GenresController(_mockService.Object, _mockLogger.Object);
        }

        #region GetGenres Tests

        [Fact]
        public async Task GetGenres_ValidParameters_ReturnsOkWithPagedResults()
        {
            // Arrange
            var pagedResult = new PagedResult<GenreDto>
            {
                Items = new List<GenreDto>
                {
                    new GenreDto { Id = 1, Name = "Rock" },
                    new GenreDto { Id = 2, Name = "Jazz" }
                },
                TotalCount = 2,
                Page = 1,
                PageSize = 50,
                TotalPages = 1
            };

            _mockService.Setup(s => s.GetAllAsync(1, 50, null, null, null))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetGenres(null, 1, 50);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedResult = Assert.IsType<PagedResult<GenreDto>>(okResult.Value);
            Assert.Equal(2, returnedResult.Items.Count());
            Assert.Equal(2, returnedResult.TotalCount);
        }

        [Fact]
        public async Task GetGenres_WithSearchTerm_ReturnsFilteredResults()
        {
            // Arrange
            var pagedResult = new PagedResult<GenreDto>
            {
                Items = new List<GenreDto>
                {
                    new GenreDto { Id = 1, Name = "Rock" }
                },
                TotalCount = 1,
                Page = 1,
                PageSize = 50,
                TotalPages = 1
            };

            _mockService.Setup(s => s.GetAllAsync(1, 50, "Rock", null, null))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetGenres("Rock", 1, 50);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedResult = Assert.IsType<PagedResult<GenreDto>>(okResult.Value);
            Assert.Single(returnedResult.Items);
            Assert.Equal("Rock", returnedResult.Items.ToList()[0].Name);
        }

        [Fact]
        public async Task GetGenres_InvalidPage_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetGenres(null, 0, 50);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task GetGenres_InvalidPageSize_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetGenres(null, 1, 0);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task GetGenres_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            _mockService.Setup(s => s.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), null, null))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetGenres(null, 1, 50);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        #endregion

        #region GetGenre Tests

        [Fact]
        public async Task GetGenre_ExistingId_ReturnsOkWithGenre()
        {
            // Arrange
            var genreDto = new GenreDto { Id = 1, Name = "Rock" };
            _mockService.Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(genreDto);

            // Act
            var result = await _controller.GetGenre(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedGenre = Assert.IsType<GenreDto>(okResult.Value);
            Assert.Equal(1, returnedGenre.Id);
            Assert.Equal("Rock", returnedGenre.Name);
        }

        [Fact]
        public async Task GetGenre_NonExistingId_ReturnsNotFound()
        {
            // Arrange
            _mockService.Setup(s => s.GetByIdAsync(999))
                .ReturnsAsync((GenreDto?)null);

            // Act
            var result = await _controller.GetGenre(999);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.NotNull(notFoundResult.Value);
        }

        [Fact]
        public async Task GetGenre_InvalidId_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetGenre(0);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequestResult.Value);
        }

        #endregion

        #region CreateGenre Tests

        [Fact]
        public async Task CreateGenre_ValidGenre_ReturnsCreatedAtAction()
        {
            // Arrange
            var createDto = new GenreDto { Name = "Electronic" };
            var createdDto = new GenreDto { Id = 1, Name = "Electronic" };

            _mockService.Setup(s => s.CreateAsync(It.IsAny<GenreDto>()))
                .ReturnsAsync(createdDto);

            // Act
            var result = await _controller.CreateGenre(createDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returnedGenre = Assert.IsType<GenreDto>(createdResult.Value);
            Assert.Equal("Electronic", returnedGenre.Name);
            Assert.Equal(nameof(_controller.GetGenre), createdResult.ActionName);
        }

        [Fact]
        public async Task CreateGenre_InvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            _controller.ModelState.AddModelError("Name", "Name is required");

            // Act
            var result = await _controller.CreateGenre(new GenreDto());

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task CreateGenre_ServiceThrowsArgumentException_ReturnsBadRequest()
        {
            // Arrange
            var createDto = new GenreDto { Name = "Rock" };
            _mockService.Setup(s => s.CreateAsync(It.IsAny<GenreDto>()))
                .ThrowsAsync(new ArgumentException("Genre already exists"));

            // Act
            var result = await _controller.CreateGenre(createDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequestResult.Value);
        }

        #endregion

        #region UpdateGenre Tests

        [Fact]
        public async Task UpdateGenre_ValidGenre_ReturnsOk()
        {
            // Arrange
            var updateDto = new GenreDto { Name = "Rock Updated" };
            var updatedDto = new GenreDto { Id = 1, Name = "Rock Updated" };

            _mockService.Setup(s => s.UpdateAsync(1, It.IsAny<GenreDto>()))
                .ReturnsAsync(updatedDto);

            // Act
            var result = await _controller.UpdateGenre(1, updateDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedGenre = Assert.IsType<GenreDto>(okResult.Value);
            Assert.Equal("Rock Updated", returnedGenre.Name);
        }

        [Fact]
        public async Task UpdateGenre_NonExistingId_ReturnsNotFound()
        {
            // Arrange
            var updateDto = new GenreDto { Name = "Rock Updated" };
            _mockService.Setup(s => s.UpdateAsync(999, It.IsAny<GenreDto>()))
                .ThrowsAsync(new KeyNotFoundException("Genre not found"));

            // Act
            var result = await _controller.UpdateGenre(999, updateDto);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.NotNull(notFoundResult.Value);
        }

        [Fact]
        public async Task UpdateGenre_InvalidId_ReturnsBadRequest()
        {
            // Arrange
            var updateDto = new GenreDto { Name = "Rock" };

            // Act
            var result = await _controller.UpdateGenre(0, updateDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task UpdateGenre_InvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            _controller.ModelState.AddModelError("Name", "Name is required");

            // Act
            var result = await _controller.UpdateGenre(1, new GenreDto());

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task UpdateGenre_ServiceThrowsArgumentException_ReturnsBadRequest()
        {
            // Arrange
            var updateDto = new GenreDto { Name = "Rock" };
            _mockService.Setup(s => s.UpdateAsync(1, It.IsAny<GenreDto>()))
                .ThrowsAsync(new ArgumentException("Duplicate genre name"));

            // Act
            var result = await _controller.UpdateGenre(1, updateDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequestResult.Value);
        }

        #endregion

        #region DeleteGenre Tests

        [Fact]
        public async Task DeleteGenre_ExistingId_ReturnsNoContent()
        {
            // Arrange
            _mockService.Setup(s => s.DeleteAsync(1))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteGenre(1);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteGenre_NonExistingId_ReturnsNotFound()
        {
            // Arrange
            _mockService.Setup(s => s.DeleteAsync(999))
                .ThrowsAsync(new KeyNotFoundException("Genre not found"));

            // Act
            var result = await _controller.DeleteGenre(999);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFoundResult.Value);
        }

        [Fact]
        public async Task DeleteGenre_InvalidId_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.DeleteGenre(0);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task DeleteGenre_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            _mockService.Setup(s => s.DeleteAsync(1))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.DeleteGenre(1);

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
                new GenresController(null!, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_NullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new GenresController(_mockService.Object, null!));
        }

        #endregion
    }
}
