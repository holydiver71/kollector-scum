using System.Linq.Expressions;
using KollectorScum.Api.DTOs;
using KollectorScum.Api.Models;
using KollectorScum.Api.Services;
using Xunit;

namespace KollectorScum.Tests.Services
{
    /// <summary>
    /// Unit tests for MusicReleaseFilterBuilder.
    /// Validates each filter clause in isolation and their combination.
    /// </summary>
    public class MusicReleaseFilterBuilderTests
    {
        private static readonly Guid DefaultUserId = Guid.Parse("12337b39-c346-449c-b269-33b2e820d74f");
        private static readonly Guid OtherUserId = Guid.Parse("99999999-9999-9999-9999-999999999999");

        private static MusicReleaseQueryParameters DefaultParams() => new()
        {
            Pagination = new PaginationParameters { PageNumber = 1, PageSize = 10 }
        };

        private static Func<MusicRelease, bool> Compile(Expression<Func<MusicRelease, bool>>? expr)
        {
            Assert.NotNull(expr);
            return expr!.Compile();
        }

        // ── User scope ────────────────────────────────────────────────────────────

        /// <summary>Filter always restricts to the requesting user's releases.</summary>
        [Fact]
        public void Build_WithUserIdOnly_FiltersByUserId()
        {
            var filter = Compile(MusicReleaseFilterBuilder.Build(DefaultParams(), DefaultUserId, null));

            Assert.True(filter(new MusicRelease { UserId = DefaultUserId }));
            Assert.False(filter(new MusicRelease { UserId = OtherUserId }));
        }

        // ── Search ───────────────────────────────────────────────────────────────

        /// <summary>Title search is case-insensitive substring match.</summary>
        [Fact]
        public void Build_WithSearchFilter_MatchesTitleCaseInsensitive()
        {
            var p = DefaultParams();
            p.Search = "dark";

            var filter = Compile(MusicReleaseFilterBuilder.Build(p, DefaultUserId, null));

            Assert.True(filter(new MusicRelease { UserId = DefaultUserId, Title = "Dark Side of the Moon" }));
            Assert.True(filter(new MusicRelease { UserId = DefaultUserId, Title = "In The Dark" }));
            Assert.False(filter(new MusicRelease { UserId = DefaultUserId, Title = "Abbey Road" }));
        }

        // ── Specific IDs ─────────────────────────────────────────────────────────

        /// <summary>When Ids CSV is provided only those releases are returned.</summary>
        [Fact]
        public void Build_WithIdsList_FiltersToSpecifiedIds()
        {
            var p = DefaultParams();
            p.Ids = "1,3";

            var filter = Compile(MusicReleaseFilterBuilder.Build(p, DefaultUserId, null));

            Assert.True(filter(new MusicRelease { Id = 1, UserId = DefaultUserId }));
            Assert.True(filter(new MusicRelease { Id = 3, UserId = DefaultUserId }));
            Assert.False(filter(new MusicRelease { Id = 2, UserId = DefaultUserId }));
            Assert.False(filter(new MusicRelease { Id = 99, UserId = DefaultUserId }));
        }

        // ── JSON array contains ───────────────────────────────────────────────────

        /// <summary>Single-element JSON array [id].</summary>
        [Fact]
        public void BuildJsonContainsClause_MatchesSingleElement()
        {
            var param = Expression.Parameter(typeof(MusicRelease), "mr");
            var expr = MusicReleaseFilterBuilder.BuildJsonContainsClause(param, nameof(MusicRelease.Artists), 5);
            var func = Expression.Lambda<Func<MusicRelease, bool>>(expr, param).Compile();

            Assert.True(func(new MusicRelease { Artists = "[5]" }));
        }

        /// <summary>First element in a multi-element array [id,...].</summary>
        [Fact]
        public void BuildJsonContainsClause_MatchesFirstElement()
        {
            var param = Expression.Parameter(typeof(MusicRelease), "mr");
            var expr = MusicReleaseFilterBuilder.BuildJsonContainsClause(param, nameof(MusicRelease.Artists), 5);
            var func = Expression.Lambda<Func<MusicRelease, bool>>(expr, param).Compile();

            Assert.True(func(new MusicRelease { Artists = "[5,10,15]" }));
        }

        /// <summary>Last element in a multi-element array [...,id].</summary>
        [Fact]
        public void BuildJsonContainsClause_MatchesLastElement()
        {
            var param = Expression.Parameter(typeof(MusicRelease), "mr");
            var expr = MusicReleaseFilterBuilder.BuildJsonContainsClause(param, nameof(MusicRelease.Artists), 15);
            var func = Expression.Lambda<Func<MusicRelease, bool>>(expr, param).Compile();

            Assert.True(func(new MusicRelease { Artists = "[5,10,15]" }));
        }

        /// <summary>Middle element in a multi-element array [..id,..].</summary>
        [Fact]
        public void BuildJsonContainsClause_MatchesMiddleElement()
        {
            var param = Expression.Parameter(typeof(MusicRelease), "mr");
            var expr = MusicReleaseFilterBuilder.BuildJsonContainsClause(param, nameof(MusicRelease.Artists), 10);
            var func = Expression.Lambda<Func<MusicRelease, bool>>(expr, param).Compile();

            Assert.True(func(new MusicRelease { Artists = "[5,10,15]" }));
        }

