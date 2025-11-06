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
    /// Unit tests for StoreService
    /// </summary>
    public class StoreServiceTests
    {
        private readonly Mock<IRepository<Store>> _mockRepository;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILogger<StoreService>> _mockLogger;
        private readonly StoreService _service;

        public StoreServiceTests()
        {
            _mockRepository = new Mock<IRepository<Store>>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<StoreService>>();
            _service = new StoreService(_mockRepository.Object, _mockUnitOfWork.Object, _mockLogger.Object);
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
                new StoreService(null!, _mockUnitOfWork.Object, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullUnitOfWork_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new StoreService(_mockRepository.Object, null!, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new StoreService(_mockRepository.Object, _mockUnitOfWork.Object, null!));
        }

        #endregion

        #region Validation Tests

        [Fact]
        public async Task CreateAsync_WithValidStore_CreatesSuccessfully()
        {
            // Arrange
            var storeDto = new StoreDto { Name = "Tower Records" };
            var pagedResult = new PagedResult<Store>
            {
                Items = new System.Collections.Generic.List<Store>(),
                TotalCount = 0,
                Page = 1,
                PageSize = 1,
                TotalPages = 0
            };

            _mockRepository.Setup(r => r.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<Store, bool>>>(),
                It.IsAny<Func<IQueryable<Store>, IOrderedQueryable<Store>>>(),
                It.IsAny<string>()))
                .ReturnsAsync(pagedResult);

            _mockRepository.Setup(r => r.AddAsync(It.IsAny<Store>()))
                .ReturnsAsync(new Store { Id = 1, Name = "Tower Records" });

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _service.CreateAsync(storeDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Tower Records", result.Name);
        }

        [Fact]
        public async Task CreateAsync_WithNullName_ThrowsArgumentException()
        {
            // Arrange
            var storeDto = new StoreDto { Name = null! };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(storeDto));
        }

        [Fact]
        public async Task CreateAsync_WithEmptyName_ThrowsArgumentException()
        {
            // Arrange
            var storeDto = new StoreDto { Name = "" };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(storeDto));
        }

        [Fact]
        public async Task CreateAsync_WithWhitespaceName_ThrowsArgumentException()
        {
            // Arrange
            var storeDto = new StoreDto { Name = "   " };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(storeDto));
        }

        [Fact]
        public async Task CreateAsync_WithNameOver100Characters_ThrowsArgumentException()
        {
            // Arrange
            var storeDto = new StoreDto { Name = new string('A', 201) };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(storeDto));
            Assert.Contains("cannot exceed 200 characters", exception.Message);
        }

        [Fact]
        public async Task CreateAsync_WithNameExactly100Characters_CreatesSuccessfully()
        {
            // Arrange
            var storeDto = new StoreDto { Name = new string('A', 200) };
            var pagedResult = new PagedResult<Store>
            {
                Items = new System.Collections.Generic.List<Store>(),
                TotalCount = 0,
                Page = 1,
                PageSize = 1,
                TotalPages = 0
            };

            _mockRepository.Setup(r => r.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<Store, bool>>>(),
                It.IsAny<Func<IQueryable<Store>, IOrderedQueryable<Store>>>(),
                It.IsAny<string>()))
                .ReturnsAsync(pagedResult);

            _mockRepository.Setup(r => r.AddAsync(It.IsAny<Store>()))
                .ReturnsAsync(new Store { Id = 1, Name = storeDto.Name });

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _service.CreateAsync(storeDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result.Name.Length);
        }

        [Fact]
        public async Task UpdateAsync_WithValidStore_UpdatesSuccessfully()
        {
            // Arrange
            var existingStore = new Store { Id = 1, Name = "Old Name" };
            var storeDto = new StoreDto { Id = 1, Name = "New Name" };

            _mockRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existingStore);

            _mockRepository.Setup(r => r.Update(It.IsAny<Store>()));

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _service.UpdateAsync(1, storeDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("New Name", result.Name);
            _mockRepository.Verify(r => r.Update(It.IsAny<Store>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WithInvalidName_ThrowsArgumentException()
        {
            // Arrange
            var existingStore = new Store { Id = 1, Name = "Old Name" };
            var storeDto = new StoreDto { Id = 1, Name = "" };

            _mockRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existingStore);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateAsync(1, storeDto));
        }

        #endregion

        #region Search Tests

        [Fact]
        public async Task GetAllAsync_WithSearch_UsesSearchFilter()
        {
            // Arrange
            var pagedResult = new PagedResult<Store>
            {
                Items = new System.Collections.Generic.List<Store>
                {
                    new Store { Id = 1, Name = "Tower Records" }
                },
                TotalCount = 1,
                Page = 1,
                PageSize = 50,
                TotalPages = 1
            };

            _mockRepository.Setup(r => r.GetPagedAsync(
                1,
                50,
                It.IsAny<Expression<Func<Store, bool>>>(),
                It.IsAny<Func<IQueryable<Store>, IOrderedQueryable<Store>>>(),
                ""))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _service.GetAllAsync(1, 50, "tower");

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Items);
            _mockRepository.Verify(r => r.GetPagedAsync(
                1,
                50,
                It.IsAny<Expression<Func<Store, bool>>>(),
                It.IsAny<Func<IQueryable<Store>, IOrderedQueryable<Store>>>(),
                ""), Times.Once);
        }

        #endregion

        #region CRUD Operation Tests

        [Fact]
        public async Task GetByIdAsync_WithValidId_ReturnsStore()
        {
            // Arrange
            var store = new Store { Id = 1, Name = "Tower Records" };
            _mockRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(store);

            // Act
            var result = await _service.GetByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("Tower Records", result.Name);
        }

        [Fact]
        public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((Store?)null);

            // Act
            var result = await _service.GetByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteAsync_WithValidId_DeletesSuccessfully()
        {
            // Arrange
            var store = new Store { Id = 1, Name = "Tower Records" };
            _mockRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(store);

            _mockRepository.Setup(r => r.Delete(It.IsAny<Store>()));

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _service.DeleteAsync(1);

            // Assert
            Assert.True(result);
            _mockRepository.Verify(r => r.Delete(It.IsAny<Store>()), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WithInvalidId_ReturnsFalse()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((Store?)null);

            // Act
            var result = await _service.DeleteAsync(999);

            // Assert
            Assert.False(result);
            _mockRepository.Verify(r => r.Delete(It.IsAny<Store>()), Times.Never);
        }

        #endregion
    }
}
