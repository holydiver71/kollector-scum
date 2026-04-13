using System.Linq.Expressions;
using KollectorScum.Api.Application.Queries;
using KollectorScum.Api.Data;
using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KollectorScum.Tests.Application
{
    /// <summary>
    /// Unit tests for GetMusicReleasesQueryHandler.
    /// Verifies the read path for paginated music release lists including
    /// artist-sort, standard paging, user isolation, and empty-context early exit.
    /// </summary>
    public class GetMusicReleasesQueryHandlerTests : IDisposable
    {
        private static readonly Guid DefaultUserId = Guid.Parse("12337b39-c346-449c-b269-33b2e820d74f");

        private readonly Mock<IMusicReleaseRepository> _mockRepo;
        private readonly Mock<IMusicReleaseMapperService> _mockMapper;
        private readonly Mock<IUserContext> _mockUserContext;
        private readonly Mock<ILogger<GetMusicReleasesQueryHandler>> _mockLogger;
        private readonly KollectorScumDbContext _context;
        private readonly GetMusicReleasesQueryHandler _handler;

        public GetMusicReleasesQueryHandlerTests()
        {
            _mockRepo = new Mock<IMusicReleaseRepository>();
            _mockMapper = new Mock<IMusicReleaseMapperService>();
            _mockUserContext = new Mock<IUserContext>();
            _mockLogger = new Mock<ILogger<GetMusicReleasesQueryHandler>>();

            _mockUserContext.Setup(u => u.GetActingUserId()).Returns(DefaultUserId);

            var options = new DbContextOptionsBuilder<KollectorScumDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new KollectorScumDbContext(options);

            _handler = new GetMusicReleasesQueryHandler(
                _mockRepo.Object,
                _mockMapper.Object,
                _context,
                _mockLogger.Object,
                _mockUserContext.Object);
        }

        public void Dispose() => _context.Dispose();

        // ── Default paged path ──────────────────────────────────────────────────

        /// <summary>Handler returns a paged result from the repository for default parameters.</summary>
        [Fact]
        public async Task HandleAsync_WithDefaultParameters_ReturnsPagedResult()
        {
            // Arrange
            var releases = new List<MusicRelease>
            {
                new() { Id = 1, Title = "Album A", UserId = DefaultUserId, DateAdded = DateTime.UtcNow },
                new() { Id = 2, Title = "Album B", UserId = DefaultUserId, DateAdded = DateTime.UtcNow }
            };

            _mockRepo.Setup(r => r.GetPagedAsync(
                    It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<Expression<Func<MusicRelease, bool>>>(),
                    It.IsAny<Func<IQueryable<MusicRelease>, IOrderedQueryable<MusicRelease>>>(),
                    It.IsAny<string>()))
                .ReturnsAsync(new PagedResult<MusicRelease>
                {
                    Items = releases, Page = 1, PageSize = 10, TotalCount = 2, TotalPages = 1
                });

            _mockMapper.Setup(m => m.MapToSummaryDtosAsync(It.IsAny<IEnumerable<MusicRelease>>()))
                .ReturnsAsync((IEnumerable<MusicRelease> mr) =>
                    mr.Select(r => new MusicReleaseSummaryDto { Id = r.Id, Title = r.Title }).ToList());

            var query = new GetMusicReleasesQuery(
                new MusicReleaseQueryParameters
                {
                    Pagination = new PaginationParameters { PageNumber = 1, PageSize = 10 }
                });

            // Act
            var result = await _handler.HandleAsync(query);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count());
        }

        // ── No user context ─────────────────────────────────────────────────────

        /// <summary>Handler short-circuits and returns empty when there is no authenticated user.</summary>
        [Fact]
        public async Task HandleAsync_WithNoUserContext_ReturnsEmpty()
        {
            // Arrange
            _mockUserContext.Setup(u => u.GetActingUserId()).Returns((Guid?)null);

            var query = new GetMusicReleasesQuery(
                new MusicReleaseQueryParameters
                {
                    Pagination = new PaginationParameters { PageNumber = 1, PageSize = 10 }
                });

            // Act
            var result = await _handler.HandleAsync(query);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Items);
            Assert.Equal(0, result.TotalCount);
            _mockRepo.Verify(r => r.GetPagedAsync(
                It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<Expression<Func<MusicRelease, bool>>>(),
                It.IsAny<Func<IQueryable<MusicRelease>, IOrderedQueryable<MusicRelease>>>(),
                It.IsAny<string>()), Times.Never);
        }

        // ── Artist sort path ────────────────────────────────────────────────────

        /// <summary>Handler uses the in-memory artist-sort path when SortBy=artist.</summary>
        [Fact]
        public async Task HandleAsync_WithArtistSort_ReturnsResultsSortedByArtistName()
        {
            // Arrange
            var releases = new List<MusicRelease>
            {
                new() { Id = 1, Title = "Z Album", UserId = DefaultUserId },
                new() { Id = 2, Title = "A Album", UserId = DefaultUserId }
            };

            _mockRepo.Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<MusicRelease, bool>>>(),
                    It.IsAny<Func<IQueryable<MusicRelease>, IOrderedQueryable<MusicRelease>>?>(),
                    It.IsAny<string>()))
                .ReturnsAsync(releases);

            _mockMapper.Setup(m => m.MapToSummaryDtosAsync(It.IsAny<IEnumerable<MusicRelease>>()))
                .ReturnsAsync((IEnumerable<MusicRelease> mr) =>
                    mr.Select(r => new MusicReleaseSummaryDto
                    {
                        Id = r.Id,
                        Title = r.Title,
                        ArtistNames = new List<string> { r.Title.Substring(0, 1) } // use first letter as artist name for sorting
                    }).ToList());

            var query = new GetMusicReleasesQuery(
                new MusicReleaseQueryParameters
                {
                    SortBy = "artist",
                    SortOrder = "asc",
                    Pagination = new PaginationParameters { PageNumber = 1, PageSize = 10 }
                });

            // Act
            var result = await _handler.HandleAsync(query);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            var items = result.Items.ToList();
            Assert.Equal(2, items[0].Id); // "A Album" → artist "A" comes first
            Assert.Equal(1, items[1].Id); // "Z Album" → artist "Z" comes second
        }

        /// <summary>Descending artist sort reverses the order.</summary>
        [Fact]
        public async Task HandleAsync_WithArtistSortDescending_ReturnsCorrectOrder()
        {
            // Arrange
            var releases = new List<MusicRelease>
            {
                new() { Id = 1, Title = "A Album", UserId = DefaultUserId },
                new() { Id = 2, Title = "Z Album", UserId = DefaultUserId }
            };

            _mockRepo.Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<MusicRelease, bool>>>(),
                    It.IsAny<Func<IQueryable<MusicRelease>, IOrderedQueryable<MusicRelease>>?>(),
                    It.IsAny<string>()))
                .ReturnsAsync(releases);

            _mockMapper.Setup(m => m.MapToSummaryDtosAsync(It.IsAny<IEnumerable<MusicRelease>>()))
                .ReturnsAsync((IEnumerable<MusicRelease> mr) =>
                    mr.Select(r => new MusicReleaseSummaryDto
                    {
                        Id = r.Id,
                        Title = r.Title,
                        ArtistNames = new List<string> { r.Title.Substring(0, 1) }
                    }).ToList());

            var query = new GetMusicReleasesQuery(
                new MusicReleaseQueryParameters
                {
                    SortBy = "artist",
                    SortOrder = "desc",
                    Pagination = new PaginationParameters { PageNumber = 1, PageSize = 10 }
                });

            // Act
            var result = await _handler.HandleAsync(query);

            // Assert
            var items = result.Items.ToList();
            Assert.Equal(2, items[0].Id); // "Z" descending → first
            Assert.Equal(1, items[1].Id); // "A" descending → last
        }

        // ── Pagination ──────────────────────────────────────────────────────────

        /// <summary>Pagination parameters are forwarded to the repository.</summary>
        [Fact]
        public async Task HandleAsync_WithPagination_ForwardsPageParamsToRepo()
        {
            // Arrange
            int capturedPage = 0, capturedSize = 0;

            _mockRepo.Setup(r => r.GetPagedAsync(
                    It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<Expression<Func<MusicRelease, bool>>>(),
                    It.IsAny<Func<IQueryable<MusicRelease>, IOrderedQueryable<MusicRelease>>>(),
                    It.IsAny<string>()))
                .Callback<int, int,
                    Expression<Func<MusicRelease, bool>>,
                    Func<IQueryable<MusicRelease>, IOrderedQueryable<MusicRelease>>,
                    string>((page, size, _, __, ___) => { capturedPage = page; capturedSize = size; })
                .ReturnsAsync(new PagedResult<MusicRelease>
                {
                    Items = new List<MusicRelease>(), Page = 3, PageSize = 5, TotalCount = 0, TotalPages = 0
                });

            _mockMapper.Setup(m => m.MapToSummaryDtosAsync(It.IsAny<IEnumerable<MusicRelease>>()))
                .ReturnsAsync(new List<MusicReleaseSummaryDto>());

            var query = new GetMusicReleasesQuery(
                new MusicReleaseQueryParameters
                {
                    Pagination = new PaginationParameters { PageNumber = 3, PageSize = 5 }
                });

            // Act
            await _handler.HandleAsync(query);

            // Assert
            Assert.Equal(3, capturedPage);
            Assert.Equal(5, capturedSize);
        }
    }
}
