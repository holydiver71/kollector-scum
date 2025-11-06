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
using System.Threading.Tasks;

namespace KollectorScum.Tests.Services
{
    /// <summary>
    /// Unit tests for MusicReleaseBatchProcessor service
    /// Tests batch processing, transaction management, and validation
    /// </summary>
    public class MusicReleaseBatchProcessorTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IRepository<MusicRelease>> _mockMusicReleaseRepository;
        private readonly Mock<IRepository<Country>> _mockCountryRepository;
        private readonly Mock<IRepository<Format>> _mockFormatRepository;
        private readonly Mock<IRepository<Label>> _mockLabelRepository;
        private readonly Mock<IRepository<Packaging>> _mockPackagingRepository;
        private readonly Mock<IRepository<Artist>> _mockArtistRepository;
        private readonly Mock<IRepository<Genre>> _mockGenreRepository;
        private readonly Mock<IRepository<Store>> _mockStoreRepository;
        private readonly Mock<ILogger<MusicReleaseBatchProcessor>> _mockLogger;
        private readonly MusicReleaseBatchProcessor _service;

        public MusicReleaseBatchProcessorTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMusicReleaseRepository = new Mock<IRepository<MusicRelease>>();
            _mockCountryRepository = new Mock<IRepository<Country>>();
            _mockFormatRepository = new Mock<IRepository<Format>>();
            _mockLabelRepository = new Mock<IRepository<Label>>();
            _mockPackagingRepository = new Mock<IRepository<Packaging>>();
            _mockArtistRepository = new Mock<IRepository<Artist>>();
            _mockGenreRepository = new Mock<IRepository<Genre>>();
            _mockStoreRepository = new Mock<IRepository<Store>>();
            _mockLogger = new Mock<ILogger<MusicReleaseBatchProcessor>>();

            // Setup UnitOfWork to return mock repositories
            _mockUnitOfWork.Setup(u => u.MusicReleases).Returns(_mockMusicReleaseRepository.Object);
            _mockUnitOfWork.Setup(u => u.Countries).Returns(_mockCountryRepository.Object);
            _mockUnitOfWork.Setup(u => u.Formats).Returns(_mockFormatRepository.Object);
            _mockUnitOfWork.Setup(u => u.Labels).Returns(_mockLabelRepository.Object);
            _mockUnitOfWork.Setup(u => u.Packagings).Returns(_mockPackagingRepository.Object);
            _mockUnitOfWork.Setup(u => u.Artists).Returns(_mockArtistRepository.Object);
            _mockUnitOfWork.Setup(u => u.Genres).Returns(_mockGenreRepository.Object);
            _mockUnitOfWork.Setup(u => u.Stores).Returns(_mockStoreRepository.Object);

