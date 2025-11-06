using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using KollectorScum.Api.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace KollectorScum.Tests.Services
{
    /// <summary>
    /// Unit tests for GenericCrudService using ArtistService as concrete implementation
    /// </summary>
    public class GenericCrudServiceTests
    {
        private readonly Mock<IRepository<Artist>> _mockRepository;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILogger<ArtistService>> _mockLogger;
        private readonly ArtistService _service;

        public GenericCrudServiceTests()
        {
            _mockRepository = new Mock<IRepository<Artist>>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<ArtistService>>();
            _service = new ArtistService(_mockRepository.Object, _mockUnitOfWork.Object, _mockLogger.Object);
        }

        #region GetAllAsync Tests

        [Fact]
        public async Task GetAllAsync_WithoutSearch_ReturnsPagedResults()
        {
            // Arrange
            var pagedResult = new PagedResult<Artist>
            {
                Items = new List<Artist>
                {
                    new Artist { Id = 1, Name = "Artist 1" },
                    new Artist { Id = 2, Name = "Artist 2" },
                    new Artist { Id = 3, Name = "Artist 3" }
                },
                TotalCount = 3,
                Page = 1,
                PageSize = 10,
                TotalPages = 1
            };

            _mockRepository.Setup(r => r.GetPagedAsync(
                1,
                10,
                It.IsAny<Expression<Func<Artist, bool>>>(),
                It.IsAny<Func<IQueryable<Artist>, IOrderedQueryable<Artist>>>(),
                ""))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _service.GetAllAsync(page: 1, pageSize: 10);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.TotalCount);
            Assert.Equal(3, result.Items.Count());
            Assert.Equal(1, result.Page);
            Assert.Equal(10, result.PageSize);
        }

        [Fact]
        public async Task GetAllAsync_WithSearch_ReturnsFilteredResults()
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
                PageSize = 10,
                TotalPages = 1
            };

            _mockRepository.Setup(r => r.GetPagedAsync(
                1,
                10,
                It.IsAny<Expression<Func<Artist, bool>>>(),
                It.IsAny<Func<IQueryable<Artist>, IOrderedQueryable<Artist>>>(),
                ""))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _service.GetAllAsync(page: 1, pageSize: 10, search: "beatles");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.TotalCount);
            Assert.Single(result.Items);
        }

        [Fact]
        public async Task GetAllAsync_EmptyResults_ReturnsEmptyPagedResult()
        {
            // Arrange
            var pagedResult = new PagedResult<Artist>
            {
                Items = new List<Artist>(),
                TotalCount = 0,
                Page = 1,
                PageSize = 10,
                TotalPages = 0
            };

            _mockRepository.Setup(r => r.GetPagedAsync(
                1,
                10,
                It.IsAny<Expression<Func<Artist, bool>>>(),
                It.IsAny<Func<IQueryable<Artist>, IOrderedQueryable<Artist>>>(),
                ""))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _service.GetAllAsync(page: 1, pageSize: 10);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.TotalCount);
            Assert.Empty(result.Items);
        }

        [Fact]
        public async Task GetAllAsync_CalculatesPagesCorrectly()
        {
            // Arrange
            var pagedResult = new PagedResult<Artist>
            {
                Items = new List<Artist>
                {
                    new Artist { Id = 1, Name = "Artist 1" },
                    new Artist { Id = 2, Name = "Artist 2" }
                },
                TotalCount = 25,
                Page = 1,
                PageSize = 10,
                TotalPages = 3
            };

            _mockRepository.Setup(r => r.GetPagedAsync(
                1,
                10,
                It.IsAny<Expression<Func<Artist, bool>>>(),
                It.IsAny<Func<IQueryable<Artist>, IOrderedQueryable<Artist>>>(),
                ""))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _service.GetAllAsync(page: 1, pageSize: 10);

            // Assert
            Assert.Equal(3, result.TotalPages);
            Assert.True(result.HasNext);
            Assert.False(result.HasPrevious);
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsDto()
        {
            // Arrange
            var artist = new Artist { Id = 1, Name = "Test Artist" };
            _mockRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(artist);

            // Act
            var result = await _service.GetByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("Test Artist", result.Name);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ReturnsNull()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((Artist?)null);

            // Act
            var result = await _service.GetByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region CreateAsync Tests

        [Fact]
        public async Task CreateAsync_ValidDto_CreatesAndReturnsDto()
        {
            // Arrange
            var dto = new ArtistDto { Name = "New Artist" };
            var createdArtist = new Artist { Id = 1, Name = "New Artist" };

            _mockRepository.Setup(r => r.AddAsync(It.IsAny<Artist>()))
                .ReturnsAsync(createdArtist);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _service.CreateAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("New Artist", result.Name);
            _mockRepository.Verify(r => r.AddAsync(It.IsAny<Artist>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_NullName_ThrowsArgumentException()
        {
            // Arrange
            var dto = new ArtistDto { Name = null! };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(dto));
            _mockRepository.Verify(r => r.AddAsync(It.IsAny<Artist>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_EmptyName_ThrowsArgumentException()
        {
            // Arrange
            var dto = new ArtistDto { Name = "" };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(dto));
            _mockRepository.Verify(r => r.AddAsync(It.IsAny<Artist>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_NameTooLong_ThrowsArgumentException()
        {
            // Arrange
            var dto = new ArtistDto { Name = new string('A', 201) }; // 201 characters

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(dto));
            _mockRepository.Verify(r => r.AddAsync(It.IsAny<Artist>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WhitespaceName_ThrowsArgumentException()
        {
            // Arrange
            var dto = new ArtistDto { Name = "   " };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(dto));
            Assert.Contains("Artist name is required", exception.Message);
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_ExistingEntity_UpdatesAndReturnsDto()
        {
            // Arrange
            var existingArtist = new Artist { Id = 1, Name = "Old Name" };
            var updateDto = new ArtistDto { Id = 1, Name = "New Name" };

            _mockRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existingArtist);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _service.UpdateAsync(1, updateDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("New Name", result.Name);
            Assert.Equal("New Name", existingArtist.Name);
            _mockRepository.Verify(r => r.Update(existingArtist), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_NonExistingEntity_ReturnsNull()
        {
            // Arrange
            var updateDto = new ArtistDto { Id = 999, Name = "New Name" };
            _mockRepository.Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((Artist?)null);

            // Act
            var result = await _service.UpdateAsync(999, updateDto);

            // Assert
            Assert.Null(result);
            _mockRepository.Verify(r => r.Update(It.IsAny<Artist>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_InvalidDto_ThrowsArgumentException()
        {
            // Arrange
            var existingArtist = new Artist { Id = 1, Name = "Old Name" };
            var updateDto = new ArtistDto { Id = 1, Name = "" };

            _mockRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existingArtist);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateAsync(1, updateDto));
            _mockRepository.Verify(r => r.Update(It.IsAny<Artist>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_NameExactly200Characters_Succeeds()
        {
            // Arrange
            var existingArtist = new Artist { Id = 1, Name = "Old Name" };
            var updateDto = new ArtistDto { Id = 1, Name = new string('A', 200) };

            _mockRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existingArtist);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _service.UpdateAsync(1, updateDto);

            // Assert
            Assert.NotNull(result);
            _mockRepository.Verify(r => r.Update(existingArtist), Times.Once);
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_ExistingEntity_DeletesAndReturnsTrue()
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
        public async Task DeleteAsync_NonExistingEntity_ReturnsFalse()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((Artist?)null);

            // Act
            var result = await _service.DeleteAsync(999);

            // Assert
            Assert.False(result);
            _mockRepository.Verify(r => r.Delete(It.IsAny<Artist>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        #endregion

        #region Search and Ordering Tests

        [Fact]
        public async Task GetAllAsync_WithSearch_CallsRepositoryWithFilter()
        {
            // Arrange
            var pagedResult = new PagedResult<Artist>
            {
                Items = new List<Artist> { new Artist { Id = 1, Name = "The Beatles" } },
                TotalCount = 1,
                Page = 1,
                PageSize = 10,
                TotalPages = 1
            };

            _mockRepository.Setup(r => r.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<Artist, bool>>>(),
                It.IsAny<Func<IQueryable<Artist>, IOrderedQueryable<Artist>>>(),
                It.IsAny<string>()))
                .ReturnsAsync(pagedResult);

            // Act
            await _service.GetAllAsync(page: 1, pageSize: 10, search: "beatles");

            // Assert
            _mockRepository.Verify(r => r.GetPagedAsync(
                1, 
                10, 
                It.IsAny<Expression<Func<Artist, bool>>>(),
                It.IsAny<Func<IQueryable<Artist>, IOrderedQueryable<Artist>>>(),
                ""), 
                Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_WithoutSearch_UsesDefaultOrdering()
        {
            // Arrange
            var pagedResult = new PagedResult<Artist>
            {
                Items = new List<Artist>
                {
                    new Artist { Id = 1, Name = "Artist A" },
                    new Artist { Id = 2, Name = "Artist B" }
                },
                TotalCount = 2,
                Page = 1,
                PageSize = 10,
                TotalPages = 1
            };

            _mockRepository.Setup(r => r.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<Artist, bool>>>(),
                It.IsAny<Func<IQueryable<Artist>, IOrderedQueryable<Artist>>>(),
                It.IsAny<string>()))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _service.GetAllAsync(page: 1, pageSize: 10);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            _mockRepository.Verify(r => r.GetPagedAsync(
                1,
                10,
                It.IsAny<Expression<Func<Artist, bool>>>(),
                It.IsAny<Func<IQueryable<Artist>, IOrderedQueryable<Artist>>>(),
                ""),
                Times.Once);
        }

        #endregion
    }
}
