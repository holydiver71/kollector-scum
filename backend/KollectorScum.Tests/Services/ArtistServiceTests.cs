using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using KollectorScum.Api.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;

namespace KollectorScum.Tests.Services
{
    public class ArtistServiceTests
    {
        private readonly Mock<IRepository<Artist>> _mockRepository;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILogger<ArtistService>> _mockLogger;
        private readonly ArtistService _service;

        public ArtistServiceTests()
        {
            _mockRepository = new Mock<IRepository<Artist>>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<ArtistService>>();
            _service = new ArtistService(_mockRepository.Object, _mockUnitOfWork.Object, _mockLogger.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            // Arrange & Act
            var service = new ArtistService(_mockRepository.Object, _mockUnitOfWork.Object, _mockLogger.Object);

            // Assert
            Assert.NotNull(service);
        }

        [Fact]
        public void Constructor_WithNullRepository_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ArtistService(null!, _mockUnitOfWork.Object, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullUnitOfWork_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ArtistService(_mockRepository.Object, null!, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ArtistService(_mockRepository.Object, _mockUnitOfWork.Object, null!));
        }

        #endregion

        #region Validation Tests

        [Fact]
        public async Task CreateAsync_WithValidArtist_Succeeds()
        {
            // Arrange
            var dto = new ArtistDto { Name = "Valid Artist Name" };
            var artist = new Artist { Id = 1, Name = dto.Name };

            _mockRepository.Setup(r => r.AddAsync(It.IsAny<Artist>()))
                .ReturnsAsync(artist);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _service.CreateAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dto.Name, result.Name);
            _mockRepository.Verify(r => r.AddAsync(It.IsAny<Artist>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WithNullName_ThrowsArgumentException()
        {
            // Arrange
            var dto = new ArtistDto { Name = null! };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(dto));
        }

        [Fact]
        public async Task CreateAsync_WithEmptyName_ThrowsArgumentException()
        {
            // Arrange
            var dto = new ArtistDto { Name = string.Empty };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(dto));
        }

        [Fact]
        public async Task CreateAsync_WithWhitespaceName_ThrowsArgumentException()
        {
            // Arrange
            var dto = new ArtistDto { Name = "   " };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(dto));
        }

        [Fact]
        public async Task CreateAsync_WithNameExceeding200Characters_ThrowsArgumentException()
        {
            // Arrange
            var dto = new ArtistDto { Name = new string('A', 201) };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(dto));
        }

        [Fact]
        public async Task CreateAsync_WithNameAt200Characters_Succeeds()
        {
            // Arrange
            var name = new string('A', 200);
            var dto = new ArtistDto { Name = name };
            var artist = new Artist { Id = 1, Name = name };

            _mockRepository.Setup(r => r.AddAsync(It.IsAny<Artist>()))
                .ReturnsAsync(artist);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _service.CreateAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(name, result.Name);
        }

        [Fact]
        public async Task UpdateAsync_WithValidArtist_Succeeds()
        {
            // Arrange
            var dto = new ArtistDto { Id = 1, Name = "Updated Artist" };
            var artist = new Artist { Id = 1, Name = "Original Artist" };

            _mockRepository.Setup(r => r.GetByIdAsync(dto.Id))
                .ReturnsAsync(artist);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _service.UpdateAsync(dto.Id, dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dto.Name, result.Name);
            _mockRepository.Verify(r => r.Update(artist), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WithInvalidName_ThrowsArgumentException()
        {
            // Arrange
            var existingArtist = new Artist { Id = 1, Name = "Old Name" };
            var dto = new ArtistDto { Id = 1, Name = string.Empty };

            _mockRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existingArtist);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateAsync(dto.Id, dto));
        }

        #endregion

        #region Search Tests

        [Fact]
        public async Task GetAllAsync_WithSearch_UsesSearchFilter()
        {
            // Arrange
            var pagedResult = new PagedResult<Artist>
            {
                Items = new List<Artist>
                {
                    new Artist { Id = 1, Name = "The Beatles" }
                },
                TotalCount = 1,
                Page = 1,
                PageSize = 50,
                TotalPages = 1
            };

            _mockRepository.Setup(r => r.GetPagedAsync(
                1,
                50,
                It.IsAny<Expression<Func<Artist, bool>>>(),
                It.IsAny<Func<IQueryable<Artist>, IOrderedQueryable<Artist>>>(),
                ""))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _service.GetAllAsync(1, 50, "beatles");

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Items);
            _mockRepository.Verify(r => r.GetPagedAsync(
                1,
                50,
                It.IsAny<Expression<Func<Artist, bool>>>(),
                It.IsAny<Func<IQueryable<Artist>, IOrderedQueryable<Artist>>>(),
                ""), Times.Once);
        }

        #endregion

        #region CRUD Tests

        [Fact]
        public async Task GetByIdAsync_WithValidId_ReturnsArtist()
        {
            // Arrange
            var artist = new Artist { Id = 1, Name = "Test Artist" };
            _mockRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(artist);

            // Act
            var result = await _service.GetByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(artist.Name, result.Name);
        }

        [Fact]
        public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((Artist?)null);

            // Act
            var result = await _service.GetByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteAsync_WithValidId_ReturnsTrue()
        {
            // Arrange
            var artist = new Artist { Id = 1, Name = "Test Artist" };
            _mockRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(artist);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _service.DeleteAsync(1);

            // Assert
            Assert.True(result);
            _mockRepository.Verify(r => r.Delete(artist), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WithInvalidId_ReturnsFalse()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((Artist?)null);

            // Act
            var result = await _service.DeleteAsync(999);

            // Assert
            Assert.False(result);
            _mockRepository.Verify(r => r.Delete(It.IsAny<Artist>()), Times.Never);
        }

        #endregion
    }
}
