using KollectorScum.Api.Data;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using KollectorScum.Api.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace KollectorScum.Tests.Repositories
{
    /// <summary>
    /// Unit tests for MusicReleaseRepository domain-specific query methods.
    /// Uses EF Core InMemory database and a mocked IUserContext.
    /// </summary>
    public class MusicReleaseRepositoryTests
    {
        private static readonly Guid UserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid OtherUserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        private static KollectorScumDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<KollectorScumDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new KollectorScumDbContext(options);
        }

        private static MusicReleaseRepository CreateRepo(KollectorScumDbContext context, Guid? actingUserId = null)
        {
            var mockUserContext = new Mock<IUserContext>();
            mockUserContext.Setup(u => u.GetActingUserId()).Returns(actingUserId ?? UserId);
            return new MusicReleaseRepository(context, mockUserContext.Object);
        }

        private static MusicRelease Release(Guid userId, string title, string? artists = null, string? genres = null,
            int? labelId = null, int? countryId = null, int? formatId = null)
            => new MusicRelease
            {
                UserId = userId,
                Title = title,
                Artists = artists,
                Genres = genres,
                LabelId = labelId,
                CountryId = countryId,
                FormatId = formatId,
                DateAdded = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
            };

        // ──────────────────────────────────────────────────────────────────────
        // GetPaginatedAsync
        // ──────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetPaginatedAsync_ReturnsOnlyCurrentUserReleases()
        {
            using var ctx = CreateContext();
            ctx.MusicReleases.AddRange(
                Release(UserId, "My Album"),
                Release(OtherUserId, "Their Album"));
            await ctx.SaveChangesAsync();

            var results = await CreateRepo(ctx).GetPaginatedAsync(1, 10);

            Assert.Single(results);
            Assert.Equal("My Album", results.First().Title);
        }

        [Fact]
        public async Task GetPaginatedAsync_ReturnsEmpty_WhenNoUserContext()
        {
            using var ctx = CreateContext();
            ctx.MusicReleases.Add(Release(UserId, "Album"));
            await ctx.SaveChangesAsync();

            var mockUserContext = new Mock<IUserContext>();
            mockUserContext.Setup(u => u.GetActingUserId()).Returns((Guid?)null);
            var repo = new MusicReleaseRepository(ctx, mockUserContext.Object);

            var results = await repo.GetPaginatedAsync(1, 10);

            Assert.Empty(results);
        }

        [Fact]
        public async Task GetPaginatedAsync_WithSearch_FiltersByTitleCaseInsensitive()
        {
            using var ctx = CreateContext();
            ctx.MusicReleases.AddRange(
                Release(UserId, "Dark Side of the Moon"),
                Release(UserId, "The Wall"),
                Release(UserId, "Animals"));
            await ctx.SaveChangesAsync();

            var results = await CreateRepo(ctx).GetPaginatedAsync(1, 10, search: "the");

            Assert.Equal(2, results.Count());
        }

        [Fact]
        public async Task GetPaginatedAsync_WithArtistId_MatchesSingleElementArray()
        {
            using var ctx = CreateContext();
            ctx.MusicReleases.AddRange(
                Release(UserId, "Solo", artists: "[42]"),
                Release(UserId, "Other", artists: "[99]"));
            await ctx.SaveChangesAsync();

            var results = await CreateRepo(ctx).GetPaginatedAsync(1, 10, artistId: 42);

            Assert.Single(results);
            Assert.Equal("Solo", results.First().Title);
        }

        [Fact]
        public async Task GetPaginatedAsync_WithArtistId_MatchesFirstPosition()
        {
            using var ctx = CreateContext();
            ctx.MusicReleases.Add(Release(UserId, "Start", artists: "[42,99]"));
            await ctx.SaveChangesAsync();

            var results = await CreateRepo(ctx).GetPaginatedAsync(1, 10, artistId: 42);

            Assert.Single(results);
        }

        [Fact]
        public async Task GetPaginatedAsync_WithArtistId_MatchesLastPosition()
        {
            using var ctx = CreateContext();
            ctx.MusicReleases.Add(Release(UserId, "End", artists: "[1,42]"));
            await ctx.SaveChangesAsync();

            var results = await CreateRepo(ctx).GetPaginatedAsync(1, 10, artistId: 42);

            Assert.Single(results);
        }

        [Fact]
        public async Task GetPaginatedAsync_WithArtistId_MatchesMiddlePosition()
        {
            using var ctx = CreateContext();
            ctx.MusicReleases.Add(Release(UserId, "Middle", artists: "[1,42,99]"));
            await ctx.SaveChangesAsync();

            var results = await CreateRepo(ctx).GetPaginatedAsync(1, 10, artistId: 42);

            Assert.Single(results);
        }

        [Fact]
        public async Task GetPaginatedAsync_WithArtistId_DoesNotMatchPartialId()
        {
            using var ctx = CreateContext();
            // "142" and "421" should not match artistId: 42
            ctx.MusicReleases.AddRange(
                Release(UserId, "False positive A", artists: "[142]"),
                Release(UserId, "False positive B", artists: "[421]"));
            await ctx.SaveChangesAsync();

            var results = await CreateRepo(ctx).GetPaginatedAsync(1, 10, artistId: 42);

            Assert.Empty(results);
        }

        [Fact]
        public async Task GetPaginatedAsync_WithLabelId_FiltersCorrectly()
        {
            using var ctx = CreateContext();
            ctx.MusicReleases.AddRange(
                Release(UserId, "Label Match", labelId: 5),
                Release(UserId, "No Label"));
            await ctx.SaveChangesAsync();

            var results = await CreateRepo(ctx).GetPaginatedAsync(1, 10, labelId: 5);

            Assert.Single(results);
            Assert.Equal("Label Match", results.First().Title);
        }

        [Fact]
        public async Task GetPaginatedAsync_PaginationRespectsPageAndPageSize()
        {
            using var ctx = CreateContext();
            for (int i = 0; i < 5; i++)
                ctx.MusicReleases.Add(Release(UserId, $"Album {i:D2}"));
            await ctx.SaveChangesAsync();

            var page1 = await CreateRepo(ctx).GetPaginatedAsync(1, 2);
            var page2 = await CreateRepo(ctx).GetPaginatedAsync(2, 2);
            var page3 = await CreateRepo(ctx).GetPaginatedAsync(3, 2);

            Assert.Equal(2, page1.Count());
            Assert.Equal(2, page2.Count());
            Assert.Single(page3);
        }

        // ──────────────────────────────────────────────────────────────────────
        // GetTotalCountAsync
        // ──────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetTotalCountAsync_ReturnsCorrectCount()
        {
            using var ctx = CreateContext();
            ctx.MusicReleases.AddRange(
                Release(UserId, "A"),
                Release(UserId, "B"),
                Release(OtherUserId, "C"));
            await ctx.SaveChangesAsync();

            var count = await CreateRepo(ctx).GetTotalCountAsync();

            Assert.Equal(2, count);
        }

        [Fact]
        public async Task GetTotalCountAsync_ReturnsZero_WhenNoUserContext()
        {
            using var ctx = CreateContext();
            ctx.MusicReleases.Add(Release(UserId, "Album"));
            await ctx.SaveChangesAsync();

            var mockUserContext = new Mock<IUserContext>();
            mockUserContext.Setup(u => u.GetActingUserId()).Returns((Guid?)null);
            var repo = new MusicReleaseRepository(ctx, mockUserContext.Object);

            Assert.Equal(0, await repo.GetTotalCountAsync());
        }

        [Fact]
        public async Task GetTotalCountAsync_WithSearchFilter_CountsMatchingOnly()
        {
            using var ctx = CreateContext();
            ctx.MusicReleases.AddRange(
                Release(UserId, "Dark Side of the Moon"),
                Release(UserId, "The Wall"),
                Release(UserId, "Animals"));
            await ctx.SaveChangesAsync();

            var count = await CreateRepo(ctx).GetTotalCountAsync(search: "dark");

            Assert.Equal(1, count);
        }

        // ──────────────────────────────────────────────────────────────────────
        // GetWithDetailsAsync
        // ──────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetWithDetailsAsync_ReturnsRelease_ForOwner()
        {
            using var ctx = CreateContext();
            var release = Release(UserId, "Owned Album");
            ctx.MusicReleases.Add(release);
            await ctx.SaveChangesAsync();

            var result = await CreateRepo(ctx).GetWithDetailsAsync(release.Id);

            Assert.NotNull(result);
            Assert.Equal("Owned Album", result!.Title);
        }

        [Fact]
        public async Task GetWithDetailsAsync_ReturnsNull_WhenNotOwner()
        {
            using var ctx = CreateContext();
            var release = Release(OtherUserId, "Not Mine");
            ctx.MusicReleases.Add(release);
            await ctx.SaveChangesAsync();

            var result = await CreateRepo(ctx).GetWithDetailsAsync(release.Id);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetWithDetailsAsync_ReturnsNull_WhenNoUserContext()
        {
            using var ctx = CreateContext();
            var release = Release(UserId, "Album");
            ctx.MusicReleases.Add(release);
            await ctx.SaveChangesAsync();

            var mockUserContext = new Mock<IUserContext>();
            mockUserContext.Setup(u => u.GetActingUserId()).Returns((Guid?)null);
            var repo = new MusicReleaseRepository(ctx, mockUserContext.Object);

            Assert.Null(await repo.GetWithDetailsAsync(release.Id));
        }

        // ──────────────────────────────────────────────────────────────────────
        // SearchAsync
        // ──────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task SearchAsync_ReturnsTitleMatchesForCurrentUser()
        {
            using var ctx = CreateContext();
            ctx.MusicReleases.AddRange(
                Release(UserId, "Dark Side of the Moon"),
                Release(UserId, "Darklands"),
                Release(UserId, "The Wall"),
                Release(OtherUserId, "Dark Territory"));
            await ctx.SaveChangesAsync();

            var results = await CreateRepo(ctx).SearchAsync("dark");

            Assert.Equal(2, results.Count());
            Assert.All(results, r => Assert.Equal(UserId, r.UserId));
        }

        [Fact]
        public async Task SearchAsync_ReturnsEmpty_WhenNoUserContext()
        {
            using var ctx = CreateContext();
            ctx.MusicReleases.Add(Release(UserId, "Album"));
            await ctx.SaveChangesAsync();

            var mockUserContext = new Mock<IUserContext>();
            mockUserContext.Setup(u => u.GetActingUserId()).Returns((Guid?)null);
            var repo = new MusicReleaseRepository(ctx, mockUserContext.Object);

            Assert.Empty(await repo.SearchAsync("album"));
        }

        [Fact]
        public async Task SearchAsync_IsCaseInsensitive()
        {
            using var ctx = CreateContext();
            ctx.MusicReleases.Add(Release(UserId, "DARK SIDE"));
            await ctx.SaveChangesAsync();

            var results = await CreateRepo(ctx).SearchAsync("dark side");

            Assert.Single(results);
        }
    }
}
