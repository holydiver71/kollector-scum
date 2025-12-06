using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using KollectorScum.Api.Controllers;
using KollectorScum.Api.DTOs;
using KollectorScum.Api.Models;
using KollectorScum.Api.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KollectorScum.Tests.Controllers
{
    /// <summary>
    /// Unit tests for KollectionsController
    /// </summary>
    public class KollectionsControllerTests : IDisposable
    {
        private readonly KollectorScumDbContext _context;
        private readonly Mock<ILogger<KollectionsController>> _mockLogger;
        private readonly KollectionsController _controller;

        public KollectionsControllerTests()
        {
            // Setup in-memory database for testing
            var options = new DbContextOptionsBuilder<KollectorScumDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new KollectorScumDbContext(options);
            _mockLogger = new Mock<ILogger<KollectionsController>>();
            _controller = new KollectionsController(_context, _mockLogger.Object);

            // Seed test data
            SeedTestData();
        }

        private void SeedTestData()
        {
            // Add test music releases
            var releases = new List<MusicRelease>
            {
                new MusicRelease { Id = 1, Title = "Test Album 1", DateAdded = DateTime.UtcNow, LastModified = DateTime.UtcNow },
                new MusicRelease { Id = 2, Title = "Test Album 2", DateAdded = DateTime.UtcNow, LastModified = DateTime.UtcNow },
                new MusicRelease { Id = 3, Title = "Test Album 3", DateAdded = DateTime.UtcNow, LastModified = DateTime.UtcNow }
            };

            _context.MusicReleases.AddRange(releases);
            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        #region GetKollections Tests

        [Fact]
        public async Task GetKollections_EmptyDatabase_ReturnsEmptyList()
        {
            // Act
            var result = await _controller.GetKollections();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<List<KollectionSummaryDto>>(okResult.Value);
            Assert.Empty(returnValue);
        }

        [Fact]
        public async Task GetKollections_WithData_ReturnsKollections()
        {
            // Arrange
            var kollection = new Kollection
            {
                Name = "My Metal Collection",
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };
            _context.Kollections.Add(kollection);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetKollections();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<List<KollectionSummaryDto>>(okResult.Value);
            Assert.Single(returnValue);
            Assert.Equal("My Metal Collection", returnValue[0].Name);
            Assert.Equal(0, returnValue[0].ItemCount);
        }

        #endregion

        #region GetKollection Tests

        [Fact]
        public async Task GetKollection_ExistingId_ReturnsKollection()
        {
            // Arrange
            var kollection = new Kollection
            {
                Name = "Test Collection",
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };
            _context.Kollections.Add(kollection);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetKollection(kollection.Id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<KollectionDto>(okResult.Value);
            Assert.Equal("Test Collection", returnValue.Name);
            Assert.Empty(returnValue.Releases);
        }

        [Fact]
        public async Task GetKollection_NonExistentId_ReturnsNotFound()
        {
            // Act
            var result = await _controller.GetKollection(999);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        #endregion

        #region CreateKollection Tests

        [Fact]
        public async Task CreateKollection_ValidData_ReturnsCreated()
        {
            // Arrange
            var createDto = new CreateKollectionDto
            {
                Name = "New Collection"
            };

            // Act
            var result = await _controller.CreateKollection(createDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returnValue = Assert.IsType<KollectionSummaryDto>(createdResult.Value);
            Assert.Equal("New Collection", returnValue.Name);
            Assert.Equal(0, returnValue.ItemCount);

            // Verify it was saved to database
            var savedKollection = await _context.Kollections.FirstOrDefaultAsync(k => k.Name == "New Collection");
            Assert.NotNull(savedKollection);
        }

        [Fact]
        public async Task CreateKollection_EmptyName_ReturnsBadRequest()
        {
            // Arrange
            var createDto = new CreateKollectionDto
            {
                Name = ""
            };

            // Act
            var result = await _controller.CreateKollection(createDto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task CreateKollection_DuplicateName_ReturnsBadRequest()
        {
            // Arrange
            var existingKollection = new Kollection
            {
                Name = "Existing Collection",
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };
            _context.Kollections.Add(existingKollection);
            await _context.SaveChangesAsync();

            var createDto = new CreateKollectionDto
            {
                Name = "Existing Collection"
            };

            // Act
            var result = await _controller.CreateKollection(createDto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        #endregion

        #region UpdateKollection Tests

        [Fact]
        public async Task UpdateKollection_ValidData_ReturnsUpdated()
        {
            // Arrange
            var kollection = new Kollection
            {
                Name = "Original Name",
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };
            _context.Kollections.Add(kollection);
            await _context.SaveChangesAsync();

            var updateDto = new UpdateKollectionDto
            {
                Name = "Updated Name"
            };

            // Act
            var result = await _controller.UpdateKollection(kollection.Id, updateDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<KollectionSummaryDto>(okResult.Value);
            Assert.Equal("Updated Name", returnValue.Name);

            // Verify it was updated in database
            var updatedKollection = await _context.Kollections.FindAsync(kollection.Id);
            Assert.Equal("Updated Name", updatedKollection!.Name);
        }

        [Fact]
        public async Task UpdateKollection_NonExistentId_ReturnsNotFound()
        {
            // Arrange
            var updateDto = new UpdateKollectionDto
            {
                Name = "New Name"
            };

            // Act
            var result = await _controller.UpdateKollection(999, updateDto);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        #endregion

        #region DeleteKollection Tests

        [Fact]
        public async Task DeleteKollection_ExistingId_ReturnsNoContent()
        {
            // Arrange
            var kollection = new Kollection
            {
                Name = "To Delete",
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };
            _context.Kollections.Add(kollection);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.DeleteKollection(kollection.Id);

            // Assert
            Assert.IsType<NoContentResult>(result);

            // Verify it was deleted from database
            var deletedKollection = await _context.Kollections.FindAsync(kollection.Id);
            Assert.Null(deletedKollection);
        }

        [Fact]
        public async Task DeleteKollection_NonExistentId_ReturnsNotFound()
        {
            // Act
            var result = await _controller.DeleteKollection(999);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion

        #region AddReleaseToKollection Tests

        [Fact]
        public async Task AddReleaseToKollection_ToExistingKollection_ReturnsSuccess()
        {
            // Arrange
            var kollection = new Kollection
            {
                Name = "Test Collection",
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };
            _context.Kollections.Add(kollection);
            await _context.SaveChangesAsync();

            var addDto = new AddToKollectionDto
            {
                MusicReleaseId = 1,
                KollectionId = kollection.Id
            };

            // Act
            var result = await _controller.AddReleaseToKollection(addDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<KollectionSummaryDto>(okResult.Value);
            Assert.Equal(1, returnValue.ItemCount);

            // Verify item was added
            var item = await _context.KollectionItems
                .FirstOrDefaultAsync(ki => ki.KollectionId == kollection.Id && ki.MusicReleaseId == 1);
            Assert.NotNull(item);
        }

        [Fact]
        public async Task AddReleaseToKollection_CreateNewKollection_ReturnsSuccess()
        {
            // Arrange
            var addDto = new AddToKollectionDto
            {
                MusicReleaseId = 1,
                NewKollectionName = "New Collection"
            };

            // Act
            var result = await _controller.AddReleaseToKollection(addDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<KollectionSummaryDto>(okResult.Value);
            Assert.Equal("New Collection", returnValue.Name);
            Assert.Equal(1, returnValue.ItemCount);

            // Verify kollection and item were created
            var kollection = await _context.Kollections.FirstOrDefaultAsync(k => k.Name == "New Collection");
            Assert.NotNull(kollection);
            
            var item = await _context.KollectionItems
                .FirstOrDefaultAsync(ki => ki.KollectionId == kollection!.Id && ki.MusicReleaseId == 1);
            Assert.NotNull(item);
        }

        [Fact]
        public async Task AddReleaseToKollection_NonExistentRelease_ReturnsNotFound()
        {
            // Arrange
            var addDto = new AddToKollectionDto
            {
                MusicReleaseId = 999,
                NewKollectionName = "New Collection"
            };

            // Act
            var result = await _controller.AddReleaseToKollection(addDto);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task AddReleaseToKollection_DuplicateItem_ReturnsBadRequest()
        {
            // Arrange
            var kollection = new Kollection
            {
                Name = "Test Collection",
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };
            _context.Kollections.Add(kollection);
            await _context.SaveChangesAsync();

            // Add item first time
            var addDto = new AddToKollectionDto
            {
                MusicReleaseId = 1,
                KollectionId = kollection.Id
            };
            await _controller.AddReleaseToKollection(addDto);

            // Act - Try to add same item again
            var result = await _controller.AddReleaseToKollection(addDto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        #endregion

        #region RemoveReleaseFromKollection Tests

        [Fact]
        public async Task RemoveReleaseFromKollection_ExistingItem_ReturnsNoContent()
        {
            // Arrange
            var kollection = new Kollection
            {
                Name = "Test Collection",
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };
            _context.Kollections.Add(kollection);
            await _context.SaveChangesAsync();

            var item = new KollectionItem
            {
                KollectionId = kollection.Id,
                MusicReleaseId = 1,
                AddedAt = DateTime.UtcNow
            };
            _context.KollectionItems.Add(item);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.RemoveReleaseFromKollection(kollection.Id, 1);

            // Assert
            Assert.IsType<NoContentResult>(result);

            // Verify item was removed
            var deletedItem = await _context.KollectionItems
                .FirstOrDefaultAsync(ki => ki.KollectionId == kollection.Id && ki.MusicReleaseId == 1);
            Assert.Null(deletedItem);
        }

        [Fact]
        public async Task RemoveReleaseFromKollection_NonExistentItem_ReturnsNotFound()
        {
            // Arrange
            var kollection = new Kollection
            {
                Name = "Test Collection",
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };
            _context.Kollections.Add(kollection);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.RemoveReleaseFromKollection(kollection.Id, 999);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion
    }
}