            _service = new MusicReleaseBatchProcessor(_mockUnitOfWork.Object, _mockLogger.Object);
        }

        #region ProcessBatchAsync Tests

        [Fact]
        public async Task ProcessBatchAsync_WithNullList_ReturnsZero()
        {
            // Act
            var result = await _service.ProcessBatchAsync(null!);

            // Assert
            Assert.Equal(0, result);
            _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task ProcessBatchAsync_WithEmptyList_ReturnsZero()
        {
            // Act
            var result = await _service.ProcessBatchAsync(new List<MusicReleaseImportDto>());

            // Assert
            Assert.Equal(0, result);
            _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task ProcessBatchAsync_WithValidBatch_ImportsSuccessfully()
        {
            // Arrange
            var releases = new List<MusicReleaseImportDto>
            {
                CreateTestReleaseDto(1, "Album 1"),
                CreateTestReleaseDto(2, "Album 2"),
                CreateTestReleaseDto(3, "Album 3")
            };

            // Setup: No existing releases
            _mockMusicReleaseRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((MusicRelease?)null);

            // Setup: Lookup data exists
            SetupValidLookupData();

            // Act
            var result = await _service.ProcessBatchAsync(releases);

            // Assert
            Assert.Equal(3, result);
            _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Once);
            _mockUnitOfWork.Verify(u => u.RollbackTransactionAsync(), Times.Never);
            _mockMusicReleaseRepository.Verify(r => r.AddAsync(It.IsAny<MusicRelease>()), Times.Exactly(3));
        }

        [Fact]
        public async Task ProcessBatchAsync_WithExistingReleases_SkipsDuplicates()
        {
            // Arrange
            var releases = new List<MusicReleaseImportDto>
            {
                CreateTestReleaseDto(1, "Album 1"),
                CreateTestReleaseDto(2, "Album 2"),
                CreateTestReleaseDto(3, "Album 3")
            };

            // Setup: Release with ID 2 already exists
            _mockMusicReleaseRepository.Setup(r => r.GetByIdAsync(2))
                .ReturnsAsync(new MusicRelease { Id = 2, Title = "Album 2" });
            _mockMusicReleaseRepository.Setup(r => r.GetByIdAsync(It.Is<int>(id => id != 2)))
                .ReturnsAsync((MusicRelease?)null);

            SetupValidLookupData();

            // Act
            var result = await _service.ProcessBatchAsync(releases);

            // Assert
            Assert.Equal(2, result); // Only 2 imported (1 was duplicate)
            _mockMusicReleaseRepository.Verify(r => r.AddAsync(It.IsAny<MusicRelease>()), Times.Exactly(2));
        }

        [Fact]
        public async Task ProcessBatchAsync_WithDifferentBatchSizes_HandlesCorrectly()
        {
            // Arrange - Small batch
            var smallBatch = new List<MusicReleaseImportDto>
            {
                CreateTestReleaseDto(1, "Album 1")
            };

            // Arrange - Medium batch
            var mediumBatch = Enumerable.Range(1, 10)
                .Select(i => CreateTestReleaseDto(i, $"Album {i}"))
                .ToList();

            // Arrange - Large batch
            var largeBatch = Enumerable.Range(1, 100)
                .Select(i => CreateTestReleaseDto(i, $"Album {i}"))
                .ToList();

            _mockMusicReleaseRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((MusicRelease?)null);
            SetupValidLookupData();

            // Act
            var smallResult = await _service.ProcessBatchAsync(smallBatch);
            var mediumResult = await _service.ProcessBatchAsync(mediumBatch);
            var largeResult = await _service.ProcessBatchAsync(largeBatch);

            // Assert
            Assert.Equal(1, smallResult);
            Assert.Equal(10, mediumResult);
            Assert.Equal(100, largeResult);
        }

        [Fact]
        public async Task ProcessBatchAsync_WhenTransactionFails_RollsBack()
        {
            // Arrange
            var releases = new List<MusicReleaseImportDto>
            {
                CreateTestReleaseDto(1, "Album 1")
            };

            _mockMusicReleaseRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((MusicRelease?)null);
            SetupValidLookupData();

            // Setup: Commit throws exception
            _mockUnitOfWork.Setup(u => u.CommitTransactionAsync())
                .ThrowsAsync(new Exception("Transaction failed"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _service.ProcessBatchAsync(releases));
            _mockUnitOfWork.Verify(u => u.RollbackTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task ProcessBatchAsync_WithPartialFailure_ContinuesWithRemainingReleases()
        {
            // Arrange
            var releases = new List<MusicReleaseImportDto>
            {
                CreateTestReleaseDto(1, "Album 1"),
                CreateTestReleaseDto(2, "Album 2"), // This one will fail
                CreateTestReleaseDto(3, "Album 3")
            };

            // Setup: First and third succeed, second fails
            _mockMusicReleaseRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync((MusicRelease?)null);
            _mockMusicReleaseRepository.Setup(r => r.GetByIdAsync(2))
                .ThrowsAsync(new Exception("Database error"));
            _mockMusicReleaseRepository.Setup(r => r.GetByIdAsync(3))
                .ReturnsAsync((MusicRelease?)null);

            SetupValidLookupData();

            // Act
            var result = await _service.ProcessBatchAsync(releases);

            // Assert
            Assert.Equal(2, result); // 2 succeeded despite 1 failure
            _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        #endregion

        #region UpdateUpcBatchAsync Tests

        [Fact]
        public async Task UpdateUpcBatchAsync_WithNullList_ReturnsZero()
        {
            // Act
            var result = await _service.UpdateUpcBatchAsync(null!);

            // Assert
            Assert.Equal(0, result);
            _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateUpcBatchAsync_WithEmptyList_ReturnsZero()
        {
            // Act
            var result = await _service.UpdateUpcBatchAsync(new List<MusicReleaseImportDto>());

            // Assert
            Assert.Equal(0, result);
            _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateUpcBatchAsync_WithValidUpcValues_UpdatesSuccessfully()
        {
            // Arrange
            var releases = new List<MusicReleaseImportDto>
            {
                CreateTestReleaseDto(1, "Album 1", upc: "123456789012"),
                CreateTestReleaseDto(2, "Album 2", upc: "234567890123"),
                CreateTestReleaseDto(3, "Album 3", upc: "345678901234")
            };

            // Setup: All releases exist
            foreach (var dto in releases)
            {
                _mockMusicReleaseRepository.Setup(r => r.GetByIdAsync(dto.Id))
                    .ReturnsAsync(new MusicRelease { Id = dto.Id, Title = dto.Title });
            }

            // Act
            var result = await _service.UpdateUpcBatchAsync(releases);

            // Assert
            Assert.Equal(3, result);
            _mockMusicReleaseRepository.Verify(r => r.Update(It.IsAny<MusicRelease>()), Times.Exactly(3));
            _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateUpcBatchAsync_WithNonExistentReleases_SkipsThem()
        {
            // Arrange
            var releases = new List<MusicReleaseImportDto>
            {
                CreateTestReleaseDto(1, "Album 1", upc: "123456789012"),
                CreateTestReleaseDto(2, "Album 2", upc: "234567890123"), // Does not exist
                CreateTestReleaseDto(3, "Album 3", upc: "345678901234")
            };

            // Setup: Only releases 1 and 3 exist
            _mockMusicReleaseRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new MusicRelease { Id = 1, Title = "Album 1" });
            _mockMusicReleaseRepository.Setup(r => r.GetByIdAsync(2))
                .ReturnsAsync((MusicRelease?)null);
            _mockMusicReleaseRepository.Setup(r => r.GetByIdAsync(3))
                .ReturnsAsync(new MusicRelease { Id = 3, Title = "Album 3" });

            // Act
            var result = await _service.UpdateUpcBatchAsync(releases);

            // Assert
            Assert.Equal(2, result); // Only 2 updated
            _mockMusicReleaseRepository.Verify(r => r.Update(It.IsAny<MusicRelease>()), Times.Exactly(2));
        }

        [Fact]
        public async Task UpdateUpcBatchAsync_WithEmptyUpcValues_SkipsThem()
        {
            // Arrange
            var releases = new List<MusicReleaseImportDto>
            {
                CreateTestReleaseDto(1, "Album 1", upc: "123456789012"),
                CreateTestReleaseDto(2, "Album 2", upc: ""), // Empty UPC
                CreateTestReleaseDto(3, "Album 3", upc: null) // Null UPC
            };

            // Setup: All releases exist
            foreach (var dto in releases)
            {
                _mockMusicReleaseRepository.Setup(r => r.GetByIdAsync(dto.Id))
                    .ReturnsAsync(new MusicRelease { Id = dto.Id, Title = dto.Title });
            }

            // Act
            var result = await _service.UpdateUpcBatchAsync(releases);

            // Assert
            Assert.Equal(1, result); // Only 1 updated (2 and 3 skipped due to empty/null UPC)
            _mockMusicReleaseRepository.Verify(r => r.Update(It.IsAny<MusicRelease>()), Times.Once);
        }

        [Fact]
        public async Task UpdateUpcBatchAsync_WhenTransactionFails_RollsBack()
        {
            // Arrange
            var releases = new List<MusicReleaseImportDto>
            {
                CreateTestReleaseDto(1, "Album 1", upc: "123456789012")
            };

            _mockMusicReleaseRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new MusicRelease { Id = 1, Title = "Album 1" });

            // Setup: Commit throws exception
            _mockUnitOfWork.Setup(u => u.CommitTransactionAsync())
                .ThrowsAsync(new Exception("Transaction failed"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _service.UpdateUpcBatchAsync(releases));
            _mockUnitOfWork.Verify(u => u.RollbackTransactionAsync(), Times.Once);
        }

        #endregion

        #region ValidateLookupDataAsync Tests

        [Fact]
        public async Task ValidateLookupDataAsync_WithAllDataPresent_ReturnsValid()
        {
            // Arrange
            _mockCountryRepository.Setup(r => r.CountAsync(null)).ReturnsAsync(10);
            _mockFormatRepository.Setup(r => r.CountAsync(null)).ReturnsAsync(5);
            _mockLabelRepository.Setup(r => r.CountAsync(null)).ReturnsAsync(20);
            _mockPackagingRepository.Setup(r => r.CountAsync(null)).ReturnsAsync(3);

            // Act
            var (isValid, errors) = await _service.ValidateLookupDataAsync();

            // Assert
            Assert.True(isValid);
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateLookupDataAsync_WithMissingCountries_ReturnsInvalid()
        {
            // Arrange
            _mockCountryRepository.Setup(r => r.CountAsync(null)).ReturnsAsync(0);
            _mockFormatRepository.Setup(r => r.CountAsync(null)).ReturnsAsync(5);
            _mockLabelRepository.Setup(r => r.CountAsync(null)).ReturnsAsync(20);
            _mockPackagingRepository.Setup(r => r.CountAsync(null)).ReturnsAsync(3);

            // Act
            var (isValid, errors) = await _service.ValidateLookupDataAsync();

            // Assert
            Assert.False(isValid);
            Assert.Contains(errors, e => e.Contains("countries"));
        }

        [Fact]
        public async Task ValidateLookupDataAsync_WithMultipleMissingTables_ReturnsAllErrors()
        {
            // Arrange
            _mockCountryRepository.Setup(r => r.CountAsync(null)).ReturnsAsync(0);
            _mockFormatRepository.Setup(r => r.CountAsync(null)).ReturnsAsync(0);
            _mockLabelRepository.Setup(r => r.CountAsync(null)).ReturnsAsync(20);
            _mockPackagingRepository.Setup(r => r.CountAsync(null)).ReturnsAsync(3);

            // Act
            var (isValid, errors) = await _service.ValidateLookupDataAsync();

            // Assert
            Assert.False(isValid);
            Assert.Equal(2, errors.Count);
            Assert.Contains(errors, e => e.Contains("countries"));
            Assert.Contains(errors, e => e.Contains("formats"));
        }

        [Fact]
        public async Task ValidateLookupDataAsync_WithAllTablesEmpty_ReturnsAllErrors()
        {
            // Arrange
            _mockCountryRepository.Setup(r => r.CountAsync(null)).ReturnsAsync(0);
            _mockFormatRepository.Setup(r => r.CountAsync(null)).ReturnsAsync(0);
            _mockLabelRepository.Setup(r => r.CountAsync(null)).ReturnsAsync(0);
            _mockPackagingRepository.Setup(r => r.CountAsync(null)).ReturnsAsync(0);

            // Act
            var (isValid, errors) = await _service.ValidateLookupDataAsync();

            // Assert
            Assert.False(isValid);
            Assert.Equal(4, errors.Count);
        }

        #endregion

        #region Helper Methods

        private MusicReleaseImportDto CreateTestReleaseDto(int id, string title, string? upc = null)
        {
            return new MusicReleaseImportDto
            {
                Id = id,
                Title = title,
                LabelId = 1,
                CountryId = 1,
                FormatId = 1,
                PackagingId = 1,
                ReleaseYear = "2024",
                Artists = new List<int> { 1 },
                Genres = new List<int> { 1 },
                Upc = upc,
                DateAdded = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };
        }

        private void SetupValidLookupData()
        {
            _mockCountryRepository.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Country, bool>>>()))
                .ReturnsAsync(true);
            _mockFormatRepository.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Format, bool>>>()))
                .ReturnsAsync(true);
            _mockLabelRepository.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Label, bool>>>()))
                .ReturnsAsync(true);
            _mockPackagingRepository.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Packaging, bool>>>()))
                .ReturnsAsync(true);
        }

        #endregion
    }
}