        /// <summary>Does not match an ID that is not in the array.</summary>
        [Fact]
        public void BuildJsonContainsClause_DoesNotMatchAbsentId()
        {
            var param = Expression.Parameter(typeof(MusicRelease), "mr");
            var expr = MusicReleaseFilterBuilder.BuildJsonContainsClause(param, nameof(MusicRelease.Artists), 99);
            var func = Expression.Lambda<Func<MusicRelease, bool>>(expr, param).Compile();

            Assert.False(func(new MusicRelease { Artists = "[5,10,15]" }));
        }

        /// <summary>Does not match a null JSON field.</summary>
        [Fact]
        public void BuildJsonContainsClause_DoesNotMatchNullField()
        {
            var param = Expression.Parameter(typeof(MusicRelease), "mr");
            var expr = MusicReleaseFilterBuilder.BuildJsonContainsClause(param, nameof(MusicRelease.Artists), 5);
            var func = Expression.Lambda<Func<MusicRelease, bool>>(expr, param).Compile();

            Assert.False(func(new MusicRelease { Artists = null }));
        }

        // ── ArtistId / GenreId ────────────────────────────────────────────────────

        /// <summary>ArtistId filter uses JSON array contains logic.</summary>
        [Fact]
        public void Build_WithArtistId_IncludesJsonArrayFilter()
        {
            var p = DefaultParams();
            p.ArtistId = 7;

            var filter = Compile(MusicReleaseFilterBuilder.Build(p, DefaultUserId, null));

            Assert.True(filter(new MusicRelease { UserId = DefaultUserId, Artists = "[7]" }));
            Assert.True(filter(new MusicRelease { UserId = DefaultUserId, Artists = "[7,8]" }));
            Assert.False(filter(new MusicRelease { UserId = DefaultUserId, Artists = "[8]" }));
            Assert.False(filter(new MusicRelease { UserId = DefaultUserId, Artists = null }));
        }

        /// <summary>GenreId filter uses JSON array contains logic.</summary>
        [Fact]
        public void Build_WithGenreId_IncludesJsonArrayFilter()
        {
            var p = DefaultParams();
            p.GenreId = 3;

            var filter = Compile(MusicReleaseFilterBuilder.Build(p, DefaultUserId, null));

            Assert.True(filter(new MusicRelease { UserId = DefaultUserId, Genres = "[3,5]" }));
            Assert.False(filter(new MusicRelease { UserId = DefaultUserId, Genres = "[5]" }));
        }

        // ── Scalar filters ────────────────────────────────────────────────────────

        /// <summary>LabelId scalar equality filter.</summary>
        [Fact]
        public void Build_WithLabelId_FiltersToThatLabel()
        {
            var p = DefaultParams();
            p.LabelId = 10;

            var filter = Compile(MusicReleaseFilterBuilder.Build(p, DefaultUserId, null));

            Assert.True(filter(new MusicRelease { UserId = DefaultUserId, LabelId = 10 }));
            Assert.False(filter(new MusicRelease { UserId = DefaultUserId, LabelId = 11 }));
            Assert.False(filter(new MusicRelease { UserId = DefaultUserId, LabelId = null }));
        }

        /// <summary>CountryId scalar equality filter.</summary>
        [Fact]
        public void Build_WithCountryId_FiltersToThatCountry()
        {
            var p = DefaultParams();
            p.CountryId = 4;

            var filter = Compile(MusicReleaseFilterBuilder.Build(p, DefaultUserId, null));

            Assert.True(filter(new MusicRelease { UserId = DefaultUserId, CountryId = 4 }));
            Assert.False(filter(new MusicRelease { UserId = DefaultUserId, CountryId = 5 }));
        }

        /// <summary>FormatId scalar equality filter.</summary>
        [Fact]
        public void Build_WithFormatId_FiltersToThatFormat()
        {
            var p = DefaultParams();
            p.FormatId = 2;

            var filter = Compile(MusicReleaseFilterBuilder.Build(p, DefaultUserId, null));

            Assert.True(filter(new MusicRelease { UserId = DefaultUserId, FormatId = 2 }));
            Assert.False(filter(new MusicRelease { UserId = DefaultUserId, FormatId = 3 }));
        }

        // ── Live flag ─────────────────────────────────────────────────────────────

        /// <summary>Live=true filter excludes non-live releases.</summary>
        [Fact]
        public void Build_WithLiveTrue_FiltersToLiveReleases()
        {
            var p = DefaultParams();
            p.Live = true;

            var filter = Compile(MusicReleaseFilterBuilder.Build(p, DefaultUserId, null));

            Assert.True(filter(new MusicRelease { UserId = DefaultUserId, Live = true }));
            Assert.False(filter(new MusicRelease { UserId = DefaultUserId, Live = false }));
        }

