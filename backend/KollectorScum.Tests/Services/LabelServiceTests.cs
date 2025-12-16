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
    /// Unit tests for LabelService
    /// </summary>
    public class LabelServiceTests
    {
        private readonly Mock<IRepository<Label>> _mockRepository;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILogger<LabelService>> _mockLogger;
        private readonly Mock<IUserContext> _mockUserContext;
        private readonly LabelService _service;
        private readonly Guid _userId;

        public LabelServiceTests()
        {
            _mockRepository = new Mock<IRepository<Label>>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<LabelService>>();
            _mockUserContext = new Mock<IUserContext>();
            var defaultUserId = Guid.Parse("12337b39-c346-449c-b269-33b2e820d74f");
            _mockUserContext.Setup(u => u.GetActingUserId()).Returns(defaultUserId);
            _mockUserContext.Setup(u => u.GetUserId()).Returns(defaultUserId);
            _mockUserContext.Setup(u => u.IsAdmin()).Returns(false);
            _userId = Guid.NewGuid();
            _mockUserContext.Setup(x => x.GetActingUserId()).Returns(_userId);
            _service = new LabelService(_mockRepository.Object, _mockUnitOfWork.Object, _mockLogger.Object, _mockUserContext.Object);
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
                new LabelService(null!, _mockUnitOfWork.Object, _mockLogger.Object, _mockUserContext.Object));
        }

        [Fact]
        public void Constructor_WithNullUnitOfWork_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new LabelService(_mockRepository.Object, null!, _mockLogger.Object, _mockUserContext.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new LabelService(_mockRepository.Object, _mockUnitOfWork.Object, null!, _mockUserContext.Object));
        }

        [Fact]
        public void Constructor_WithNullUserContext_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new LabelService(_mockRepository.Object, _mockUnitOfWork.Object, _mockLogger.Object, null!));
        }

        #endregion

        #region Validation Tests

        [Fact]
        public async Task CreateAsync_WithValidLabel_CreatesSuccessfully()
        {
            // Arrange
            var labelDto = new LabelDto { Name = "Blue Note" };
            var pagedResult = new PagedResult<Label>
            {
                Items = new System.Collections.Generic.List<Label>(),
                TotalCount = 0,
                Page = 1,
                PageSize = 1,
                TotalPages = 0
            };

            _mockRepository.Setup(r => r.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<Label, bool>>>(),
                It.IsAny<Func<IQueryable<Label>, IOrderedQueryable<Label>>>(),
                It.IsAny<string>()))
                .ReturnsAsync(pagedResult);

            _mockRepository.Setup(r => r.AddAsync(It.IsAny<Label>()))
                .ReturnsAsync(new Label { Id = 1, Name = "Blue Note", UserId = _userId });

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _service.CreateAsync(labelDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Blue Note", result.Name);
        }

        [Fact]
        public async Task CreateAsync_WithNullName_ThrowsArgumentException()
        {
            // Arrange
            var labelDto = new LabelDto { Name = null! };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(labelDto));
        }

        [Fact]
        public async Task CreateAsync_WithEmptyName_ThrowsArgumentException()
        {
            // Arrange
            var labelDto = new LabelDto { Name = "" };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(labelDto));
        }

        [Fact]
        public async Task CreateAsync_WithWhitespaceName_ThrowsArgumentException()
        {
            // Arrange
            var labelDto = new LabelDto { Name = "   " };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(labelDto));
        }

        [Fact]
        public async Task CreateAsync_WithNameOver100Characters_ThrowsArgumentException()
        {
            // Arrange
            var labelDto = new LabelDto { Name = new string('A', 201) };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(labelDto));
            Assert.Contains("cannot exceed 200 characters", exception.Message);
        }

        [Fact]
        public async Task CreateAsync_WithNameExactly100Characters_CreatesSuccessfully()
        {
            // Arrange
            var labelDto = new LabelDto { Name = new string('A', 200) };
            var pagedResult = new PagedResult<Label>
            {
                Items = new System.Collections.Generic.List<Label>(),
                TotalCount = 0,
                Page = 1,
                PageSize = 1,
                TotalPages = 0
            };

            _mockRepository.Setup(r => r.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<Label, bool>>>(),
                It.IsAny<Func<IQueryable<Label>, IOrderedQueryable<Label>>>(),
                It.IsAny<string>()))
                .ReturnsAsync(pagedResult);

            _mockRepository.Setup(r => r.AddAsync(It.IsAny<Label>()))
                .ReturnsAsync(new Label { Id = 1, Name = labelDto.Name, UserId = _userId });

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _service.CreateAsync(labelDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result.Name.Length);
        }

        [Fact]
        public async Task UpdateAsync_WithValidLabel_UpdatesSuccessfully()
        {
            // Arrange
            var existingLabel = new Label { Id = 1, Name = "Old Name", UserId = _userId };
            var labelDto = new LabelDto { Id = 1, Name = "New Name" };

            _mockRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existingLabel);

            _mockRepository.Setup(r => r.Update(It.IsAny<Label>()));

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _service.UpdateAsync(1, labelDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("New Name", result.Name);
            _mockRepository.Verify(r => r.Update(It.IsAny<Label>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WithInvalidName_ThrowsArgumentException()
        {
            // Arrange
            var existingLabel = new Label { Id = 1, Name = "Old Name", UserId = _userId };
            var labelDto = new LabelDto { Id = 1, Name = "" };

            _mockRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existingLabel);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateAsync(1, labelDto));
        }

        #endregion

        #region Search Tests

        [Fact]
        public async Task GetAllAsync_WithSearch_UsesSearchFilter()
        {
            // Arrange
            var pagedResult = new PagedResult<Label>
            {
                Items = new System.Collections.Generic.List<Label>
                {
                    new Label { Id = 1, Name = "Blue Note", UserId = _userId }
                },
                TotalCount = 1,
                Page = 1,
                PageSize = 50,
                TotalPages = 1
            };

            _mockRepository.Setup(r => r.GetPagedAsync(
                1,
                50,
                It.IsAny<Expression<Func<Label, bool>>>(),
                It.IsAny<Func<IQueryable<Label>, IOrderedQueryable<Label>>>(),
                ""))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _service.GetAllAsync(1, 50, "blue");

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Items);
            _mockRepository.Verify(r => r.GetPagedAsync(
                1,
                50,
                It.IsAny<Expression<Func<Label, bool>>>(),
                It.IsAny<Func<IQueryable<Label>, IOrderedQueryable<Label>>>(),
                ""), Times.Once);
        }

        #endregion

        #region CRUD Operation Tests

        [Fact]
        public async Task GetByIdAsync_WithValidId_ReturnsLabel()
        {
            // Arrange
            var label = new Label { Id = 1, Name = "Blue Note", UserId = _userId };
            _mockRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(label);

            // Act
            var result = await _service.GetByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("Blue Note", result.Name);
        }

        [Fact]
        public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((Label?)null);

            // Act
            var result = await _service.GetByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteAsync_WithValidId_DeletesSuccessfully()
        {
            // Arrange
            var label = new Label { Id = 1, Name = "Blue Note", UserId = _userId };
            _mockRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(label);

            _mockRepository.Setup(r => r.Delete(It.IsAny<Label>()));

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _service.DeleteAsync(1);

            // Assert
            Assert.True(result);
            _mockRepository.Verify(r => r.Delete(It.IsAny<Label>()), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WithInvalidId_ReturnsFalse()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((Label?)null);

            // Act
            var result = await _service.DeleteAsync(999);

            // Assert
            Assert.False(result);
            _mockRepository.Verify(r => r.Delete(It.IsAny<Label>()), Times.Never);
        }

        #endregion
    }
}
