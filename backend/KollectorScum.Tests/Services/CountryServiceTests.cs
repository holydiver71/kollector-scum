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
    /// Unit tests for CountryService
    /// </summary>
    public class CountryServiceTests
    {
        private readonly Mock<IRepository<Country>> _mockRepository;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILogger<CountryService>> _mockLogger;
        private readonly Mock<IUserContext> _mockUserContext;
        private readonly CountryService _service;
        private readonly Guid _userId;

        public CountryServiceTests()
        {
            _mockRepository = new Mock<IRepository<Country>>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<CountryService>>();
            _mockUserContext = new Mock<IUserContext>();
            var defaultUserId = Guid.Parse("12337b39-c346-449c-b269-33b2e820d74f");
            _mockUserContext.Setup(u => u.GetActingUserId()).Returns(defaultUserId);
            _mockUserContext.Setup(u => u.GetUserId()).Returns(defaultUserId);
            _mockUserContext.Setup(u => u.IsAdmin()).Returns(false);
            _userId = Guid.NewGuid();
            _mockUserContext.Setup(x => x.GetActingUserId()).Returns(_userId);
            _service = new CountryService(_mockRepository.Object, _mockUnitOfWork.Object, _mockLogger.Object, _mockUserContext.Object);
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
                new CountryService(null!, _mockUnitOfWork.Object, _mockLogger.Object, _mockUserContext.Object));
        }

        [Fact]
        public void Constructor_WithNullUnitOfWork_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new CountryService(_mockRepository.Object, null!, _mockLogger.Object, _mockUserContext.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new CountryService(_mockRepository.Object, _mockUnitOfWork.Object, null!, _mockUserContext.Object));
        }

        [Fact]
        public void Constructor_WithNullUserContext_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new CountryService(_mockRepository.Object, _mockUnitOfWork.Object, _mockLogger.Object, null!));
        }

        #endregion

        #region Validation Tests

        [Fact]
        public async Task CreateAsync_WithValidCountry_CreatesSuccessfully()
        {
            // Arrange
            var countryDto = new CountryDto { Name = "USA" };
            var pagedResult = new PagedResult<Country>
            {
                Items = new System.Collections.Generic.List<Country>(),
                TotalCount = 0,
                Page = 1,
                PageSize = 1,
                TotalPages = 0
            };

            _mockRepository.Setup(r => r.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<Country, bool>>>(),
                It.IsAny<Func<IQueryable<Country>, IOrderedQueryable<Country>>>(),
                It.IsAny<string>()))
                .ReturnsAsync(pagedResult);

            _mockRepository.Setup(r => r.AddAsync(It.IsAny<Country>()))
                .ReturnsAsync(new Country { Id = 1, Name = "USA", UserId = _userId });

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _service.CreateAsync(countryDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("USA", result.Name);
        }

        [Fact]
        public async Task CreateAsync_WithNullName_ThrowsArgumentException()
        {
            // Arrange
            var countryDto = new CountryDto { Name = null! };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(countryDto));
        }

        [Fact]
        public async Task CreateAsync_WithEmptyName_ThrowsArgumentException()
        {
            // Arrange
            var countryDto = new CountryDto { Name = "" };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(countryDto));
        }

        [Fact]
        public async Task CreateAsync_WithWhitespaceName_ThrowsArgumentException()
        {
            // Arrange
            var countryDto = new CountryDto { Name = "   " };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(countryDto));
        }

        [Fact]
        public async Task CreateAsync_WithNameOver100Characters_ThrowsArgumentException()
        {
            // Arrange
            var countryDto = new CountryDto { Name = new string('A', 101) };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(countryDto));
            Assert.Contains("cannot exceed 100 characters", exception.Message);
        }

        [Fact]
        public async Task CreateAsync_WithNameExactly100Characters_CreatesSuccessfully()
        {
            // Arrange
            var countryDto = new CountryDto { Name = new string('A', 100) };
            var pagedResult = new PagedResult<Country>
            {
                Items = new System.Collections.Generic.List<Country>(),
                TotalCount = 0,
                Page = 1,
                PageSize = 1,
                TotalPages = 0
            };

            _mockRepository.Setup(r => r.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<Country, bool>>>(),
                It.IsAny<Func<IQueryable<Country>, IOrderedQueryable<Country>>>(),
                It.IsAny<string>()))
                .ReturnsAsync(pagedResult);

            _mockRepository.Setup(r => r.AddAsync(It.IsAny<Country>()))
                .ReturnsAsync(new Country { Id = 1, Name = countryDto.Name, UserId = _userId });

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _service.CreateAsync(countryDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(100, result.Name.Length);
        }

        [Fact]
        public async Task UpdateAsync_WithValidCountry_UpdatesSuccessfully()
        {
            // Arrange
            var existingCountry = new Country { Id = 1, Name = "Old Name", UserId = _userId };
            var countryDto = new CountryDto { Id = 1, Name = "New Name" };

            _mockRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existingCountry);

            _mockRepository.Setup(r => r.Update(It.IsAny<Country>()));

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _service.UpdateAsync(1, countryDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("New Name", result.Name);
            _mockRepository.Verify(r => r.Update(It.IsAny<Country>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WithInvalidName_ThrowsArgumentException()
        {
            // Arrange
            var existingCountry = new Country { Id = 1, Name = "Old Name", UserId = _userId };
            var countryDto = new CountryDto { Id = 1, Name = "" };

            _mockRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existingCountry);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateAsync(1, countryDto));
        }

        #endregion

        #region Search Tests

        [Fact]
        public async Task GetAllAsync_WithSearch_UsesSearchFilter()
        {
            // Arrange
            var pagedResult = new PagedResult<Country>
            {
                Items = new System.Collections.Generic.List<Country>
                {
                    new Country { Id = 1, Name = "USA", UserId = _userId }
                },
                TotalCount = 1,
                Page = 1,
                PageSize = 50,
                TotalPages = 1
            };

            _mockRepository.Setup(r => r.GetPagedAsync(
                1,
                50,
                It.IsAny<Expression<Func<Country, bool>>>(),
                It.IsAny<Func<IQueryable<Country>, IOrderedQueryable<Country>>>(),
                ""))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _service.GetAllAsync(1, 50, "usa");

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Items);
            _mockRepository.Verify(r => r.GetPagedAsync(
                1,
                50,
                It.IsAny<Expression<Func<Country, bool>>>(),
                It.IsAny<Func<IQueryable<Country>, IOrderedQueryable<Country>>>(),
                ""), Times.Once);
        }

        #endregion

        #region CRUD Operation Tests

        [Fact]
        public async Task GetByIdAsync_WithValidId_ReturnsCountry()
        {
            // Arrange
            var country = new Country { Id = 1, Name = "USA", UserId = _userId };
            _mockRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(country);

            // Act
            var result = await _service.GetByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("USA", result.Name);
        }

        [Fact]
        public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((Country?)null);

            // Act
            var result = await _service.GetByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteAsync_WithValidId_DeletesSuccessfully()
        {
            // Arrange
            var country = new Country { Id = 1, Name = "USA", UserId = _userId };
            _mockRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(country);

            _mockRepository.Setup(r => r.Delete(It.IsAny<Country>()));

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _service.DeleteAsync(1);

            // Assert
            Assert.True(result);
            _mockRepository.Verify(r => r.Delete(It.IsAny<Country>()), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WithInvalidId_ReturnsFalse()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((Country?)null);

            // Act
            var result = await _service.DeleteAsync(999);

            // Assert
            Assert.False(result);
            _mockRepository.Verify(r => r.Delete(It.IsAny<Country>()), Times.Never);
        }

        #endregion
    }
}
