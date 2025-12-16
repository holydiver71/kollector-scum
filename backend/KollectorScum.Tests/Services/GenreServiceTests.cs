using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using KollectorScum.Api.Services;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace KollectorScum.Tests.Services
{
    /// <summary>
    /// Unit tests for GenreService
    /// </summary>
    public class GenreServiceTests
    {
        private readonly Mock<IRepository<Genre>> _mockRepository;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILogger<GenreService>> _mockLogger;
        private readonly Mock<IUserContext> _mockUserContext;
        private readonly GenreService _service;
        private readonly Guid _userId;

        public GenreServiceTests()
        {
            _mockRepository = new Mock<IRepository<Genre>>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<GenreService>>();
            _mockUserContext = new Mock<IUserContext>();
            var defaultUserId = Guid.Parse("12337b39-c346-449c-b269-33b2e820d74f");
            _mockUserContext.Setup(u => u.GetActingUserId()).Returns(defaultUserId);
            _mockUserContext.Setup(u => u.GetUserId()).Returns(defaultUserId);
            _mockUserContext.Setup(u => u.IsAdmin()).Returns(false);
            _userId = Guid.NewGuid();
            _mockUserContext.Setup(x => x.GetActingUserId()).Returns(_userId);
            _service = new GenreService(_mockRepository.Object, _mockUnitOfWork.Object, _mockLogger.Object, _mockUserContext.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            // Act & Assert
            Assert.NotNull(_service);
        }

        [Fact]
        public void Constructor_WithNullRepository_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new GenreService(null!, _mockUnitOfWork.Object, _mockLogger.Object, _mockUserContext.Object));
        }

        [Fact]
        public void Constructor_WithNullUnitOfWork_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new GenreService(_mockRepository.Object, null!, _mockLogger.Object, _mockUserContext.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new GenreService(_mockRepository.Object, _mockUnitOfWork.Object, null!, _mockUserContext.Object));
        }

        [Fact]
        public void Constructor_WithNullUserContext_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new GenreService(_mockRepository.Object, _mockUnitOfWork.Object, _mockLogger.Object, null!));
        }

        #endregion

        #region Validation Tests

        [Fact]
        public async Task CreateAsync_WithValidGenre_CreatesSuccessfully()
        {
            // Arrange
            var genreDto = new GenreDto { Name = "Rock" };
            var pagedResult = new PagedResult<Genre>
            {
                Items = new System.Collections.Generic.List<Genre>(),
                TotalCount = 0,
                Page = 1,
                PageSize = 1,
                TotalPages = 0
            };

            _mockRepository.Setup(r => r.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<Genre, bool>>>(),
                It.IsAny<Func<IQueryable<Genre>, IOrderedQueryable<Genre>>>(),
                It.IsAny<string>()))
                .ReturnsAsync(pagedResult);

            _mockRepository.Setup(r => r.AddAsync(It.IsAny<Genre>()))
                .ReturnsAsync(new Genre { Id = 1, Name = "Rock", UserId = _userId });

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _service.CreateAsync(genreDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Rock", result.Name);
        }

        [Fact]
        public async Task CreateAsync_WithNullName_ThrowsArgumentException()
        {
            // Arrange
            var genreDto = new GenreDto { Name = null! };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(genreDto));
        }

        [Fact]
        public async Task CreateAsync_WithEmptyName_ThrowsArgumentException()
        {
            // Arrange
            var genreDto = new GenreDto { Name = "" };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(genreDto));
        }

        [Fact]
        public async Task CreateAsync_WithWhitespaceName_ThrowsArgumentException()
        {
            // Arrange
            var genreDto = new GenreDto { Name = "   " };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(genreDto));
        }

        [Fact]
        public async Task CreateAsync_WithNameOver100Characters_ThrowsArgumentException()
        {
            // Arrange
            var genreDto = new GenreDto { Name = new string('A', 101) };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(genreDto));
            Assert.Contains("cannot exceed 100 characters", exception.Message);
        }

        [Fact]
        public async Task CreateAsync_WithNameExactly100Characters_CreatesSuccessfully()
        {
            // Arrange
            var genreDto = new GenreDto { Name = new string('A', 100) };
            var pagedResult = new PagedResult<Genre>
            {
                Items = new System.Collections.Generic.List<Genre>(),
                TotalCount = 0,
                Page = 1,
                PageSize = 1,
                TotalPages = 0
            };

            _mockRepository.Setup(r => r.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<Genre, bool>>>(),
                It.IsAny<Func<IQueryable<Genre>, IOrderedQueryable<Genre>>>(),
                It.IsAny<string>()))
                .ReturnsAsync(pagedResult);

            _mockRepository.Setup(r => r.AddAsync(It.IsAny<Genre>()))
                .ReturnsAsync(new Genre { Id = 1, Name = genreDto.Name, UserId = _userId });

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _service.CreateAsync(genreDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(100, result.Name.Length);
        }

        [Fact]
        public async Task UpdateAsync_WithValidGenre_UpdatesSuccessfully()
        {
            // Arrange
            var existingGenre = new Genre { Id = 1, Name = "Old Name", UserId = _userId };
            var genreDto = new GenreDto { Id = 1, Name = "New Name" };

            _mockRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existingGenre);

            _mockRepository.Setup(r => r.Update(It.IsAny<Genre>()));

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _service.UpdateAsync(1, genreDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("New Name", result.Name);
            _mockRepository.Verify(r => r.Update(It.IsAny<Genre>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WithInvalidName_ThrowsArgumentException()
        {
            // Arrange
            var existingGenre = new Genre { Id = 1, Name = "Old Name", UserId = _userId };
            var genreDto = new GenreDto { Id = 1, Name = "" };

            _mockRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existingGenre);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateAsync(1, genreDto));
        }

        #endregion

        #region Search Tests

        [Fact]
        public async Task GetAllAsync_WithSearch_UsesSearchFilter()
        {
            // Arrange
            var pagedResult = new PagedResult<Genre>
            {
                Items = new System.Collections.Generic.List<Genre>
                {
                    new Genre { Id = 1, Name = "Rock", UserId = _userId }
                },
                TotalCount = 1,
                Page = 1,
                PageSize = 50,
                TotalPages = 1
            };

            _mockRepository.Setup(r => r.GetPagedAsync(
                1,
                50,
                It.IsAny<Expression<Func<Genre, bool>>>(),
                It.IsAny<Func<IQueryable<Genre>, IOrderedQueryable<Genre>>>(),
                ""))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _service.GetAllAsync(1, 50, "rock");

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Items);
            _mockRepository.Verify(r => r.GetPagedAsync(
                1,
                50,
                It.IsAny<Expression<Func<Genre, bool>>>(),
                It.IsAny<Func<IQueryable<Genre>, IOrderedQueryable<Genre>>>(),
                ""), Times.Once);
        }

        #endregion

        #region CRUD Operation Tests

        [Fact]
        public async Task GetByIdAsync_WithValidId_ReturnsGenre()
        {
            // Arrange
            var genre = new Genre { Id = 1, Name = "Jazz", UserId = _userId };
            _mockRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(genre);

            // Act
            var result = await _service.GetByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("Jazz", result.Name);
        }

        [Fact]
        public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((Genre?)null);

            // Act
            var result = await _service.GetByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteAsync_WithValidId_DeletesSuccessfully()
        {
            // Arrange
            var genre = new Genre { Id = 1, Name = "Classical", UserId = _userId };
            _mockRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(genre);

            _mockRepository.Setup(r => r.Delete(It.IsAny<Genre>()));

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _service.DeleteAsync(1);

            // Assert
            Assert.True(result);
            _mockRepository.Verify(r => r.Delete(It.IsAny<Genre>()), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WithInvalidId_ReturnsFalse()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((Genre?)null);

            // Act
            var result = await _service.DeleteAsync(999);

            // Assert
            Assert.False(result);
            _mockRepository.Verify(r => r.Delete(It.IsAny<Genre>()), Times.Never);
        }

        #endregion
    }
}
