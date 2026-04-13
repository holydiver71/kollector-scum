using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Text.Json;
using KollectorScum.Api.Application.Queries;
using KollectorScum.Api.Data;
using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using KollectorScum.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KollectorScum.Tests.Services
{
    /// <summary>
    /// Unit tests for <see cref="MusicReleaseQueryService"/>.
    /// Covers GetMusicReleasesAsync, GetMusicReleaseAsync, GetSearchSuggestionsAsync,
    /// and GetCollectionStatisticsAsync.
    /// </summary>
    public class MusicReleaseQueryServiceTests
    {
        private readonly Mock<IMusicReleaseRepository> _mockMusicReleaseRepo;
        private readonly Mock<IRepository<Artist>> _mockArtistRepo;
        private readonly Mock<IRepository<Label>> _mockLabelRepo;
        private readonly Mock<IMusicReleaseMapperService> _mockMapper;
        private readonly Mock<ICollectionStatisticsService> _mockStatisticsService;
        private readonly Mock<IDiscogsService> _mockDiscogsService;
        private readonly KollectorScumDbContext _context;
        private readonly Mock<ILogger<MusicReleaseQueryService>> _mockLogger;
        private readonly Mock<IUserContext> _mockUserContext;
        private readonly MusicReleaseQueryService _service;
        private readonly Guid _defaultUserId;

        /// <summary>
        /// Initialises shared mocks and a real <see cref="MusicReleaseQueryService"/> instance
        /// backed by an in-memory EF Core database.
        /// </summary>
        public MusicReleaseQueryServiceTests()
        {
            _mockMusicReleaseRepo = new Mock<IMusicReleaseRepository>();
            _mockArtistRepo = new Mock<IRepository<Artist>>();
            _mockLabelRepo = new Mock<IRepository<Label>>();
            _mockMapper = new Mock<IMusicReleaseMapperService>();
            _mockStatisticsService = new Mock<ICollectionStatisticsService>();
            _mockDiscogsService = new Mock<IDiscogsService>();
            _mockLogger = new Mock<ILogger<MusicReleaseQueryService>>();
            _mockUserContext = new Mock<IUserContext>();
            _defaultUserId = Guid.Parse("12337b39-c346-449c-b269-33b2e820d74f");
            _mockUserContext.Setup(u => u.GetActingUserId()).Returns(_defaultUserId);
            _mockUserContext.Setup(u => u.GetUserId()).Returns(_defaultUserId);
            _mockUserContext.Setup(u => u.IsAdmin()).Returns(false);

            var options = new DbContextOptionsBuilder<KollectorScumDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new KollectorScumDbContext(options);

            var handlerLogger = new Mock<ILogger<GetMusicReleasesQueryHandler>>().Object;
            var handler = new GetMusicReleasesQueryHandler(
                _mockMusicReleaseRepo.Object,
                _mockMapper.Object,
                _context,
                handlerLogger,
                _mockUserContext.Object);

            _service = new MusicReleaseQueryService(
                _mockMusicReleaseRepo.Object,
                _mockArtistRepo.Object,
                _mockLabelRepo.Object,
                _mockMapper.Object,
                _mockStatisticsService.Object,
                _context,
                _mockLogger.Object,
                _mockUserContext.Object,
                handler,
                _mockDiscogsService.Object
            );
        }

        // ── Helper: configure GetPagedAsync to return a ready-made paged result ──

        /// <summary>
        /// Sets up <see cref="IMusicReleaseRepository.GetPagedAsync"/> to return the supplied releases
        /// and configures the mapper to echo them as summary DTOs.
        /// </summary>
        private void SetupPagedRepoAndMapper(List<MusicRelease> releases)
        {
            var pagedResult = new PagedResult<MusicRelease>
            {
                Items = releases,
                Page = 1,
                PageSize = 10,
                TotalCount = releases.Count,
                TotalPages = 1
            };

            _mockMusicReleaseRepo.Setup(x => x.GetPagedAsync(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<Expression<Func<MusicRelease, bool>>>(),
                    It.IsAny<Func<IQueryable<MusicRelease>, IOrderedQueryable<MusicRelease>>>(),
                    It.IsAny<string>()))
                .ReturnsAsync(pagedResult);

            _mockMapper.Setup(m => m.MapToSummaryDtosAsync(It.IsAny<IEnumerable<MusicRelease>>()))
                .ReturnsAsync((IEnumerable<MusicRelease> mrs) =>
                    mrs.Select(mr => new MusicReleaseSummaryDto { Id = mr.Id, Title = mr.Title }).ToList());
        }

        #region GetMusicReleasesAsync Tests

        [Fact]
        public async Task GetMusicReleasesAsync_WithUserContext_FiltersByUserId()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            _mockUserContext.Setup(x => x.GetActingUserId()).Returns(userId);

            var parameters = new MusicReleaseQueryParameters
            {
                Pagination = new PaginationParameters { PageNumber = 1, PageSize = 10 }
            };

            // Capture the filter passed to GetPagedAsync
            Expression<Func<MusicRelease, bool>>? capturedFilter = null;
            _mockMusicReleaseRepo.Setup(x => x.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<MusicRelease, bool>>>(),
                It.IsAny<Func<IQueryable<MusicRelease>, IOrderedQueryable<MusicRelease>>>(),
                It.IsAny<string>()
            )).Callback<int, int, Expression<Func<MusicRelease, bool>>, Func<IQueryable<MusicRelease>, IOrderedQueryable<MusicRelease>>, string>(
                (page, size, filter, orderBy, include) => capturedFilter = filter
            ).ReturnsAsync(new PagedResult<MusicRelease>
            {
                Items = new List<MusicRelease>(),
                TotalCount = 0
            });

            // Act
            await _service.GetMusicReleasesAsync(parameters);

            // Assert
            Assert.NotNull(capturedFilter);
            
            // Compile and test the filter
            var func = capturedFilter!.Compile();
            
            var myRelease = new MusicRelease { UserId = userId, Title = "My Release" };
            var otherRelease = new MusicRelease { UserId = otherUserId, Title = "Other Release" };

            Assert.True(func(myRelease), "Filter should match user's release");
            Assert.False(func(otherRelease), "Filter should NOT match other user's release");
        }

        [Fact]
        public async Task GetMusicReleasesAsync_WithNoUserContext_ReturnsEmpty()
        {
            // Arrange — no user in context
            _mockUserContext.Setup(x => x.GetActingUserId()).Returns((Guid?)null);

            var parameters = new MusicReleaseQueryParameters
            {
                Pagination = new PaginationParameters { PageNumber = 1, PageSize = 10 }
            };

            // Act
            var result = await _service.GetMusicReleasesAsync(parameters);

            // Assert — service short-circuits before calling the repository
            Assert.NotNull(result);
            Assert.Empty(result.Items);
            Assert.Equal(0, result.TotalCount);
            _mockMusicReleaseRepo.Verify(
                x => x.GetPagedAsync(
                    It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<Expression<Func<MusicRelease, bool>>>(),
                    It.IsAny<Func<IQueryable<MusicRelease>, IOrderedQueryable<MusicRelease>>>(),
                    It.IsAny<string>()),
                Times.Never,
                "Repository should not be called when there is no user context");
        }

        /// <summary>
        /// Mirrors legacy test: GetMusicReleasesAsync_WithSearchFilter_ReturnsFilteredResults.
        /// When a Search term is set the filter should exclude releases whose title does not match.
        /// </summary>
        [Fact]
        public async Task GetMusicReleasesAsync_WithSearchFilter_ReturnsFilteredResults()
        {
            // Arrange
            var matchingRelease = new MusicRelease { Id = 1, Title = "Metal Album", UserId = _defaultUserId };
            SetupPagedRepoAndMapper(new List<MusicRelease> { matchingRelease });

            var parameters = new MusicReleaseQueryParameters
            {
                Search = "Metal",
                Pagination = new PaginationParameters { PageNumber = 1, PageSize = 10 }
            };

            // Capture the compiled filter so we can verify it works
            Expression<Func<MusicRelease, bool>>? capturedFilter = null;
            _mockMusicReleaseRepo.Setup(x => x.GetPagedAsync(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<Expression<Func<MusicRelease, bool>>>(),
                    It.IsAny<Func<IQueryable<MusicRelease>, IOrderedQueryable<MusicRelease>>>(),
                    It.IsAny<string>()))
                .Callback<int, int, Expression<Func<MusicRelease, bool>>, Func<IQueryable<MusicRelease>, IOrderedQueryable<MusicRelease>>, string>(
                    (page, size, filter, orderBy, include) => capturedFilter = filter)
                .ReturnsAsync(new PagedResult<MusicRelease>
                {
                    Items = new List<MusicRelease> { matchingRelease },
                    Page = 1, PageSize = 10, TotalCount = 1, TotalPages = 1
                });

            _mockMapper.Setup(m => m.MapToSummaryDtosAsync(It.IsAny<IEnumerable<MusicRelease>>()))
                .ReturnsAsync(new List<MusicReleaseSummaryDto> { new MusicReleaseSummaryDto { Id = 1, Title = "Metal Album" } });

            // Act
            var result = await _service.GetMusicReleasesAsync(parameters);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Items);

            // Verify the filter discriminates titles correctly
            Assert.NotNull(capturedFilter);
            var func = capturedFilter!.Compile();
            var nonMatchingRelease = new MusicRelease { Id = 2, Title = "Jazz Record", UserId = _defaultUserId };
            Assert.True(func(matchingRelease), "Filter should match 'Metal Album' for search 'Metal'");
            Assert.False(func(nonMatchingRelease), "Filter should NOT match 'Jazz Record' for search 'Metal'");
        }

        /// <summary>
        /// Mirrors legacy test: GetMusicReleasesAsync_WithYearRangeFilter_ReturnsFilteredResults.
        /// When YearFrom and YearTo are set the filter excludes releases outside the year range.
        /// </summary>
        [Fact]
        public async Task GetMusicReleasesAsync_WithYearRangeFilter_ReturnsFilteredResults()
        {
            // Arrange
            var release2020 = new MusicRelease { Id = 1, Title = "2020 Album", UserId = _defaultUserId, ReleaseYear = new DateTime(2020, 6, 1, 0, 0, 0, DateTimeKind.Utc) };
            var release2015 = new MusicRelease { Id = 2, Title = "2015 Album", UserId = _defaultUserId, ReleaseYear = new DateTime(2015, 6, 1, 0, 0, 0, DateTimeKind.Utc) };

            Expression<Func<MusicRelease, bool>>? capturedFilter = null;
            _mockMusicReleaseRepo.Setup(x => x.GetPagedAsync(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<Expression<Func<MusicRelease, bool>>>(),
                    It.IsAny<Func<IQueryable<MusicRelease>, IOrderedQueryable<MusicRelease>>>(),
                    It.IsAny<string>()))
                .Callback<int, int, Expression<Func<MusicRelease, bool>>, Func<IQueryable<MusicRelease>, IOrderedQueryable<MusicRelease>>, string>(
                    (page, size, filter, orderBy, include) => capturedFilter = filter)
                .ReturnsAsync(new PagedResult<MusicRelease>
                {
                    Items = new List<MusicRelease> { release2020 },
                    Page = 1, PageSize = 10, TotalCount = 1, TotalPages = 1
                });

            _mockMapper.Setup(m => m.MapToSummaryDtosAsync(It.IsAny<IEnumerable<MusicRelease>>()))
                .ReturnsAsync(new List<MusicReleaseSummaryDto> { new MusicReleaseSummaryDto { Id = 1, Title = "2020 Album" } });

            var parameters = new MusicReleaseQueryParameters
            {
                YearFrom = 2018,
                YearTo = 2022,
                Pagination = new PaginationParameters { PageNumber = 1, PageSize = 10 }
            };

            // Act
            var result = await _service.GetMusicReleasesAsync(parameters);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(capturedFilter);

            var func = capturedFilter!.Compile();
            Assert.True(func(release2020), "2020 album should be inside 2018–2022 range");
            Assert.False(func(release2015), "2015 album should be outside 2018–2022 range");
        }

        /// <summary>
        /// Mirrors legacy test: GetMusicReleasesAsync_WhenMapperThrows_PropagatesOriginalException.
        /// Verifies that mapper exceptions propagate without being swallowed.
        /// </summary>
        [Fact]
        public async Task GetMusicReleasesAsync_WhenMapperThrows_PropagatesOriginalException()
        {
            // Arrange
            var release = new MusicRelease { Id = 1, Title = "Album", UserId = _defaultUserId };

            _mockMusicReleaseRepo.Setup(x => x.GetPagedAsync(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<Expression<Func<MusicRelease, bool>>>(),
                    It.IsAny<Func<IQueryable<MusicRelease>, IOrderedQueryable<MusicRelease>>>(),
                    It.IsAny<string>()))
                .ReturnsAsync(new PagedResult<MusicRelease>
                {
                    Items = new List<MusicRelease> { release },
                    Page = 1, PageSize = 10, TotalCount = 1, TotalPages = 1
                });

            _mockMapper.Setup(m => m.MapToSummaryDtosAsync(It.IsAny<IEnumerable<MusicRelease>>()))
                .ThrowsAsync(new InvalidOperationException("Mapper failure"));

            var parameters = new MusicReleaseQueryParameters
            {
                Pagination = new PaginationParameters { PageNumber = 1, PageSize = 10 }
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.GetMusicReleasesAsync(parameters));
        }

        /// <summary>
        /// Mirrors legacy test: GetMusicReleasesAsync_WithMultipleFilters_ReturnsFilteredResults.
        /// Combines several query parameters and verifies the paged result is returned correctly.
        /// </summary>
        [Fact]
        public async Task GetMusicReleasesAsync_WithMultipleFilters_ReturnsFilteredResults()
        {
            // Arrange
            var release = new MusicRelease
            {
                Id = 1,
                Title = "Live Album",
                UserId = _defaultUserId,
                ReleaseYear = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Artists = "[1]",
                Genres = "[1]",
                LabelId = 1,
                CountryId = 1,
                FormatId = 1,
                Live = true
            };

            SetupPagedRepoAndMapper(new List<MusicRelease> { release });

            var parameters = new MusicReleaseQueryParameters
            {
                ArtistId = 1,
                GenreId = 1,
                LabelId = 1,
                CountryId = 1,
                FormatId = 1,
                Live = true,
                Pagination = new PaginationParameters { PageNumber = 1, PageSize = 10 }
            };

            // Act
            var result = await _service.GetMusicReleasesAsync(parameters);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Items);
        }

        #endregion

        #region GetMusicReleaseAsync Tests

        /// <summary>
        /// Mirrors legacy test: GetMusicReleaseAsync_WithValidId_ReturnsRelease.
        /// When the release exists and is owned by the current user the mapped DTO is returned.
        /// </summary>
        [Fact]
        public async Task GetMusicReleaseAsync_WithValidId_ReturnsRelease()
        {
            // Arrange
            var release = new MusicRelease
            {
                Id = 1,
                Title = "Test Album",
                UserId = _defaultUserId,
                Media = JsonSerializer.Serialize(new List<object>
                {
                    new { Title = "Side A", Tracks = new[] { new { Title = "Track 1" } } }
                })
            };

            _mockMusicReleaseRepo
                .Setup(r => r.GetByIdAsync(1, "Label,Country,Format,Packaging"))
                .ReturnsAsync(release);

            _mockMapper.Setup(m => m.MapToFullDtoAsync(It.IsAny<MusicRelease>()))
                .ReturnsAsync(new MusicReleaseDto { Id = 1, Title = "Test Album", DateAdded = DateTime.UtcNow, LastModified = DateTime.UtcNow });

            // Act
            var result = await _service.GetMusicReleaseAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result!.Id);
            Assert.Equal("Test Album", result.Title);
        }

        /// <summary>
        /// Mirrors legacy test: GetMusicReleaseAsync_WithInvalidId_ReturnsNull.
        /// When the release does not exist the service returns null.
        /// </summary>
        [Fact]
        public async Task GetMusicReleaseAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            _mockMusicReleaseRepo
                .Setup(r => r.GetByIdAsync(999, "Label,Country,Format,Packaging"))
                .ReturnsAsync((MusicRelease?)null);

            // Act
            var result = await _service.GetMusicReleaseAsync(999);

            // Assert
            Assert.Null(result);
        }

        /// <summary>
        /// Mirrors legacy test: GetMusicReleaseAsync_WithPurchaseInfo_ReturnsMappedData.
        /// Verifies purchase info stored in the release is preserved through mapping.
        /// </summary>
        [Fact]
        public async Task GetMusicReleaseAsync_WithPurchaseInfo_ReturnsMappedData()
        {
            // Arrange
            var purchaseInfo = new KollectorScum.Api.Models.ValueObjects.PurchaseInfo
            {
                StoreID = 1,
                Price = 12.99m,
                Notes = "Test purchase"
            };

            var release = new MusicRelease
            {
                Id = 1,
                Title = "Test Album with Purchase",
                UserId = _defaultUserId,
                PurchaseInfo = JsonSerializer.Serialize(purchaseInfo),
                // Provide non-empty media so tracklist backfill is skipped
                Media = JsonSerializer.Serialize(new List<object>
                {
                    new { Title = "Side A", Tracks = new[] { new { Title = "Track 1" } } }
                })
            };

            _mockMusicReleaseRepo
                .Setup(r => r.GetByIdAsync(1, "Label,Country,Format,Packaging"))
                .ReturnsAsync(release);

            var expectedDto = new MusicReleaseDto
            {
                Id = 1,
                Title = "Test Album with Purchase",
                PurchaseInfo = new MusicReleasePurchaseInfoDto { StoreId = 1, Price = 12.99m },
                DateAdded = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            _mockMapper.Setup(m => m.MapToFullDtoAsync(It.IsAny<MusicRelease>()))
                .ReturnsAsync(expectedDto);

            // Act
            var result = await _service.GetMusicReleaseAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result!.PurchaseInfo);
            Assert.Equal(1, result.PurchaseInfo!.StoreId);
            Assert.Equal(12.99m, result.PurchaseInfo.Price);
        }

        [Fact]
        public async Task GetMusicReleaseAsync_BackfillsMediaFromDiscogs_WhenMediaMissing()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _mockUserContext.Setup(x => x.GetActingUserId()).Returns(userId);

            var release = new MusicRelease
            {
                Id = 38549,
                UserId = userId,
                Title = "Futhark Dawning / Wisdom & Darkness",
                DiscogsId = 8895299,
                Media = null,
                Artists = "[3901470]",
                Genres = "[664911,664934]",
                ReleaseYear = new DateTime(2016, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            };

            _mockMusicReleaseRepo
                .Setup(r => r.GetByIdAsync(38549, "Label,Country,Format,Packaging"))
                .ReturnsAsync(release);

            _mockDiscogsService
                .Setup(s => s.GetReleaseDetailsAsync("8895299"))
                .ReturnsAsync(new DiscogsReleaseDto
                {
                    Tracklist = new List<DiscogsTrackDto>
                    {
                        new DiscogsTrackDto { Title = "Into The Hall", Duration = "2:58" },
                        new DiscogsTrackDto { Title = "Wisdom Of The Runes", Duration = "4:16" }
                    }
                });

            _mockMapper
                .Setup(m => m.MapToFullDtoAsync(It.IsAny<MusicRelease>()))
                .ReturnsAsync((MusicRelease mr) =>
                {
                    var media = string.IsNullOrWhiteSpace(mr.Media)
                        ? null
                        : JsonSerializer.Deserialize<List<MusicReleaseMediaDto>>(mr.Media);
                    return new MusicReleaseDto
                    {
                        Id = mr.Id,
                        Title = mr.Title,
                        Media = media,
                        DateAdded = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow
                    };
                });

            // Act
            var result = await _service.GetMusicReleaseAsync(38549);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result!.Media);
            Assert.Single(result.Media!);
            Assert.NotNull(result.Media![0].Tracks);
            Assert.Equal(2, result.Media[0].Tracks!.Count);
            Assert.Equal("Into The Hall", result.Media[0].Tracks[0].Title);
            _mockDiscogsService.Verify(s => s.GetReleaseDetailsAsync("8895299"), Times.Once);
        }

        #endregion

        #region GetSearchSuggestionsAsync Tests

        /// <summary>
        /// Mirrors legacy test: GetSearchSuggestionsAsync_WithValidQuery_ReturnsSuggestions.
        /// When the query is at least 2 characters long the service returns matching suggestions.
        /// </summary>
        [Fact]
        public async Task GetSearchSuggestionsAsync_WithValidQuery_ReturnsSuggestions()
        {
            // Arrange
            var release = new MusicRelease { Id = 1, Title = "Metal Album", UserId = _defaultUserId };

            _mockMusicReleaseRepo.Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<MusicRelease, bool>>>(),
                    It.IsAny<Func<IQueryable<MusicRelease>, IOrderedQueryable<MusicRelease>>>(),
                    It.IsAny<string>()))
                .ReturnsAsync(new List<MusicRelease> { release });

            _mockArtistRepo.Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Artist, bool>>>(),
                    It.IsAny<Func<IQueryable<Artist>, IOrderedQueryable<Artist>>>(),
                    It.IsAny<string>()))
                .ReturnsAsync(new List<Artist>());

            _mockLabelRepo.Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Label, bool>>>(),
                    It.IsAny<Func<IQueryable<Label>, IOrderedQueryable<Label>>>(),
                    It.IsAny<string>()))
                .ReturnsAsync(new List<Label>());

            // Act
            var result = await _service.GetSearchSuggestionsAsync("Metal", 10);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Contains(result, s => s.Type == "release" && s.Name == "Metal Album");
        }

        /// <summary>
        /// Mirrors legacy test: GetSearchSuggestionsAsync_WithShortQuery_ReturnsEmptyList.
        /// A query shorter than 2 characters returns an empty list without hitting the repos.
        /// </summary>
        [Fact]
        public async Task GetSearchSuggestionsAsync_WithShortQuery_ReturnsEmptyList()
        {
            // Arrange – no repo setups needed

            // Act
            var result = await _service.GetSearchSuggestionsAsync("M", 10);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
            _mockMusicReleaseRepo.Verify(r => r.GetAsync(
                It.IsAny<Expression<Func<MusicRelease, bool>>>(),
                It.IsAny<Func<IQueryable<MusicRelease>, IOrderedQueryable<MusicRelease>>>(),
                It.IsAny<string>()), Times.Never);
        }

        /// <summary>
        /// Mirrors legacy test: GetSearchSuggestionsAsync_WithNullQuery_ReturnsEmptyList.
        /// A null query returns an empty list without hitting the repos.
        /// </summary>
        [Fact]
        public async Task GetSearchSuggestionsAsync_WithNullQuery_ReturnsEmptyList()
        {
            // Act
            var result = await _service.GetSearchSuggestionsAsync(null!, 10);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion

        #region GetCollectionStatisticsAsync Tests

        /// <summary>
        /// Mirrors legacy test: GetCollectionStatisticsAsync_WithReleases_ReturnsStatistics.
        /// The service delegates entirely to ICollectionStatisticsService and returns its result.
        /// </summary>
        [Fact]
        public async Task GetCollectionStatisticsAsync_WithReleases_ReturnsStatistics()
        {
            // Arrange
            var expectedStats = new CollectionStatisticsDto
            {
                TotalReleases = 10,
                TotalArtists = 5,
                TotalGenres = 3,
                TotalLabels = 2
            };

            _mockStatisticsService
                .Setup(s => s.GetCollectionStatisticsAsync())
                .ReturnsAsync(expectedStats);

            // Act
            var result = await _service.GetCollectionStatisticsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(10, result.TotalReleases);
            Assert.Equal(5, result.TotalArtists);
            Assert.Equal(3, result.TotalGenres);
            Assert.Equal(2, result.TotalLabels);
            _mockStatisticsService.Verify(s => s.GetCollectionStatisticsAsync(), Times.Once);
        }

        /// <summary>
        /// Mirrors legacy test: GetCollectionStatisticsAsync_WithNoReleases_ReturnsEmptyStatistics.
        /// When the collection is empty the statistics have zero counts.
        /// </summary>
        [Fact]
        public async Task GetCollectionStatisticsAsync_WithNoReleases_ReturnsEmptyStatistics()
        {
            // Arrange
            var emptyStats = new CollectionStatisticsDto
            {
                TotalReleases = 0,
                TotalArtists = 0,
                TotalGenres = 0,
                TotalLabels = 0
            };

            _mockStatisticsService
                .Setup(s => s.GetCollectionStatisticsAsync())
                .ReturnsAsync(emptyStats);

            // Act
            var result = await _service.GetCollectionStatisticsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.TotalReleases);
            Assert.Equal(0, result.TotalArtists);
        }

        /// <summary>
        /// Mirrors legacy test: GetCollectionStatisticsAsync_WithComplexData_ReturnsDetailedStatistics.
        /// Verifies that breakdown lists (by year, genre, format) are passed through from the service.
        /// </summary>
        [Fact]
        public async Task GetCollectionStatisticsAsync_WithComplexData_ReturnsDetailedStatistics()
        {
            // Arrange
            var complexStats = new CollectionStatisticsDto
            {
                TotalReleases = 25,
                TotalArtists = 15,
                TotalValue = 299.95m,
                AveragePrice = 12.00m,
                ReleasesByYear = new List<YearStatisticDto>
                {
                    new YearStatisticDto { Year = 2020, Count = 5 },
                    new YearStatisticDto { Year = 2021, Count = 10 }
                },
                ReleasesByGenre = new List<GenreStatisticDto>
                {
                    new GenreStatisticDto { GenreId = 1, GenreName = "Rock", Count = 12, Percentage = 48 }
                },
                ReleasesByFormat = new List<FormatStatisticDto>
                {
                    new FormatStatisticDto { FormatId = 1, FormatName = "Vinyl", Count = 20, Percentage = 80 }
                }
            };

            _mockStatisticsService
                .Setup(s => s.GetCollectionStatisticsAsync())
                .ReturnsAsync(complexStats);

            // Act
            var result = await _service.GetCollectionStatisticsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(25, result.TotalReleases);
            Assert.Equal(299.95m, result.TotalValue);
            Assert.Equal(2, result.ReleasesByYear.Count);
            Assert.Single(result.ReleasesByGenre);
            Assert.Equal("Rock", result.ReleasesByGenre[0].GenreName);
            Assert.Single(result.ReleasesByFormat);
            Assert.Equal("Vinyl", result.ReleasesByFormat[0].FormatName);
        }

        #endregion
    }
}
