using System.Linq.Expressions;
using System.Text.Json;
using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using KollectorScum.Api.Models.ValueObjects;
using KollectorScum.Api.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KollectorScum.Tests.Services
{
    /// <summary>
    /// Unit tests for MusicReleaseService
    /// </summary>
    public class MusicReleaseServiceTests
    {
        private readonly Mock<IRepository<MusicRelease>> _mockMusicReleaseRepo;
        private readonly Mock<IRepository<Artist>> _mockArtistRepo;
        private readonly Mock<IRepository<Genre>> _mockGenreRepo;
        private readonly Mock<IRepository<Label>> _mockLabelRepo;
        private readonly Mock<IRepository<Country>> _mockCountryRepo;
        private readonly Mock<IRepository<Format>> _mockFormatRepo;
        private readonly Mock<IRepository<Packaging>> _mockPackagingRepo;
        private readonly Mock<IRepository<Store>> _mockStoreRepo;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IEntityResolverService> _mockEntityResolver;
        private readonly Mock<IMusicReleaseMapperService> _mockMapper;
        private readonly Mock<ICollectionStatisticsService> _mockStatisticsService;
        private readonly Mock<ILogger<MusicReleaseService>> _mockLogger;
        private readonly MusicReleaseService _service;

        public MusicReleaseServiceTests()
        {
            _mockMusicReleaseRepo = new Mock<IRepository<MusicRelease>>();
            _mockArtistRepo = new Mock<IRepository<Artist>>();
            _mockGenreRepo = new Mock<IRepository<Genre>>();
            _mockLabelRepo = new Mock<IRepository<Label>>();
            _mockCountryRepo = new Mock<IRepository<Country>>();
            _mockFormatRepo = new Mock<IRepository<Format>>();
            _mockPackagingRepo = new Mock<IRepository<Packaging>>();
            _mockStoreRepo = new Mock<IRepository<Store>>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockEntityResolver = new Mock<IEntityResolverService>();
            _mockMapper = new Mock<IMusicReleaseMapperService>();
            _mockStatisticsService = new Mock<ICollectionStatisticsService>();
            _mockLogger = new Mock<ILogger<MusicReleaseService>>();

            _service = new MusicReleaseService(
                _mockMusicReleaseRepo.Object,
                _mockArtistRepo.Object,
                _mockLabelRepo.Object,
                _mockUnitOfWork.Object,
                _mockEntityResolver.Object,
                _mockMapper.Object,
                _mockStatisticsService.Object,
                _mockLogger.Object
            );
        }

        #region GetMusicReleasesAsync Tests

        [Fact]
        public async Task GetMusicReleasesAsync_WithNoFilters_ReturnsPagedResults()
        {
            // Arrange
            var musicReleases = new List<MusicRelease>
            {
                new MusicRelease
                {
                    Id = 1,
                    Title = "Test Album 1",
                    ReleaseYear = new DateTime(2020, 1, 1),
                    Artists = "[1]",
                    Genres = "[1]",
                    Label = new Label { Id = 1, Name = "Test Label" },
                    Format = new Format { Id = 1, Name = "CD" },
                    Country = new Country { Id = 1, Name = "UK" },
                    DateAdded = DateTime.UtcNow
                },
                new MusicRelease
                {
                    Id = 2,
                    Title = "Test Album 2",
                    ReleaseYear = new DateTime(2021, 1, 1),
                    Artists = "[2]",
                    Genres = "[2]",
                    Label = new Label { Id = 2, Name = "Test Label 2" },
                    Format = new Format { Id = 2, Name = "Vinyl" },
                    Country = new Country { Id = 2, Name = "US" },
                    DateAdded = DateTime.UtcNow
                }
            };

            var pagedResult = new PagedResult<MusicRelease>
            {
                Items = musicReleases,
                Page = 1,
                PageSize = 10,
                TotalCount = 2,
                TotalPages = 1
            };

            _mockMusicReleaseRepo.Setup(r => r.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<MusicRelease, bool>>?>(),
                It.IsAny<Func<IQueryable<MusicRelease>, IOrderedQueryable<MusicRelease>>?>(),
                It.IsAny<string>()
            )).ReturnsAsync(pagedResult);

            _mockMapper.Setup(m => m.MapToSummaryDto(It.IsAny<MusicRelease>()))
                .Returns((MusicRelease mr) => new MusicReleaseSummaryDto
                {
                    Id = mr.Id,
                    Title = mr.Title,
                    ReleaseYear = mr.ReleaseYear,
                    DateAdded = mr.DateAdded
                });

            // Act
            var result = await _service.GetMusicReleasesAsync(null, null, null, null, null, null, null, null, null, 1, 10);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Items.Count());
            Assert.Equal(1, result.Page);
            Assert.Equal(10, result.PageSize);
            Assert.Equal(2, result.TotalCount);
        }

        [Fact]
        public async Task GetMusicReleasesAsync_WithSearchFilter_ReturnsFilteredResults()
        {
            // Arrange
            var musicReleases = new List<MusicRelease>
            {
                new MusicRelease
                {
                    Id = 1,
                    Title = "Dark Side of the Moon",
                    ReleaseYear = new DateTime(1973, 3, 1),
                    Artists = "[1]",
                    Label = new Label { Id = 1, Name = "Harvest" },
                    DateAdded = DateTime.UtcNow
                }
            };

            var pagedResult = new PagedResult<MusicRelease>
            {
                Items = musicReleases,
                Page = 1,
                PageSize = 10,
                TotalCount = 1,
                TotalPages = 1
            };

            _mockMusicReleaseRepo.Setup(r => r.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<MusicRelease, bool>>?>(),
                It.IsAny<Func<IQueryable<MusicRelease>, IOrderedQueryable<MusicRelease>>?>(),
                It.IsAny<string>()
            )).ReturnsAsync(pagedResult);

            _mockMapper.Setup(m => m.MapToSummaryDto(It.IsAny<MusicRelease>()))
                .Returns((MusicRelease mr) => new MusicReleaseSummaryDto
                {
                    Id = mr.Id,
                    Title = mr.Title,
                    ReleaseYear = mr.ReleaseYear
                });

            // Act
            var result = await _service.GetMusicReleasesAsync("Dark", null, null, null, null, null, null, null, null, 1, 10);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Items);
            Assert.Equal("Dark Side of the Moon", result.Items.First().Title);
        }

        [Fact]
        public async Task GetMusicReleasesAsync_WithYearRangeFilter_ReturnsFilteredResults()
        {
            // Arrange
            var musicReleases = new List<MusicRelease>
            {
                new MusicRelease
                {
                    Id = 1,
                    Title = "Album from 2020",
                    ReleaseYear = new DateTime(2020, 1, 1),
                    Artists = "[1]",
                    DateAdded = DateTime.UtcNow
                }
            };

            var pagedResult = new PagedResult<MusicRelease>
            {
                Items = musicReleases,
                Page = 1,
                PageSize = 10,
                TotalCount = 1,
                TotalPages = 1
            };

            _mockMusicReleaseRepo.Setup(r => r.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<MusicRelease, bool>>?>(),
                It.IsAny<Func<IQueryable<MusicRelease>, IOrderedQueryable<MusicRelease>>?>(),
                It.IsAny<string>()
            )).ReturnsAsync(pagedResult);

            _mockMapper.Setup(m => m.MapToSummaryDto(It.IsAny<MusicRelease>()))
                .Returns((MusicRelease mr) => new MusicReleaseSummaryDto { Id = mr.Id, Title = mr.Title });

            // Act
            var result = await _service.GetMusicReleasesAsync(null, null, null, null, null, null, null, 2020, 2020, 1, 10);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Items);
        }

        #endregion

        #region GetMusicReleaseAsync Tests

        [Fact]
        public async Task GetMusicReleaseAsync_WithValidId_ReturnsRelease()
        {
            // Arrange
            var artistId = 1;
            var genreId = 1;
            var musicRelease = new MusicRelease
            {
                Id = 1,
                Title = "Test Album",
                ReleaseYear = new DateTime(2020, 1, 1),
                Artists = $"[{artistId}]",
                Genres = $"[{genreId}]",
                Label = new Label { Id = 1, Name = "Test Label" },
                Country = new Country { Id = 1, Name = "UK" },
                Format = new Format { Id = 1, Name = "CD" },
                Packaging = new Packaging { Id = 1, Name = "Jewel Case" },
                DateAdded = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            var expectedDto = new MusicReleaseDto
            {
                Id = 1,
                Title = "Test Album",
                Artists = new List<ArtistDto> { new ArtistDto { Id = artistId, Name = "Test Artist" } }
            };

            _mockMusicReleaseRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<string>()))
                .ReturnsAsync(musicRelease);
            
            _mockMapper.Setup(m => m.MapToFullDtoAsync(It.IsAny<MusicRelease>()))
                .ReturnsAsync(expectedDto);

            // Act
            var result = await _service.GetMusicReleaseAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("Test Album", result.Title);
            Assert.NotNull(result.Artists);
            Assert.Single(result.Artists);
        }

        [Fact]
        public async Task GetMusicReleaseAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            _mockMusicReleaseRepo.Setup(r => r.GetByIdAsync(999, It.IsAny<string>()))
                .ReturnsAsync((MusicRelease?)null);

            // Act
            var result = await _service.GetMusicReleaseAsync(999);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetSearchSuggestionsAsync Tests

        [Fact]
        public async Task GetSearchSuggestionsAsync_WithValidQuery_ReturnsSuggestions()
        {
            // Arrange
            var releases = new List<MusicRelease>
            {
                new MusicRelease { Id = 1, Title = "Dark Side of the Moon", ReleaseYear = new DateTime(1973, 1, 1) }
            };
            var artists = new List<Artist>
            {
                new Artist { Id = 1, Name = "Pink Floyd" }
            };
            var labels = new List<Label>
            {
                new Label { Id = 1, Name = "Harvest Records" }
            };

            _mockMusicReleaseRepo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<MusicRelease, bool>>>(),
                It.IsAny<Func<IQueryable<MusicRelease>, IOrderedQueryable<MusicRelease>>?>(),
                It.IsAny<string>()
            )).ReturnsAsync(releases);

            _mockArtistRepo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<Artist, bool>>>(),
                It.IsAny<Func<IQueryable<Artist>, IOrderedQueryable<Artist>>?>(),
                It.IsAny<string>()
            )).ReturnsAsync(artists);

            _mockLabelRepo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<Label, bool>>>(),
                It.IsAny<Func<IQueryable<Label>, IOrderedQueryable<Label>>?>(),
                It.IsAny<string>()
            )).ReturnsAsync(labels);

            // Act
            var result = await _service.GetSearchSuggestionsAsync("dark", 10);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [Fact]
        public async Task GetSearchSuggestionsAsync_WithShortQuery_ReturnsEmptyList()
        {
            // Act
            var result = await _service.GetSearchSuggestionsAsync("a", 10);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetSearchSuggestionsAsync_WithNullQuery_ReturnsEmptyList()
        {
            // Act
            var result = await _service.GetSearchSuggestionsAsync(string.Empty, 10);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion

        #region GetCollectionStatisticsAsync Tests

        [Fact]
        public async Task GetCollectionStatisticsAsync_WithReleases_ReturnsStatistics()
        {
            // Arrange
            var expectedStats = new CollectionStatisticsDto
            {
                TotalReleases = 2,
                TotalArtists = 2,
                TotalGenres = 2,
                TotalLabels = 2,
                TotalValue = 26.98m
            };

            _mockStatisticsService.Setup(s => s.GetCollectionStatisticsAsync())
                .ReturnsAsync(expectedStats);

            // Act
            var result = await _service.GetCollectionStatisticsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalReleases);
            Assert.True(result.TotalValue > 0);
            _mockStatisticsService.Verify(s => s.GetCollectionStatisticsAsync(), Times.Once);
        }

        [Fact]
        public async Task GetCollectionStatisticsAsync_WithNoReleases_ReturnsEmptyStatistics()
        {
            // Arrange
            var expectedStats = new CollectionStatisticsDto
            {
                TotalReleases = 0,
                TotalValue = null
            };

            _mockStatisticsService.Setup(s => s.GetCollectionStatisticsAsync())
                .ReturnsAsync(expectedStats);

            // Act
            var result = await _service.GetCollectionStatisticsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.TotalReleases);
            Assert.Null(result.TotalValue);
        }

        #endregion

        #region CreateMusicReleaseAsync Tests

        [Fact]
        public async Task CreateMusicReleaseAsync_WithValidData_CreatesRelease()
        {
            // Arrange
            var createDto = new CreateMusicReleaseDto
            {
                Title = "New Album",
                ReleaseYear = new DateTime(2023, 1, 1),
                ArtistIds = new List<int> { 1 },
                GenreIds = new List<int> { 1 },
                LabelId = 1,
                CountryId = 1,
                FormatId = 1
            };

            var createdEntities = new CreatedEntitiesDto();

            _mockEntityResolver.Setup(e => e.ResolveOrCreateArtistsAsync(
                It.IsAny<List<int>?>(), It.IsAny<List<string>?>(), It.IsAny<CreatedEntitiesDto>()))
                .ReturnsAsync(new List<int> { 1 });
            
            _mockEntityResolver.Setup(e => e.ResolveOrCreateGenresAsync(
                It.IsAny<List<int>?>(), It.IsAny<List<string>?>(), It.IsAny<CreatedEntitiesDto>()))
                .ReturnsAsync(new List<int> { 1 });

            _mockEntityResolver.Setup(e => e.ResolveOrCreateLabelAsync(
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CreatedEntitiesDto>()))
                .ReturnsAsync(1);

            _mockEntityResolver.Setup(e => e.ResolveOrCreateCountryAsync(
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CreatedEntitiesDto>()))
                .ReturnsAsync(1);

            _mockEntityResolver.Setup(e => e.ResolveOrCreateFormatAsync(
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CreatedEntitiesDto>()))
                .ReturnsAsync(1);

            _mockEntityResolver.Setup(e => e.ResolveOrCreatePackagingAsync(
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CreatedEntitiesDto>()))
                .ReturnsAsync((int?)null);

            _mockMusicReleaseRepo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<MusicRelease, bool>>>(),
                It.IsAny<Func<IQueryable<MusicRelease>, IOrderedQueryable<MusicRelease>>?>(),
                It.IsAny<string>()
            )).ReturnsAsync(new List<MusicRelease>());

            _mockMusicReleaseRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<MusicRelease>());

            _mockMusicReleaseRepo.Setup(r => r.AddAsync(It.IsAny<MusicRelease>()))
                .Callback<MusicRelease>(mr => mr.Id = 1)
                .ReturnsAsync((MusicRelease mr) => mr);

            _mockMapper.Setup(m => m.MapToFullDtoAsync(It.IsAny<MusicRelease>()))
                .ReturnsAsync(new MusicReleaseDto { Id = 1, Title = "New Album" });

            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            _mockUnitOfWork.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateMusicReleaseAsync(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Release);
            Assert.Equal("New Album", result.Release.Title);
            _mockMusicReleaseRepo.Verify(r => r.AddAsync(It.IsAny<MusicRelease>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateMusicReleaseAsync_WithNewArtistName_CreatesArtistAndRelease()
        {
            // Arrange
            var createDto = new CreateMusicReleaseDto
            {
                Title = "New Album",
                ReleaseYear = new DateTime(2023, 1, 1),
                ArtistNames = new List<string> { "New Artist" },
                GenreIds = new List<int> { 1 },
                LabelId = 1
            };

            var createdEntities = new CreatedEntitiesDto();
            createdEntities.Artists = new List<ArtistDto> { new ArtistDto { Id = 1, Name = "New Artist" } };

            _mockEntityResolver.Setup(e => e.ResolveOrCreateArtistsAsync(
                It.IsAny<List<int>?>(), It.IsAny<List<string>?>(), It.IsAny<CreatedEntitiesDto>()))
                .Callback<List<int>?, List<string>?, CreatedEntitiesDto>((ids, names, created) =>
                {
                    created.Artists = new List<ArtistDto> { new ArtistDto { Id = 1, Name = "New Artist" } };
                })
                .ReturnsAsync(new List<int> { 1 });

            _mockEntityResolver.Setup(e => e.ResolveOrCreateGenresAsync(
                It.IsAny<List<int>?>(), It.IsAny<List<string>?>(), It.IsAny<CreatedEntitiesDto>()))
                .ReturnsAsync(new List<int> { 1 });

            _mockEntityResolver.Setup(e => e.ResolveOrCreateLabelAsync(
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CreatedEntitiesDto>()))
                .ReturnsAsync(1);

            _mockMusicReleaseRepo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<MusicRelease, bool>>>(),
                It.IsAny<Func<IQueryable<MusicRelease>, IOrderedQueryable<MusicRelease>>?>(),
                It.IsAny<string>()
            )).ReturnsAsync(new List<MusicRelease>());

            _mockMusicReleaseRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<MusicRelease>());

            _mockMusicReleaseRepo.Setup(r => r.AddAsync(It.IsAny<MusicRelease>()))
                .Callback<MusicRelease>(mr => mr.Id = 1)
                .ReturnsAsync((MusicRelease mr) => mr);

            _mockMapper.Setup(m => m.MapToFullDtoAsync(It.IsAny<MusicRelease>()))
                .ReturnsAsync(new MusicReleaseDto { Id = 1, Title = "New Album" });

            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            _mockUnitOfWork.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateMusicReleaseAsync(createDto);

            // Assert
            Assert.NotNull(result);
            _mockEntityResolver.Verify(e => e.ResolveOrCreateArtistsAsync(
                It.IsAny<List<int>?>(), It.IsAny<List<string>?>(), It.IsAny<CreatedEntitiesDto>()), Times.Once);
        }

        [Fact]
        public async Task CreateMusicReleaseAsync_WithDuplicateCatalogNumber_ThrowsArgumentException()
        {
            // Arrange
            var createDto = new CreateMusicReleaseDto
            {
                Title = "Duplicate Album",
                ReleaseYear = new DateTime(2023, 1, 1),
                LabelNumber = "CATALOG001",
                ArtistIds = new List<int> { 1 }
            };

            var existingRelease = new MusicRelease
            {
                Id = 1,
                Title = "Existing Album",
                LabelNumber = "CATALOG001",
                Artists = "[1]"
            };

            _mockMusicReleaseRepo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<MusicRelease, bool>>>(),
                It.IsAny<Func<IQueryable<MusicRelease>, IOrderedQueryable<MusicRelease>>?>(),
                It.IsAny<string>()
            )).ReturnsAsync(new List<MusicRelease> { existingRelease });

            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateMusicReleaseAsync(createDto));
            _mockUnitOfWork.Verify(u => u.RollbackTransactionAsync(), Times.AtLeastOnce);
        }

        #endregion

        #region UpdateMusicReleaseAsync Tests

        [Fact]
        public async Task UpdateMusicReleaseAsync_WithValidData_UpdatesRelease()
        {
            // Arrange
            var updateDto = new UpdateMusicReleaseDto
            {
                Title = "Updated Album",
                ReleaseYear = new DateTime(2023, 1, 1),
                ArtistIds = new List<int> { 1 },
                GenreIds = new List<int> { 1 }
            };

            var existingRelease = new MusicRelease
            {
                Id = 1,
                Title = "Old Album",
                ReleaseYear = new DateTime(2022, 1, 1),
                Artists = "[1]",
                Genres = "[1]",
                Label = new Label { Id = 1, Name = "Test Label" },
                DateAdded = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            _mockMusicReleaseRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<string>()))
                .ReturnsAsync((MusicRelease?)null);
            _mockMusicReleaseRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existingRelease);

            _mockEntityResolver.Setup(e => e.ResolveOrCreateArtistsAsync(
                It.IsAny<List<int>?>(), It.IsAny<List<string>?>(), It.IsAny<CreatedEntitiesDto>()))
                .ReturnsAsync(new List<int> { 1 });

            _mockEntityResolver.Setup(e => e.ResolveOrCreateGenresAsync(
                It.IsAny<List<int>?>(), It.IsAny<List<string>?>(), It.IsAny<CreatedEntitiesDto>()))
                .ReturnsAsync(new List<int> { 1 });

            _mockMapper.Setup(m => m.MapToFullDtoAsync(It.IsAny<MusicRelease>()))
                .ReturnsAsync(new MusicReleaseDto { Id = 1, Title = "Updated Album" });

            _mockMusicReleaseRepo.Setup(r => r.Update(It.IsAny<MusicRelease>()));
            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _service.UpdateMusicReleaseAsync(1, updateDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Updated Album", result.Title);
            _mockMusicReleaseRepo.Verify(r => r.Update(It.IsAny<MusicRelease>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateMusicReleaseAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var updateDto = new UpdateMusicReleaseDto
            {
                Title = "Updated Album"
            };

            _mockMusicReleaseRepo.Setup(r => r.GetByIdAsync(999, It.IsAny<string>()))
                .ReturnsAsync((MusicRelease?)null);
            _mockMusicReleaseRepo.Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((MusicRelease?)null);

            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.UpdateMusicReleaseAsync(999, updateDto);

            // Assert
            Assert.Null(result);
            _mockUnitOfWork.Verify(u => u.RollbackTransactionAsync(), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateMusicReleaseAsync_WithNewStoreName_CreatesNewStore()
        {
            // Arrange
            var updateDto = new UpdateMusicReleaseDto
            {
                Title = "Album with Purchase Info",
                PurchaseInfo = new MusicReleasePurchaseInfoDto
                {
                    StoreName = "New Record Store",
                    Price = 25.99m,
                    Currency = "USD",
                    PurchaseDate = new DateTime(2023, 10, 15)
                }
            };

            var existingRelease = new MusicRelease
            {
                Id = 1,
                Title = "Album",
                DateAdded = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            Store? createdStore = null;

            _mockMusicReleaseRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existingRelease);

            _mockUnitOfWork.Setup(u => u.Stores)
                .Returns(_mockStoreRepo.Object);

            _mockStoreRepo.Setup(s => s.GetAsync(
                It.IsAny<Expression<Func<Store, bool>>>(),
                It.IsAny<Func<IQueryable<Store>, IOrderedQueryable<Store>>>(),
                It.IsAny<string>()))
                .ReturnsAsync(new List<Store>()); // No existing stores

            _mockStoreRepo.Setup(s => s.AddAsync(It.IsAny<Store>()))
                .Callback<Store>(s => 
                {
                    s.Id = 100; // Simulate database assigning ID
                    createdStore = s;
                })
                .ReturnsAsync((Store s) => s);

            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            _mockMusicReleaseRepo.Setup(r => r.Update(It.IsAny<MusicRelease>()));

            _mockMapper.Setup(m => m.MapToFullDtoAsync(It.IsAny<MusicRelease>()))
                .ReturnsAsync(new MusicReleaseDto 
                { 
                    Id = 1, 
                    Title = "Album with Purchase Info",
                    PurchaseInfo = new MusicReleasePurchaseInfoDto 
                    { 
                        StoreId = 100,
                        StoreName = "New Record Store",
                        Price = 25.99m,
                        Currency = "USD"
                    }
                });

            // Act
            var result = await _service.UpdateMusicReleaseAsync(1, updateDto);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.PurchaseInfo);
            Assert.Equal(100, result.PurchaseInfo.StoreId);
            Assert.Equal("New Record Store", result.PurchaseInfo.StoreName);
            Assert.NotNull(createdStore);
            Assert.Equal("New Record Store", createdStore.Name);
            _mockStoreRepo.Verify(s => s.AddAsync(It.IsAny<Store>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.AtLeast(2)); // Once for store, once for release
            _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateMusicReleaseAsync_WithExistingStoreName_ReusesExistingStore()
        {
            // Arrange
            var updateDto = new UpdateMusicReleaseDto
            {
                Title = "Album with Purchase Info",
                PurchaseInfo = new MusicReleasePurchaseInfoDto
                {
                    StoreName = "Existing Record Store",
                    Price = 19.99m,
                    Currency = "USD"
                }
            };

            var existingRelease = new MusicRelease
            {
                Id = 1,
                Title = "Album",
                DateAdded = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            var existingStore = new Store
            {
                Id = 50,
                Name = "Existing Record Store"
            };

            _mockMusicReleaseRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existingRelease);

            _mockUnitOfWork.Setup(u => u.Stores)
                .Returns(_mockStoreRepo.Object);

            _mockStoreRepo.Setup(s => s.GetAsync(
                It.IsAny<Expression<Func<Store, bool>>>(),
                It.IsAny<Func<IQueryable<Store>, IOrderedQueryable<Store>>>(),
                It.IsAny<string>()))
                .ReturnsAsync(new List<Store> { existingStore });

            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            _mockMusicReleaseRepo.Setup(r => r.Update(It.IsAny<MusicRelease>()));

            _mockMapper.Setup(m => m.MapToFullDtoAsync(It.IsAny<MusicRelease>()))
                .ReturnsAsync(new MusicReleaseDto 
                { 
                    Id = 1, 
                    Title = "Album with Purchase Info",
                    PurchaseInfo = new MusicReleasePurchaseInfoDto 
                    { 
                        StoreId = 50,
                        StoreName = "Existing Record Store",
                        Price = 19.99m
                    }
                });

            // Act
            var result = await _service.UpdateMusicReleaseAsync(1, updateDto);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.PurchaseInfo);
            Assert.Equal(50, result.PurchaseInfo.StoreId);
            Assert.Equal("Existing Record Store", result.PurchaseInfo.StoreName);
            _mockStoreRepo.Verify(s => s.AddAsync(It.IsAny<Store>()), Times.Never); // No new store created
            _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateMusicReleaseAsync_WithStoreNameCaseInsensitive_ReusesExistingStore()
        {
            // Arrange
            var updateDto = new UpdateMusicReleaseDto
            {
                Title = "Album",
                PurchaseInfo = new MusicReleasePurchaseInfoDto
                {
                    StoreName = "EXISTING RECORD STORE", // Different case
                    Price = 15.99m
                }
            };

            var existingRelease = new MusicRelease { Id = 1, Title = "Album" };
            var existingStore = new Store { Id = 50, Name = "Existing Record Store" };

            _mockMusicReleaseRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingRelease);
            _mockUnitOfWork.Setup(u => u.Stores).Returns(_mockStoreRepo.Object);

            _mockStoreRepo.Setup(s => s.GetAsync(
                It.IsAny<Expression<Func<Store, bool>>>(),
                It.IsAny<Func<IQueryable<Store>, IOrderedQueryable<Store>>>(),
                It.IsAny<string>()))
                .ReturnsAsync(new List<Store> { existingStore });

            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            _mockMusicReleaseRepo.Setup(r => r.Update(It.IsAny<MusicRelease>()));

            _mockMapper.Setup(m => m.MapToFullDtoAsync(It.IsAny<MusicRelease>()))
                .ReturnsAsync(new MusicReleaseDto { Id = 1, PurchaseInfo = new MusicReleasePurchaseInfoDto { StoreId = 50 } });

            // Act
            var result = await _service.UpdateMusicReleaseAsync(1, updateDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(50, result.PurchaseInfo!.StoreId);
            _mockStoreRepo.Verify(s => s.AddAsync(It.IsAny<Store>()), Times.Never);
        }

        [Fact]
        public async Task UpdateMusicReleaseAsync_WithStoreId_UsesProvidedStoreId()
        {
            // Arrange
            var updateDto = new UpdateMusicReleaseDto
            {
                Title = "Album",
                PurchaseInfo = new MusicReleasePurchaseInfoDto
                {
                    StoreId = 75, // Explicit StoreId provided
                    StoreName = "Should Be Ignored",
                    Price = 20.00m
                }
            };

            var existingRelease = new MusicRelease { Id = 1, Title = "Album" };

            _mockMusicReleaseRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingRelease);
            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            _mockMusicReleaseRepo.Setup(r => r.Update(It.IsAny<MusicRelease>()));

            _mockMapper.Setup(m => m.MapToFullDtoAsync(It.IsAny<MusicRelease>()))
                .ReturnsAsync(new MusicReleaseDto { Id = 1, PurchaseInfo = new MusicReleasePurchaseInfoDto { StoreId = 75 } });

            // Act
            var result = await _service.UpdateMusicReleaseAsync(1, updateDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(75, result.PurchaseInfo!.StoreId);
            _mockStoreRepo.Verify(s => s.GetAsync(
                It.IsAny<Expression<Func<Store, bool>>>(),
                It.IsAny<Func<IQueryable<Store>, IOrderedQueryable<Store>>>(),
                It.IsAny<string>()), Times.Never); // No store lookup when ID provided
            _mockStoreRepo.Verify(s => s.AddAsync(It.IsAny<Store>()), Times.Never);
        }

        [Fact]
        public async Task UpdateMusicReleaseAsync_WithWhitespaceStoreName_TrimsWhitespace()
        {
            // Arrange
            var updateDto = new UpdateMusicReleaseDto
            {
                Title = "Album",
                PurchaseInfo = new MusicReleasePurchaseInfoDto
                {
                    StoreName = "  Trimmed Store  ",
                    Price = 12.99m
                }
            };

            var existingRelease = new MusicRelease { Id = 1, Title = "Album" };
            Store? createdStore = null;

            _mockMusicReleaseRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingRelease);
            _mockUnitOfWork.Setup(u => u.Stores).Returns(_mockStoreRepo.Object);

            _mockStoreRepo.Setup(s => s.GetAsync(
                It.IsAny<Expression<Func<Store, bool>>>(),
                It.IsAny<Func<IQueryable<Store>, IOrderedQueryable<Store>>>(),
                It.IsAny<string>()))
                .ReturnsAsync(new List<Store>());

            _mockStoreRepo.Setup(s => s.AddAsync(It.IsAny<Store>()))
                .Callback<Store>(s => { s.Id = 101; createdStore = s; })
                .ReturnsAsync((Store s) => s);

            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            _mockMusicReleaseRepo.Setup(r => r.Update(It.IsAny<MusicRelease>()));

            _mockMapper.Setup(m => m.MapToFullDtoAsync(It.IsAny<MusicRelease>()))
                .ReturnsAsync(new MusicReleaseDto { Id = 1, PurchaseInfo = new MusicReleasePurchaseInfoDto { StoreId = 101 } });

            // Act
            var result = await _service.UpdateMusicReleaseAsync(1, updateDto);

            // Assert
            Assert.NotNull(createdStore);
            Assert.Equal("Trimmed Store", createdStore.Name); // Whitespace trimmed
        }

        [Fact]
        public async Task UpdateMusicReleaseAsync_WithoutPurchaseInfo_UpdatesSuccessfully()
        {
            // Arrange
            var updateDto = new UpdateMusicReleaseDto
            {
                Title = "Album Without Purchase Info",
                ReleaseYear = new DateTime(2023, 1, 1)
            };

            var existingRelease = new MusicRelease { Id = 1, Title = "Old Title" };

            _mockMusicReleaseRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingRelease);
            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            _mockMusicReleaseRepo.Setup(r => r.Update(It.IsAny<MusicRelease>()));

            _mockMapper.Setup(m => m.MapToFullDtoAsync(It.IsAny<MusicRelease>()))
                .ReturnsAsync(new MusicReleaseDto { Id = 1, Title = "Album Without Purchase Info" });

            // Act
            var result = await _service.UpdateMusicReleaseAsync(1, updateDto);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.PurchaseInfo);
            _mockStoreRepo.Verify(s => s.GetAsync(
                It.IsAny<Expression<Func<Store, bool>>>(),
                It.IsAny<Func<IQueryable<Store>, IOrderedQueryable<Store>>>(),
                It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task UpdateMusicReleaseAsync_OnException_RollsBackTransaction()
        {
            // Arrange
            var updateDto = new UpdateMusicReleaseDto
            {
                Title = "Album",
                PurchaseInfo = new MusicReleasePurchaseInfoDto
                {
                    StoreName = "Test Store"
                }
            };

            var existingRelease = new MusicRelease { Id = 1, Title = "Album" };

            _mockMusicReleaseRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingRelease);
            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.Stores).Returns(_mockStoreRepo.Object);

            _mockStoreRepo.Setup(s => s.GetAsync(
                It.IsAny<Expression<Func<Store, bool>>>(),
                It.IsAny<Func<IQueryable<Store>, IOrderedQueryable<Store>>>(),
                It.IsAny<string>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _service.UpdateMusicReleaseAsync(1, updateDto));
            _mockUnitOfWork.Verify(u => u.RollbackTransactionAsync(), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateMusicReleaseAsync_WithEmptyStoreName_IgnoresStoreCreation()
        {
            // Arrange
            var updateDto = new UpdateMusicReleaseDto
            {
                Title = "Album",
                PurchaseInfo = new MusicReleasePurchaseInfoDto
                {
                    StoreName = "   ", // Whitespace only
                    Price = 10.00m
                }
            };

            var existingRelease = new MusicRelease { Id = 1, Title = "Album" };

            _mockMusicReleaseRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingRelease);
            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            _mockMusicReleaseRepo.Setup(r => r.Update(It.IsAny<MusicRelease>()));

            _mockMapper.Setup(m => m.MapToFullDtoAsync(It.IsAny<MusicRelease>()))
                .ReturnsAsync(new MusicReleaseDto { Id = 1, PurchaseInfo = new MusicReleasePurchaseInfoDto { Price = 10.00m } });

            // Act
            var result = await _service.UpdateMusicReleaseAsync(1, updateDto);

            // Assert
            Assert.NotNull(result);
            _mockStoreRepo.Verify(s => s.GetAsync(
                It.IsAny<Expression<Func<Store, bool>>>(),
                It.IsAny<Func<IQueryable<Store>, IOrderedQueryable<Store>>>(),
                It.IsAny<string>()), Times.Never);
            _mockStoreRepo.Verify(s => s.AddAsync(It.IsAny<Store>()), Times.Never);
        }

        #endregion

        #region DeleteMusicReleaseAsync Tests

        [Fact]
        public async Task DeleteMusicReleaseAsync_WithValidId_DeletesRelease()
        {
            // Arrange
            var existingRelease = new MusicRelease
            {
                Id = 1,
                Title = "Album to Delete"
            };

            _mockMusicReleaseRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existingRelease);
            _mockMusicReleaseRepo.Setup(r => r.Delete(It.IsAny<MusicRelease>()));
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _service.DeleteMusicReleaseAsync(1);

            // Assert
            Assert.True(result);
            _mockMusicReleaseRepo.Verify(r => r.Delete(It.IsAny<MusicRelease>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteMusicReleaseAsync_WithInvalidId_ReturnsFalse()
        {
            // Arrange
            _mockMusicReleaseRepo.Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((MusicRelease?)null);

            // Act
            var result = await _service.DeleteMusicReleaseAsync(999);

            // Assert
            Assert.False(result);
            _mockMusicReleaseRepo.Verify(r => r.Delete(It.IsAny<MusicRelease>()), Times.Never);
        }

        #endregion

        #region Additional Coverage Tests

        [Fact]
        public async Task GetMusicReleasesAsync_WithMultipleFilters_ReturnsFilteredResults()
        {
            // Arrange
            var musicReleases = new List<MusicRelease>
            {
                new MusicRelease
                {
                    Id = 1,
                    Title = "Live Album",
                    ReleaseYear = new DateTime(2020, 1, 1),
                    Artists = "[1]",
                    Genres = "[1]",
                    LabelId = 1,
                    CountryId = 1,
                    FormatId = 1,
                    Live = true,
                    Label = new Label { Id = 1, Name = "Test Label" },
                    Format = new Format { Id = 1, Name = "CD" },
                    Country = new Country { Id = 1, Name = "UK" },
                    DateAdded = DateTime.UtcNow
                }
            };

            var pagedResult = new PagedResult<MusicRelease>
            {
                Items = musicReleases,
                Page = 1,
                PageSize = 10,
                TotalCount = 1,
                TotalPages = 1
            };

            _mockMusicReleaseRepo.Setup(r => r.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<MusicRelease, bool>>?>(),
                It.IsAny<Func<IQueryable<MusicRelease>, IOrderedQueryable<MusicRelease>>?>(),
                It.IsAny<string>()
            )).ReturnsAsync(pagedResult);

            // Act
            var result = await _service.GetMusicReleasesAsync(null, 1, 1, 1, 1, 1, true, null, null, 1, 10);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Items);
        }

        [Fact]
        public async Task GetMusicReleaseAsync_WithPurchaseInfo_ReturnsMappedData()
        {
            // Arrange
            var storeId = 1;
            var purchaseInfo = new PurchaseInfo
            {
                StoreID = storeId,
                Price = 12.99m,
                Date = DateTime.Now.AddDays(-30),
                Notes = "Test purchase"
            };

            var musicRelease = new MusicRelease
            {
                Id = 1,
                Title = "Test Album with Purchase",
                ReleaseYear = new DateTime(2020, 1, 1),
                Artists = "[1]",
                PurchaseInfo = JsonSerializer.Serialize(purchaseInfo),
                Label = new Label { Id = 1, Name = "Test Label" },
                DateAdded = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            var expectedDto = new MusicReleaseDto
            {
                Id = 1,
                Title = "Test Album with Purchase",
                PurchaseInfo = new MusicReleasePurchaseInfoDto
                {
                    StoreId = storeId,
                    StoreName = "Test Store",
                    Price = 12.99m
                }
            };

            _mockMusicReleaseRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<string>()))
                .ReturnsAsync(musicRelease);

            _mockMapper.Setup(m => m.MapToFullDtoAsync(It.IsAny<MusicRelease>()))
                .ReturnsAsync(expectedDto);

            // Act
            var result = await _service.GetMusicReleaseAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.PurchaseInfo);
            Assert.Equal(storeId, result.PurchaseInfo.StoreId);
            Assert.Equal("Test Store", result.PurchaseInfo.StoreName);
            Assert.Equal(12.99m, result.PurchaseInfo.Price);
        }

        [Fact]
        public async Task GetCollectionStatisticsAsync_WithComplexData_ReturnsDetailedStatistics()
        {
            // Arrange
            var expectedStats = new CollectionStatisticsDto
            {
                TotalReleases = 3,
                TotalArtists = 2,
                TotalGenres = 2,
                TotalLabels = 2,
                TotalValue = 26.98m,
                AveragePrice = 13.49m,
                MostExpensiveRelease = new MusicReleaseSummaryDto { Id = 2, Title = "Album 2" },
                RecentlyAdded = new List<MusicReleaseSummaryDto>
                {
                    new MusicReleaseSummaryDto { Id = 3, Title = "Album 3" },
                    new MusicReleaseSummaryDto { Id = 2, Title = "Album 2" },
                    new MusicReleaseSummaryDto { Id = 1, Title = "Album 1" }
                }
            };

            _mockStatisticsService.Setup(s => s.GetCollectionStatisticsAsync())
                .ReturnsAsync(expectedStats);

            // Act
            var result = await _service.GetCollectionStatisticsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.TotalReleases);
            Assert.Equal(2, result.TotalArtists);
            Assert.Equal(2, result.TotalGenres);
            Assert.Equal(2, result.TotalLabels);
            Assert.NotNull(result.TotalValue);
            Assert.True(result.TotalValue > 0);
            Assert.NotNull(result.AveragePrice);
            Assert.NotNull(result.MostExpensiveRelease);
            Assert.Equal(3, result.RecentlyAdded.Count);
            _mockStatisticsService.Verify(s => s.GetCollectionStatisticsAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateMusicReleaseAsync_WithNewGenreName_CreatesGenreAndRelease()
        {
            // Arrange
            var createDto = new CreateMusicReleaseDto
            {
                Title = "New Album",
                ReleaseYear = new DateTime(2023, 1, 1),
                ArtistIds = new List<int> { 1 },
                GenreNames = new List<string> { "New Genre" },
                LabelId = 1
            };

            _mockEntityResolver.Setup(e => e.ResolveOrCreateArtistsAsync(
                It.IsAny<List<int>?>(), It.IsAny<List<string>?>(), It.IsAny<CreatedEntitiesDto>()))
                .ReturnsAsync(new List<int> { 1 });

            _mockEntityResolver.Setup(e => e.ResolveOrCreateGenresAsync(
                It.IsAny<List<int>?>(), It.IsAny<List<string>?>(), It.IsAny<CreatedEntitiesDto>()))
                .Callback<List<int>?, List<string>?, CreatedEntitiesDto>((ids, names, created) =>
                {
                    created.Genres = new List<GenreDto> { new GenreDto { Id = 1, Name = "New Genre" } };
                })
                .ReturnsAsync(new List<int> { 1 });

            _mockEntityResolver.Setup(e => e.ResolveOrCreateLabelAsync(
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CreatedEntitiesDto>()))
                .ReturnsAsync(1);

            _mockMusicReleaseRepo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<MusicRelease, bool>>>(),
                It.IsAny<Func<IQueryable<MusicRelease>, IOrderedQueryable<MusicRelease>>?>(),
                It.IsAny<string>()
            )).ReturnsAsync(new List<MusicRelease>());

            _mockMusicReleaseRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<MusicRelease>());

            _mockMusicReleaseRepo.Setup(r => r.AddAsync(It.IsAny<MusicRelease>()))
                .Callback<MusicRelease>(mr => mr.Id = 1)
                .ReturnsAsync((MusicRelease mr) => mr);

            _mockMapper.Setup(m => m.MapToFullDtoAsync(It.IsAny<MusicRelease>()))
                .ReturnsAsync(new MusicReleaseDto { Id = 1, Title = "New Album" });

            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            _mockUnitOfWork.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateMusicReleaseAsync(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Created);
            Assert.NotNull(result.Created.Genres);
            Assert.Single(result.Created.Genres);
            _mockEntityResolver.Verify(e => e.ResolveOrCreateGenresAsync(
                It.IsAny<List<int>?>(), It.IsAny<List<string>?>(), It.IsAny<CreatedEntitiesDto>()), Times.Once);
        }

        [Fact]
        public async Task CreateMusicReleaseAsync_WithExistingArtistAndGenreNames_DoesNotCreateDuplicates()
        {
            // Arrange
            var createDto = new CreateMusicReleaseDto
            {
                Title = "New Album",
                ReleaseYear = new DateTime(2023, 1, 1),
                ArtistNames = new List<string> { "Existing Artist" },
                GenreNames = new List<string> { "Existing Genre" },
                LabelId = 1
            };

            var existingArtist = new Artist { Id = 1, Name = "Existing Artist" };
            var existingGenre = new Genre { Id = 1, Name = "Existing Genre" };
            var label = new Label { Id = 1, Name = "Test Label" };

            _mockArtistRepo.Setup(r => r.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<Artist, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync(existingArtist);
            _mockGenreRepo.Setup(r => r.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<Genre, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync(existingGenre);
            _mockArtistRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingArtist);
            _mockGenreRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingGenre);
            _mockLabelRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(label);

            _mockMusicReleaseRepo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<MusicRelease, bool>>>(),
                It.IsAny<Func<IQueryable<MusicRelease>, IOrderedQueryable<MusicRelease>>?>(),
                It.IsAny<string>()
            )).ReturnsAsync(new List<MusicRelease>());

            _mockMusicReleaseRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<MusicRelease>());

            _mockMusicReleaseRepo.Setup(r => r.AddAsync(It.IsAny<MusicRelease>()))
                .Callback<MusicRelease>(mr => mr.Id = 1)
                .ReturnsAsync((MusicRelease mr) => mr);

            _mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
            _mockUnitOfWork.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateMusicReleaseAsync(createDto);

            // Assert
            Assert.NotNull(result);
            _mockArtistRepo.Verify(r => r.AddAsync(It.IsAny<Artist>()), Times.Never);
            _mockGenreRepo.Verify(r => r.AddAsync(It.IsAny<Genre>()), Times.Never);
        }

        #endregion
    }
}
