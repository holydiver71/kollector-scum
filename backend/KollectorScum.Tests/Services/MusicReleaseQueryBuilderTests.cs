using System.Linq;
using KollectorScum.Api.Models;
using KollectorScum.Api.Services;
using KollectorScum.Api.DTOs;
using Xunit;

namespace KollectorScum.Tests.Services
{
    public class MusicReleaseQueryBuilderTests
    {
        [Fact]
        public void ApplyFilters_WithYearFromAndYearTo_FiltersByReleaseYearRange()
        {
            // Arrange
            var releases = new[]
            {
                new MusicRelease { Id = 1, Title = "Old Album", ReleaseYear = new System.DateTime(1975,1,1,0,0,0,System.DateTimeKind.Utc) },
                new MusicRelease { Id = 2, Title = "Target Album", ReleaseYear = new System.DateTime(1976,1,1,0,0,0,System.DateTimeKind.Utc) },
                new MusicRelease { Id = 3, Title = "New Album", ReleaseYear = new System.DateTime(1980,1,1,0,0,0,System.DateTimeKind.Utc) }
            };

            var queryable = releases.AsQueryable();

            var parameters = new MusicReleaseQueryParameters
            {
                YearFrom = 1976,
                YearTo = 1980
            };

            var builder = new MusicReleaseQueryBuilder(queryable, parameters);

            // Act
            builder.ApplyFilters(null);
            var result = builder.Build().ToList();

            // Assert
            Assert.Contains(result, r => r.Id == 2);
            Assert.Contains(result, r => r.Id == 3);
            Assert.DoesNotContain(result, r => r.Id == 1);
        }
    }
}
