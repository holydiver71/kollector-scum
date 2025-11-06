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
    /// Unit tests for MusicReleaseImportOrchestrator service
    /// Tests orchestration, delegation, and coordination logic
    /// </summary>
    public class MusicReleaseImportOrchestratorTests
    {
        private readonly Mock<IJsonFileReader> _mockFileReader;
        private readonly Mock<IMusicReleaseBatchProcessor> _mockBatchProcessor;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IRepository<MusicRelease>> _mockMusicReleaseRepository;
        private readonly Mock<ILogger<MusicReleaseImportOrchestrator>> _mockLogger;
        private readonly MusicReleaseImportOrchestrator _service;
        private readonly string _testDataPath = "/test/data/path";

        public MusicReleaseImportOrchestratorTests()
        {
            _mockFileReader = new Mock<IJsonFileReader>();
            _mockBatchProcessor = new Mock<IMusicReleaseBatchProcessor>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMusicReleaseRepository = new Mock<IRepository<MusicRelease>>();
            _mockLogger = new Mock<ILogger<MusicReleaseImportOrchestrator>>();

            _mockUnitOfWork.Setup(u => u.MusicReleases).Returns(_mockMusicReleaseRepository.Object);

            _service = new MusicReleaseImportOrchestrator(
                _mockFileReader.Object,
                _mockBatchProcessor.Object,
                _mockUnitOfWork.Object,
                _mockLogger.Object,
                _testDataPath);
        }

        #region ImportMusicReleasesAsync Tests

        [Fact]
        public async Task ImportMusicReleasesAsync_WithNonExistentFile_ReturnsZero()
        {
            // Arrange
            _mockFileReader.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);

            // Act
            var result = await _service.ImportMusicReleasesAsync();

            // Assert
            Assert.Equal(0, result);
            _mockFileReader.Verify(f => f.ReadJsonFileAsync<List<MusicReleaseImportDto>>(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ImportMusicReleasesAsync_WithEmptyFile_ReturnsZero()
        {
            // Arrange
            _mockFileReader.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);
            _mockFileReader.Setup(f => f.ReadJsonFileAsync<List<MusicReleaseImportDto>>(It.IsAny<string>()))
                .ReturnsAsync((List<MusicReleaseImportDto>?)null);

            // Act
            var result = await _service.ImportMusicReleasesAsync();

            // Assert
            Assert.Equal(0, result);
            _mockBatchProcessor.Verify(b => b.ProcessBatchAsync(It.IsAny<List<MusicReleaseImportDto>>()), Times.Never);
        }

        [Fact]
        public async Task ImportMusicReleasesAsync_WithValidData_DelegatesToBatchProcessor()
        {
            // Arrange
            var releases = CreateTestReleases(50);
            _mockFileReader.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);
            _mockFileReader.Setup(f => f.ReadJsonFileAsync<List<MusicReleaseImportDto>>(It.IsAny<string>()))
                .ReturnsAsync(releases);
            _mockBatchProcessor.Setup(b => b.ProcessBatchAsync(It.IsAny<List<MusicReleaseImportDto>>()))
                .ReturnsAsync((List<MusicReleaseImportDto> batch) => batch.Count);

            // Act
            var result = await _service.ImportMusicReleasesAsync();

            // Assert
            Assert.Equal(50, result);
            _mockBatchProcessor.Verify(b => b.ProcessBatchAsync(It.IsAny<List<MusicReleaseImportDto>>()), Times.Once);
        }

        [Fact]
        public async Task ImportMusicReleasesAsync_WithLargeDataset_ProcessesInBatches()
        {
            // Arrange - 250 releases should result in 3 batches (100, 100, 50)
            var releases = CreateTestReleases(250);
            _mockFileReader.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);
            _mockFileReader.Setup(f => f.ReadJsonFileAsync<List<MusicReleaseImportDto>>(It.IsAny<string>()))
                .ReturnsAsync(releases);
            _mockBatchProcessor.Setup(b => b.ProcessBatchAsync(It.IsAny<List<MusicReleaseImportDto>>()))
                .ReturnsAsync((List<MusicReleaseImportDto> batch) => batch.Count);

            // Act
            var result = await _service.ImportMusicReleasesAsync();

            // Assert
            Assert.Equal(250, result);
            _mockBatchProcessor.Verify(b => b.ProcessBatchAsync(It.IsAny<List<MusicReleaseImportDto>>()), Times.Exactly(3));
        }

        [Fact]
        public async Task ImportMusicReleasesAsync_WhenBatchProcessorFails_ThrowsException()
        {
            // Arrange
            var releases = CreateTestReleases(10);
            _mockFileReader.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);
            _mockFileReader.Setup(f => f.ReadJsonFileAsync<List<MusicReleaseImportDto>>(It.IsAny<string>()))
                .ReturnsAsync(releases);
            _mockBatchProcessor.Setup(b => b.ProcessBatchAsync(It.IsAny<List<MusicReleaseImportDto>>()))
                .ThrowsAsync(new Exception("Batch processing failed"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _service.ImportMusicReleasesAsync());
        }

        #endregion

        #region ImportMusicReleasesBatchAsync Tests

        [Fact]
        public async Task ImportMusicReleasesBatchAsync_WithNonExistentFile_ReturnsZero()
        {
            // Arrange
            _mockFileReader.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);

            // Act
            var result = await _service.ImportMusicReleasesBatchAsync(10, 0);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public async Task ImportMusicReleasesBatchAsync_WithValidBatch_ImportsCorrectRange()
        {
            // Arrange
            var allReleases = CreateTestReleases(100);
            _mockFileReader.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);
            _mockFileReader.Setup(f => f.ReadJsonFileAsync<List<MusicReleaseImportDto>>(It.IsAny<string>()))
                .ReturnsAsync(allReleases);
            _mockBatchProcessor.Setup(b => b.ProcessBatchAsync(It.IsAny<List<MusicReleaseImportDto>>()))
                .ReturnsAsync((List<MusicReleaseImportDto> batch) => batch.Count);

            // Act - Import releases 10-19 (skip 10, take 10)
            var result = await _service.ImportMusicReleasesBatchAsync(10, 10);

            // Assert
            Assert.Equal(10, result);
            _mockBatchProcessor.Verify(b => b.ProcessBatchAsync(
                It.Is<List<MusicReleaseImportDto>>(batch => batch.Count == 10 && batch[0].Id == 11)), 
                Times.Once);
        }

        #endregion

        #region GetMusicReleaseCountAsync Tests

        [Fact]
        public async Task GetMusicReleaseCountAsync_WithNonExistentFile_ReturnsZero()
        {
            // Arrange
            _mockFileReader.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);

            // Act
            var result = await _service.GetMusicReleaseCountAsync();

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public async Task GetMusicReleaseCountAsync_WithValidFile_DelegatesToFileReader()
        {
            // Arrange
            _mockFileReader.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);
            _mockFileReader.Setup(f => f.GetJsonArrayCountAsync<MusicReleaseImportDto>(It.IsAny<string>()))
                .ReturnsAsync(250);

            // Act
            var result = await _service.GetMusicReleaseCountAsync();

            // Assert
            Assert.Equal(250, result);
            _mockFileReader.Verify(f => f.GetJsonArrayCountAsync<MusicReleaseImportDto>(It.IsAny<string>()), Times.Once);
        }

        #endregion

        #region GetImportProgressAsync Tests

        [Fact]
        public async Task GetImportProgressAsync_ReturnsProgressInfo()
        {
            // Arrange
            _mockFileReader.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);
            _mockFileReader.Setup(f => f.GetJsonArrayCountAsync<MusicReleaseImportDto>(It.IsAny<string>()))
                .ReturnsAsync(100);
            _mockMusicReleaseRepository.Setup(r => r.CountAsync(null))
                .ReturnsAsync(75);

            // Act
            var result = await _service.GetImportProgressAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(100, result.TotalRecords);
            Assert.Equal(75, result.ImportedRecords);
            Assert.Equal(75.0, result.ProgressPercentage);
        }

        [Fact]
        public async Task GetImportProgressAsync_WhenFullyImported_ShowsComplete()
        {
            // Arrange
            _mockFileReader.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);
            _mockFileReader.Setup(f => f.GetJsonArrayCountAsync<MusicReleaseImportDto>(It.IsAny<string>()))
                .ReturnsAsync(50);
            _mockMusicReleaseRepository.Setup(r => r.CountAsync(null))
                .ReturnsAsync(50);

            // Act
            var result = await _service.GetImportProgressAsync();

            // Assert
            Assert.Equal(50, result.ImportedRecords);
            Assert.Equal(100.0, result.ProgressPercentage);
        }

        #endregion

        #region UpdateUpcValuesAsync Tests

        [Fact]
        public async Task UpdateUpcValuesAsync_WithNonExistentFile_ReturnsZero()
        {
            // Arrange
            _mockFileReader.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);

            // Act
            var result = await _service.UpdateUpcValuesAsync();

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public async Task UpdateUpcValuesAsync_WithValidData_DelegatesToBatchProcessor()
        {
            // Arrange
            var releases = CreateTestReleases(50);
            _mockFileReader.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);
            _mockFileReader.Setup(f => f.ReadJsonFileAsync<List<MusicReleaseImportDto>>(It.IsAny<string>()))
                .ReturnsAsync(releases);
            _mockBatchProcessor.Setup(b => b.UpdateUpcBatchAsync(It.IsAny<List<MusicReleaseImportDto>>()))
                .ReturnsAsync(45); // 45 updated

            // Act
            var result = await _service.UpdateUpcValuesAsync();

            // Assert
            Assert.Equal(45, result);
            _mockBatchProcessor.Verify(b => b.UpdateUpcBatchAsync(It.IsAny<List<MusicReleaseImportDto>>()), Times.Once);
        }

        #endregion

        #region ValidateLookupDataAsync Tests

        [Fact]
        public async Task ValidateLookupDataAsync_DelegatesToBatchProcessor()
        {
            // Arrange
            var expectedErrors = new List<string> { "Error 1", "Error 2" };
            _mockBatchProcessor.Setup(b => b.ValidateLookupDataAsync())
                .ReturnsAsync((false, expectedErrors));

            // Act
            var (isValid, errors) = await _service.ValidateLookupDataAsync();

            // Assert
            Assert.False(isValid);
            Assert.Equal(expectedErrors, errors);
            _mockBatchProcessor.Verify(b => b.ValidateLookupDataAsync(), Times.Once);
        }

        [Fact]
        public async Task ValidateLookupDataAsync_WhenValid_ReturnsTrue()
        {
            // Arrange
            _mockBatchProcessor.Setup(b => b.ValidateLookupDataAsync())
                .ReturnsAsync((true, new List<string>()));

            // Act
            var (isValid, errors) = await _service.ValidateLookupDataAsync();

            // Assert
            Assert.True(isValid);
            Assert.Empty(errors);
        }

        #endregion

        #region Helper Methods

        private List<MusicReleaseImportDto> CreateTestReleases(int count)
        {
            return Enumerable.Range(1, count)
                .Select(i => new MusicReleaseImportDto
                {
                    Id = i,
                    Title = $"Album {i}",
                    LabelId = 1,
                    CountryId = 1,
                    FormatId = 1,
                    PackagingId = 1,
                    ReleaseYear = "2024",
                    Artists = new List<int> { 1 },
                    Genres = new List<int> { 1 },
                    Upc = $"12345678901{i:D1}",
                    DateAdded = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                })
                .ToList();
        }

        #endregion
    }
}
