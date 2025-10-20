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
            _mockLogger = new Mock<ILogger<MusicReleaseService>>();

            _service = new MusicReleaseService(
                _mockMusicReleaseRepo.Object,
                _mockArtistRepo.Object,
                _mockGenreRepo.Object,
                _mockLabelRepo.Object,
                _mockCountryRepo.Object,
                _mockFormatRepo.Object,
                _mockPackagingRepo.Object,
                _mockStoreRepo.Object,
                _mockUnitOfWork.Object,
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

            var artist = new Artist { Id = artistId, Name = "Test Artist" };
            var genre = new Genre { Id = genreId, Name = "Rock" };

            _mockMusicReleaseRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<string>()))
                .ReturnsAsync(musicRelease);
            _mockArtistRepo.Setup(r => r.GetByIdAsync(artistId))
                .ReturnsAsync(artist);
            _mockGenreRepo.Setup(r => r.GetByIdAsync(genreId))
                .ReturnsAsync(genre);

            // Act
            var result = await _service.GetMusicReleaseAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("Test Album", result.Title);
            Assert.NotNull(result.Artists);
            Assert.Single(result.Artists);
            Assert.Equal("Test Artist", result.Artists[0].Name);
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
            var releases = new List<MusicRelease>
            {
                new MusicRelease
                {
                    Id = 1,
                    Title = "Album 1",
                    ReleaseYear = new DateTime(2020, 1, 1),
                    Artists = "[1]",
                    Genres = "[1]",
                    LabelId = 1,
                    FormatId = 1,
                    CountryId = 1,
                    PurchaseInfo = "{\"Price\": 10.99}",
                    DateAdded = DateTime.UtcNow
                },
                new MusicRelease
                {
                    Id = 2,
                    Title = "Album 2",
                    ReleaseYear = new DateTime(2021, 1, 1),
                    Artists = "[1,2]",
                    Genres = "[1,2]",
                    LabelId = 2,
                    FormatId = 1,
                    CountryId = 1,
                    PurchaseInfo = "{\"Price\": 15.99}",
                    DateAdded = DateTime.UtcNow
                }
            };

            var formats = new List<Format>
            {
                new Format { Id = 1, Name = "CD" }
            };

            var countries = new List<Country>
            {
                new Country { Id = 1, Name = "UK" }
            };

            var genres = new List<Genre>
            {
                new Genre { Id = 1, Name = "Rock" },
                new Genre { Id = 2, Name = "Pop" }
            };

            _mockMusicReleaseRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(releases);
            _mockFormatRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(formats);
            _mockCountryRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(countries);
            _mockGenreRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(genres);

            // Act
            var result = await _service.GetCollectionStatisticsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalReleases);
            Assert.True(result.TotalValue > 0);
        }

        [Fact]
        public async Task GetCollectionStatisticsAsync_WithNoReleases_ReturnsEmptyStatistics()
        {
            // Arrange
            _mockMusicReleaseRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<MusicRelease>());

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

            var artist = new Artist { Id = 1, Name = "Test Artist" };
            var genre = new Genre { Id = 1, Name = "Rock" };
            var label = new Label { Id = 1, Name = "Test Label" };
            var country = new Country { Id = 1, Name = "UK" };
            var format = new Format { Id = 1, Name = "CD" };

            _mockArtistRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(artist);
            _mockGenreRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(genre);
            _mockLabelRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(label);
            _mockCountryRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(country);
            _mockFormatRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(format);

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

            var genre = new Genre { Id = 1, Name = "Rock" };
            var label = new Label { Id = 1, Name = "Test Label" };

            _mockArtistRepo.Setup(r => r.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<Artist, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync((Artist?)null);
            _mockArtistRepo.Setup(r => r.AddAsync(It.IsAny<Artist>()))
                .Callback<Artist>(a => a.Id = 1)
                .ReturnsAsync((Artist a) => a);
            _mockArtistRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new Artist { Id = 1, Name = "New Artist" });

            _mockGenreRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(genre);
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
            Assert.NotNull(result.Created);
            Assert.NotNull(result.Created.Artists);
            Assert.Single(result.Created.Artists);
            Assert.Equal("New Artist", result.Created.Artists[0].Name);
            _mockArtistRepo.Verify(r => r.AddAsync(It.IsAny<Artist>()), Times.Once);
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

            var artist = new Artist { Id = 1, Name = "Test Artist" };
            var genre = new Genre { Id = 1, Name = "Rock" };

            _mockMusicReleaseRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<string>()))
                .ReturnsAsync((MusicRelease?)null);
            _mockMusicReleaseRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existingRelease);
            _mockArtistRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(artist);
            _mockGenreRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(genre);
            _mockLabelRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new Label { Id = 1, Name = "Test Label" });
            _mockMusicReleaseRepo.Setup(r => r.Update(It.IsAny<MusicRelease>()));
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _service.UpdateMusicReleaseAsync(1, updateDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Updated Album", result.Title);
            _mockMusicReleaseRepo.Verify(r => r.Update(It.IsAny<MusicRelease>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
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

            // Act
            var result = await _service.UpdateMusicReleaseAsync(999, updateDto);

            // Assert
            Assert.Null(result);
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

            var artist = new Artist { Id = 1, Name = "Test Artist" };
            var store = new Store { Id = storeId, Name = "Test Store" };

            _mockMusicReleaseRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<string>()))
                .ReturnsAsync(musicRelease);
            _mockArtistRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(artist);
            _mockStoreRepo.Setup(r => r.GetByIdAsync(storeId))
                .ReturnsAsync(store);

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
            var purchaseInfo1 = new PurchaseInfo { Price = 10.99m };
            var purchaseInfo2 = new PurchaseInfo { Price = 15.99m };

            var releases = new List<MusicRelease>
            {
                new MusicRelease
                {
                    Id = 1,
                    Title = "Album 1",
                    ReleaseYear = new DateTime(2020, 1, 1),
                    Artists = "[1]",
                    Genres = "[1]",
                    LabelId = 1,
                    FormatId = 1,
                    CountryId = 1,
                    PurchaseInfo = JsonSerializer.Serialize(purchaseInfo1),
                    DateAdded = DateTime.UtcNow.AddDays(-10)
                },
                new MusicRelease
                {
                    Id = 2,
                    Title = "Album 2",
                    ReleaseYear = new DateTime(2021, 1, 1),
                    Artists = "[2]",
                    Genres = "[2]",
                    LabelId = 2,
                    FormatId = 2,
                    CountryId = 2,
                    PurchaseInfo = JsonSerializer.Serialize(purchaseInfo2),
                    DateAdded = DateTime.UtcNow.AddDays(-5)
                },
                new MusicRelease
                {
                    Id = 3,
                    Title = "Album 3",
                    ReleaseYear = new DateTime(2020, 6, 1),
                    Artists = "[1,2]",
                    Genres = "[1,2]",
                    LabelId = 1,
                    FormatId = 1,
                    CountryId = 1,
                    PurchaseInfo = null,
                    DateAdded = DateTime.UtcNow.AddDays(-1)
                }
            };

            var formats = new List<Format>
            {
                new Format { Id = 1, Name = "CD" },
                new Format { Id = 2, Name = "Vinyl" }
            };

            var countries = new List<Country>
            {
                new Country { Id = 1, Name = "UK" },
                new Country { Id = 2, Name = "US" }
            };

            var genres = new List<Genre>
            {
                new Genre { Id = 1, Name = "Rock" },
                new Genre { Id = 2, Name = "Pop" }
            };

            _mockMusicReleaseRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(releases);
            _mockFormatRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(formats);
            _mockCountryRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(countries);
            _mockGenreRepo.Setup(r => r.GetAllAsync())
                .ReturnsAsync(genres);

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

            var artist = new Artist { Id = 1, Name = "Test Artist" };
            var label = new Label { Id = 1, Name = "Test Label" };

            _mockArtistRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(artist);
            _mockLabelRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(label);

            _mockGenreRepo.Setup(r => r.GetFirstOrDefaultAsync(It.IsAny<Expression<Func<Genre, bool>>>(), It.IsAny<string>()))
                .ReturnsAsync((Genre?)null);
            _mockGenreRepo.Setup(r => r.AddAsync(It.IsAny<Genre>()))
                .Callback<Genre>(g => g.Id = 1)
                .ReturnsAsync((Genre g) => g);
            _mockGenreRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new Genre { Id = 1, Name = "New Genre" });

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
            Assert.NotNull(result.Created);
            Assert.NotNull(result.Created.Genres);
            Assert.Single(result.Created.Genres);
            _mockGenreRepo.Verify(r => r.AddAsync(It.IsAny<Genre>()), Times.Once);
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
