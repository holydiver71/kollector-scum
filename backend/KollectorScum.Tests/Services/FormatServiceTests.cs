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
    /// Unit tests for FormatService
    /// </summary>
    public class FormatServiceTests
    {
        private readonly Mock<IRepository<Format>> _mockRepository;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILogger<FormatService>> _mockLogger;
        private readonly Mock<IUserContext> _mockUserContext;
        private readonly FormatService _service;
        private readonly Guid _userId;

        public FormatServiceTests()
        {
            _mockRepository = new Mock<IRepository<Format>>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<FormatService>>();
            _mockUserContext = new Mock<IUserContext>();
            var defaultUserId = Guid.Parse("12337b39-c346-449c-b269-33b2e820d74f");
            _mockUserContext.Setup(u => u.GetActingUserId()).Returns(defaultUserId);
            _mockUserContext.Setup(u => u.GetUserId()).Returns(defaultUserId);
            _mockUserContext.Setup(u => u.IsAdmin()).Returns(false);
            _userId = Guid.NewGuid();
            _mockUserContext.Setup(x => x.GetActingUserId()).Returns(_userId);
            _service = new FormatService(_mockRepository.Object, _mockUnitOfWork.Object, _mockLogger.Object, _mockUserContext.Object);
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
                new FormatService(null!, _mockUnitOfWork.Object, _mockLogger.Object, _mockUserContext.Object));
        }

        [Fact]
        public void Constructor_WithNullUnitOfWork_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new FormatService(_mockRepository.Object, null!, _mockLogger.Object, _mockUserContext.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new FormatService(_mockRepository.Object, _mockUnitOfWork.Object, null!, _mockUserContext.Object));
        }

        [Fact]
        public void Constructor_WithNullUserContext_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new FormatService(_mockRepository.Object, _mockUnitOfWork.Object, _mockLogger.Object, null!));
        }

        #endregion

        #region Validation Tests

        [Fact]
        public async Task CreateAsync_WithValidFormat_CreatesSuccessfully()
        {
            // Arrange
            var formatDto = new FormatDto { Name = "Vinyl" };
            var pagedResult = new PagedResult<Format>
            {
                Items = new System.Collections.Generic.List<Format>(),
                TotalCount = 0,
                Page = 1,
                PageSize = 1,
                TotalPages = 0
            };

            _mockRepository.Setup(r => r.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<Format, bool>>>(),
                It.IsAny<Func<IQueryable<Format>, IOrderedQueryable<Format>>>(),
                It.IsAny<string>()))
                .ReturnsAsync(pagedResult);

            _mockRepository.Setup(r => r.AddAsync(It.IsAny<Format>()))
                .ReturnsAsync(new Format { Id = 1, Name = "Vinyl", UserId = _userId });

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _service.CreateAsync(formatDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Vinyl", result.Name);
        }

        [Fact]
        public async Task CreateAsync_WithNullName_ThrowsArgumentException()
        {
            // Arrange
            var formatDto = new FormatDto { Name = null! };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(formatDto));
        }

        [Fact]
        public async Task CreateAsync_WithEmptyName_ThrowsArgumentException()
        {
            // Arrange
            var formatDto = new FormatDto { Name = "" };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(formatDto));
        }

        [Fact]
        public async Task CreateAsync_WithWhitespaceName_ThrowsArgumentException()
        {
            // Arrange
            var formatDto = new FormatDto { Name = "   " };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(formatDto));
        }

        [Fact]
        public async Task CreateAsync_WithNameOver100Characters_ThrowsArgumentException()
        {
            // Arrange
            var formatDto = new FormatDto { Name = new string('A', 51) };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(formatDto));
            Assert.Contains("cannot exceed 50 characters", exception.Message);
        }

        [Fact]
        public async Task CreateAsync_WithNameExactly100Characters_CreatesSuccessfully()
        {
            // Arrange
            var formatDto = new FormatDto { Name = new string('A', 50) };
            var pagedResult = new PagedResult<Format>
            {
                Items = new System.Collections.Generic.List<Format>(),
                TotalCount = 0,
                Page = 1,
                PageSize = 1,
                TotalPages = 0
            };

            _mockRepository.Setup(r => r.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<Format, bool>>>(),
                It.IsAny<Func<IQueryable<Format>, IOrderedQueryable<Format>>>(),
                It.IsAny<string>()))
                .ReturnsAsync(pagedResult);

            _mockRepository.Setup(r => r.AddAsync(It.IsAny<Format>()))
                .ReturnsAsync(new Format { Id = 1, Name = formatDto.Name, UserId = _userId });

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _service.CreateAsync(formatDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(50, result.Name.Length);
        }

        [Fact]
        public async Task UpdateAsync_WithValidFormat_UpdatesSuccessfully()
        {
            // Arrange
            var existingFormat = new Format { Id = 1, Name = "Old Name", UserId = _userId };
            var formatDto = new FormatDto { Id = 1, Name = "New Name" };

            _mockRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existingFormat);

            _mockRepository.Setup(r => r.Update(It.IsAny<Format>()));

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _service.UpdateAsync(1, formatDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("New Name", result.Name);
            _mockRepository.Verify(r => r.Update(It.IsAny<Format>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WithInvalidName_ThrowsArgumentException()
        {
            // Arrange
            var existingFormat = new Format { Id = 1, Name = "Old Name", UserId = _userId };
            var formatDto = new FormatDto { Id = 1, Name = "" };

            _mockRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existingFormat);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateAsync(1, formatDto));
        }

        #endregion

        #region Search Tests

        [Fact]
        public async Task GetAllAsync_WithSearch_UsesSearchFilter()
        {
            // Arrange
            var pagedResult = new PagedResult<Format>
            {
                Items = new System.Collections.Generic.List<Format>
                {
                    new Format { Id = 1, Name = "Vinyl", UserId = _userId }
                },
                TotalCount = 1,
                Page = 1,
                PageSize = 50,
                TotalPages = 1
            };

            _mockRepository.Setup(r => r.GetPagedAsync(
                1,
                50,
                It.IsAny<Expression<Func<Format, bool>>>(),
                It.IsAny<Func<IQueryable<Format>, IOrderedQueryable<Format>>>(),
                ""))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _service.GetAllAsync(1, 50, "vinyl");

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Items);
            _mockRepository.Verify(r => r.GetPagedAsync(
                1,
                50,
                It.IsAny<Expression<Func<Format, bool>>>(),
                It.IsAny<Func<IQueryable<Format>, IOrderedQueryable<Format>>>(),
                ""), Times.Once);
        }

        #endregion

        #region CRUD Operation Tests

        [Fact]
        public async Task GetByIdAsync_WithValidId_ReturnsFormat()
        {
            // Arrange
            var format = new Format { Id = 1, Name = "Vinyl", UserId = _userId };
            _mockRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(format);

            // Act
            var result = await _service.GetByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("Vinyl", result.Name);
        }

        [Fact]
        public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((Format?)null);

            // Act
            var result = await _service.GetByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteAsync_WithValidId_DeletesSuccessfully()
        {
            // Arrange
            var format = new Format { Id = 1, Name = "Vinyl", UserId = _userId };
            _mockRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(format);

            _mockRepository.Setup(r => r.Delete(It.IsAny<Format>()));

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _service.DeleteAsync(1);

            // Assert
            Assert.True(result);
            _mockRepository.Verify(r => r.Delete(It.IsAny<Format>()), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WithInvalidId_ReturnsFalse()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((Format?)null);

            // Act
            var result = await _service.DeleteAsync(999);

            // Assert
            Assert.False(result);
            _mockRepository.Verify(r => r.Delete(It.IsAny<Format>()), Times.Never);
        }

        #endregion
    }
}
