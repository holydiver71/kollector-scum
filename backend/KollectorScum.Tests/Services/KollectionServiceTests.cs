using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using KollectorScum.Api.DTOs;
using KollectorScum.Api.Models;
using KollectorScum.Api.Services;
using KollectorScum.Api.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KollectorScum.Tests.Services
{
    /// <summary>
    /// Unit tests for KollectionService
    /// </summary>
    public class KollectionServiceTests : IDisposable
    {
        private readonly KollectorScumDbContext _context;
        private readonly Mock<ILogger<KollectionService>> _mockLogger;
        private readonly KollectionService _service;

        public KollectionServiceTests()
        {
            var options = new DbContextOptionsBuilder<KollectorScumDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new KollectorScumDbContext(options);
            _mockLogger = new Mock<ILogger<KollectionService>>();
            _service = new KollectionService(_context, _mockLogger.Object);

            // Seed test data
            SeedTestData();
        }

        private void SeedTestData()
        {
            var genres = new List<Genre>
            {
                new Genre { Id = 1, Name = "Heavy Metal" },
                new Genre { Id = 2, Name = "Thrash Metal" },
                new Genre { Id = 3, Name = "Death Metal" },
                new Genre { Id = 4, Name = "Rock" },
                new Genre { Id = 5, Name = "Indie" }
            };

            _context.Genres.AddRange(genres);
            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            // Act & Assert
            Assert.NotNull(_service);
        }

        [Fact]
        public void Constructor_WithNullContext_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new KollectionService(null!, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new KollectionService(_context, null!));
        }

        #endregion

        #region CreateAsync Tests

        [Fact]
        public async Task CreateAsync_WithValidData_CreatesKollection()
        {
            // Arrange
            var createDto = new CreateKollectionDto
            {
                Name = "My Metal Collection",
                GenreIds = new List<int> { 1, 2, 3 }
            };

            // Act
            var result = await _service.CreateAsync(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("My Metal Collection", result.Name);
            Assert.Equal(3, result.GenreIds.Count);
            Assert.Contains(1, result.GenreIds);
            Assert.Contains(2, result.GenreIds);
            Assert.Contains(3, result.GenreIds);
            Assert.Equal(3, result.GenreNames.Count);
            Assert.Contains("Heavy Metal", result.GenreNames);
            Assert.Contains("Thrash Metal", result.GenreNames);
            Assert.Contains("Death Metal", result.GenreNames);
        }

        [Fact]
        public async Task CreateAsync_WithDuplicateName_ThrowsArgumentException()
        {
            // Arrange
            var createDto1 = new CreateKollectionDto
            {
                Name = "My Collection",
                GenreIds = new List<int> { 1, 2 }
            };
            await _service.CreateAsync(createDto1);

            var createDto2 = new CreateKollectionDto
            {
                Name = "My Collection",
                GenreIds = new List<int> { 3, 4 }
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(createDto2));
        }

        [Fact]
        public async Task CreateAsync_WithInvalidGenreIds_ThrowsArgumentException()
        {
            // Arrange
            var createDto = new CreateKollectionDto
            {
                Name = "Invalid Collection",
                GenreIds = new List<int> { 1, 999 } // 999 doesn't exist
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(createDto));
        }

        #endregion

        #region GetAllAsync Tests

        [Fact]
        public async Task GetAllAsync_ReturnsAllKollections()
        {
            // Arrange
            await _service.CreateAsync(new CreateKollectionDto
            {
                Name = "Metal Collection",
                GenreIds = new List<int> { 1, 2 }
            });
            await _service.CreateAsync(new CreateKollectionDto
            {
                Name = "Rock Collection",
                GenreIds = new List<int> { 4 }
            });

            // Act
            var result = await _service.GetAllAsync(1, 10);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count());
        }

        [Fact]
        public async Task GetAllAsync_WithSearch_FiltersCorrectly()
        {
            // Arrange
            await _service.CreateAsync(new CreateKollectionDto
            {
                Name = "Metal Collection",
                GenreIds = new List<int> { 1, 2 }
            });
            await _service.CreateAsync(new CreateKollectionDto
            {
                Name = "Rock Collection",
                GenreIds = new List<int> { 4 }
            });

            // Act
            var result = await _service.GetAllAsync(1, 10, "metal");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.TotalCount);
            Assert.Equal("Metal Collection", result.Items.First().Name);
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_WithValidId_ReturnsKollection()
        {
            // Arrange
            var created = await _service.CreateAsync(new CreateKollectionDto
            {
                Name = "Test Collection",
                GenreIds = new List<int> { 1, 2 }
            });

            // Act
            var result = await _service.GetByIdAsync(created.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test Collection", result.Name);
            Assert.Equal(2, result.GenreIds.Count);
        }

        [Fact]
        public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Act
            var result = await _service.GetByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_WithValidData_UpdatesKollection()
        {
            // Arrange
            var created = await _service.CreateAsync(new CreateKollectionDto
            {
                Name = "Original Name",
                GenreIds = new List<int> { 1, 2 }
            });

            var updateDto = new UpdateKollectionDto
            {
                Name = "Updated Name",
                GenreIds = new List<int> { 2, 3, 4 }
            };

            // Act
            var result = await _service.UpdateAsync(created.Id, updateDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Updated Name", result.Name);
            Assert.Equal(3, result.GenreIds.Count);
            Assert.Contains(2, result.GenreIds);
            Assert.Contains(3, result.GenreIds);
            Assert.Contains(4, result.GenreIds);
        }

        [Fact]
        public async Task UpdateAsync_WithDuplicateName_ThrowsArgumentException()
        {
            // Arrange
            await _service.CreateAsync(new CreateKollectionDto
            {
                Name = "Collection 1",
                GenreIds = new List<int> { 1 }
            });
            var created2 = await _service.CreateAsync(new CreateKollectionDto
            {
                Name = "Collection 2",
                GenreIds = new List<int> { 2 }
            });

            var updateDto = new UpdateKollectionDto
            {
                Name = "Collection 1", // Duplicate name
                GenreIds = new List<int> { 2 }
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.UpdateAsync(created2.Id, updateDto));
        }

        [Fact]
        public async Task UpdateAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var updateDto = new UpdateKollectionDto
            {
                Name = "Updated Name",
                GenreIds = new List<int> { 1, 2 }
            };

            // Act
            var result = await _service.UpdateAsync(999, updateDto);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_WithValidId_DeletesKollection()
        {
            // Arrange
            var created = await _service.CreateAsync(new CreateKollectionDto
            {
                Name = "To Delete",
                GenreIds = new List<int> { 1, 2 }
            });

            // Act
            var result = await _service.DeleteAsync(created.Id);

            // Assert
            Assert.True(result);
            var deleted = await _service.GetByIdAsync(created.Id);
            Assert.Null(deleted);
        }

        [Fact]
        public async Task DeleteAsync_WithInvalidId_ReturnsFalse()
        {
            // Act
            var result = await _service.DeleteAsync(999);

            // Assert
            Assert.False(result);
        }

        #endregion
    }
}