        // ── Year range ────────────────────────────────────────────────────────────

        /// <summary>YearFrom excludes releases before the lower bound.</summary>
        [Fact]
        public void Build_WithYearFrom_ExcludesReleasesBefore()
        {
            var p = DefaultParams();
            p.YearFrom = 2000;

            var filter = Compile(MusicReleaseFilterBuilder.Build(p, DefaultUserId, null));

            Assert.True(filter(new MusicRelease { UserId = DefaultUserId, ReleaseYear = new DateTime(2000, 6, 1, 0, 0, 0, DateTimeKind.Utc) }));
            Assert.True(filter(new MusicRelease { UserId = DefaultUserId, ReleaseYear = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc) }));
            Assert.False(filter(new MusicRelease { UserId = DefaultUserId, ReleaseYear = new DateTime(1999, 12, 31, 0, 0, 0, DateTimeKind.Utc) }));
            Assert.False(filter(new MusicRelease { UserId = DefaultUserId, ReleaseYear = null }));
        }

        /// <summary>YearTo excludes releases after the upper bound.</summary>
        [Fact]
        public void Build_WithYearTo_ExcludesReleasesAfter()
        {
            var p = DefaultParams();
            p.YearTo = 2010;

            var filter = Compile(MusicReleaseFilterBuilder.Build(p, DefaultUserId, null));

            Assert.True(filter(new MusicRelease { UserId = DefaultUserId, ReleaseYear = new DateTime(2010, 6, 1, 0, 0, 0, DateTimeKind.Utc) }));
            Assert.True(filter(new MusicRelease { UserId = DefaultUserId, ReleaseYear = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc) }));
            Assert.False(filter(new MusicRelease { UserId = DefaultUserId, ReleaseYear = new DateTime(2011, 1, 1, 0, 0, 0, DateTimeKind.Utc) }));
            Assert.False(filter(new MusicRelease { UserId = DefaultUserId, ReleaseYear = null }));
        }

        // ── Kollection genre OR ───────────────────────────────────────────────────

        /// <summary>Multiple kollection genre IDs are ORed together.</summary>
        [Fact]
        public void Build_WithKollectionGenreIds_MatchesAnyGenre()
        {
            var genreIds = new List<int> { 1, 2, 3 };

            var filter = Compile(MusicReleaseFilterBuilder.Build(DefaultParams(), DefaultUserId, genreIds));

            Assert.True(filter(new MusicRelease { UserId = DefaultUserId, Genres = "[1]" }));
            Assert.True(filter(new MusicRelease { UserId = DefaultUserId, Genres = "[2,5]" }));
            Assert.True(filter(new MusicRelease { UserId = DefaultUserId, Genres = "[3]" }));
            Assert.False(filter(new MusicRelease { UserId = DefaultUserId, Genres = "[4,5]" }));
            Assert.False(filter(new MusicRelease { UserId = DefaultUserId, Genres = null }));
        }

        /// <summary>Empty kollection genre list does not add any genre clause.</summary>
        [Fact]
        public void Build_WithEmptyKollectionGenreIds_AddsNoGenreClause()
        {
            var filter = Compile(MusicReleaseFilterBuilder.Build(DefaultParams(), DefaultUserId, new List<int>()));

            // Any release belonging to the user should pass (no genre restriction)
            Assert.True(filter(new MusicRelease { UserId = DefaultUserId, Genres = null }));
            Assert.True(filter(new MusicRelease { UserId = DefaultUserId, Genres = "[99]" }));
        }

        // ── Combined filters ──────────────────────────────────────────────────────

        /// <summary>Multiple active filters are ANDed: a release must satisfy all of them.</summary>
        [Fact]
        public void Build_WithMultipleFilters_RequiresAllConditions()
        {
            var p = DefaultParams();
            p.Search = "dark";
            p.GenreId = 5;
            p.YearFrom = 1970;
            p.YearTo = 1980;

            var filter = Compile(MusicReleaseFilterBuilder.Build(p, DefaultUserId, null));

            var matching = new MusicRelease
            {
                UserId = DefaultUserId,
                Title = "Dark Side of the Moon",
                Genres = "[5,8]",
                ReleaseYear = new DateTime(1973, 3, 1, 0, 0, 0, DateTimeKind.Utc)
            };
            var wrongTitle = new MusicRelease { UserId = DefaultUserId, Title = "Abbey Road", Genres = "[5]", ReleaseYear = new DateTime(1973, 1, 1, 0, 0, 0, DateTimeKind.Utc) };
            var wrongYear = new MusicRelease { UserId = DefaultUserId, Title = "Dark Album", Genres = "[5]", ReleaseYear = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc) };
            var wrongUser = new MusicRelease { UserId = OtherUserId, Title = "Dark Side of the Moon", Genres = "[5]", ReleaseYear = new DateTime(1973, 1, 1, 0, 0, 0, DateTimeKind.Utc) };

            Assert.True(filter(matching));
            Assert.False(filter(wrongTitle));
            Assert.False(filter(wrongYear));
            Assert.False(filter(wrongUser));
        }
    }
}
