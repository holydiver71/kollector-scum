using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KollectorScum.Api.Data;
using KollectorScum.Api.DTOs;
using KollectorScum.Api.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace KollectorScum.Tests.Integration
{
    /// <summary>
    /// Relational integration tests for collection statistics endpoint.
    /// </summary>
    public class CollectionStatisticsIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
    {
        private static readonly Guid TestUserId = Guid.Parse("12337b39-c346-449c-b269-33b2e820d74f");

        private readonly WebApplicationFactory<Program> _factory;
        private readonly SqliteConnection _connection;
        private readonly JsonSerializerOptions _jsonOptions;

        public CollectionStatisticsIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Test");
                builder.ConfigureTestServices(services =>
                {
                    var descriptorsToRemove = services
                        .Where(d =>
                            d.ServiceType == typeof(KollectorScumDbContext) ||
                            d.ServiceType == typeof(DbContextOptions<KollectorScumDbContext>) ||
                            (d.ServiceType.IsGenericType &&
                             (d.ServiceType.GetGenericTypeDefinition() == typeof(IConfigureOptions<>) ||
                              d.ServiceType.GetGenericTypeDefinition() == typeof(IPostConfigureOptions<>)) &&
                             d.ServiceType.GetGenericArguments()[0] == typeof(DbContextOptions<KollectorScumDbContext>)))
                        .ToList();

                    foreach (var descriptor in descriptorsToRemove)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<KollectorScumDbContext>(options =>
                        options.UseSqlite(_connection));

                    services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = "Test";
                        options.DefaultChallengeScheme = "Test";
                    })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });
                });
            });

            SeedDatabase();

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        public void Dispose()
        {
            _factory.Dispose();
            _connection.Dispose();
        }

        [Fact]
        public async Task GetCollectionStatistics_ReturnsExpectedAggregates_ForAuthenticatedUser()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/api/musicreleases/statistics");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<CollectionStatisticsDto>(_jsonOptions);

            Assert.NotNull(result);
            Assert.Equal(3, result.TotalReleases);
            Assert.Equal(2, result.TotalArtists);
            Assert.Equal(2, result.TotalGenres);
            Assert.Equal(2, result.TotalLabels);

            Assert.Equal(2, result.ReleasesByYear.Count);
            Assert.Equal(2, result.ReleasesByYear.First(y => y.Year == 2020).Count);
            Assert.Equal(1, result.ReleasesByYear.First(y => y.Year == 2021).Count);

            Assert.Equal(2, result.ReleasesByFormat.Count);
            Assert.Equal(2, result.ReleasesByFormat.First(f => f.FormatName == "CD").Count);
            Assert.Equal(1, result.ReleasesByFormat.First(f => f.FormatName == "Vinyl").Count);

            Assert.Equal(2, result.ReleasesByCountry.Count);
            Assert.Equal(2, result.ReleasesByCountry.First(c => c.CountryName == "UK").Count);
            Assert.Equal(1, result.ReleasesByCountry.First(c => c.CountryName == "US").Count);

            Assert.Equal(2, result.ReleasesByGenre.Count);
            Assert.Equal(2, result.ReleasesByGenre.First(g => g.GenreName == "Rock").Count);
            Assert.Equal(2, result.ReleasesByGenre.First(g => g.GenreName == "Metal").Count);

            Assert.Equal(22.50m, result.TotalValue);
            Assert.Equal(11.25m, result.AveragePrice);
            Assert.NotNull(result.MostExpensiveRelease);
            Assert.Equal("Owned Album 2", result.MostExpensiveRelease!.Title);

            Assert.Equal(3, result.RecentlyAdded.Count);
            Assert.Equal("Owned Album 3", result.RecentlyAdded[0].Title);
            Assert.DoesNotContain(result.RecentlyAdded, release => release.Title == "Other User Album");
        }

        private void SeedDatabase()
        {
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<KollectorScumDbContext>();
            dbContext.Database.EnsureCreated();

            if (!dbContext.ApplicationUsers.Any(u => u.Id == TestUserId))
            {
                dbContext.ApplicationUsers.Add(new ApplicationUser
                {
                    Id = TestUserId,
                    GoogleSub = "test-google-sub",
                    Email = "testuser@example.com",
                    DisplayName = "TestUser",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsAdmin = false
                });
            }

            var otherUserId = Guid.NewGuid();
            dbContext.ApplicationUsers.Add(new ApplicationUser
            {
                Id = otherUserId,
                GoogleSub = "other-google-sub",
                Email = "other@example.com",
                DisplayName = "OtherUser",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsAdmin = false
            });

            dbContext.Formats.AddRange(
                new Format { Id = 1, UserId = TestUserId, Name = "CD" },
                new Format { Id = 2, UserId = TestUserId, Name = "Vinyl" });

            dbContext.Countries.AddRange(
                new Country { Id = 1, UserId = TestUserId, Name = "UK" },
                new Country { Id = 2, UserId = TestUserId, Name = "US" });

            dbContext.Genres.AddRange(
                new Genre { Id = 1, UserId = TestUserId, Name = "Rock" },
                new Genre { Id = 2, UserId = TestUserId, Name = "Metal" });

            dbContext.Labels.AddRange(
                new Label { Id = 1, UserId = TestUserId, Name = "Label A" },
                new Label { Id = 2, UserId = TestUserId, Name = "Label B" });

            dbContext.MusicReleases.AddRange(
                new MusicRelease
                {
                    Id = 1,
                    UserId = TestUserId,
                    Title = "Owned Album 1",
                    ReleaseYear = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    Artists = "[1]",
                    Genres = "[1]",
                    LabelId = 1,
                    FormatId = 1,
                    CountryId = 1,
                    PurchaseInfo = "{\"Price\":10.00}",
                    DateAdded = new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc),
                    LastModified = new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc)
                },
                new MusicRelease
                {
                    Id = 2,
                    UserId = TestUserId,
                    Title = "Owned Album 2",
                    ReleaseYear = new DateTime(2020, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                    Artists = "[2]",
                    Genres = "[2]",
                    LabelId = 2,
                    FormatId = 1,
                    CountryId = 1,
                    PurchaseInfo = "{\"Price\":12.50}",
                    DateAdded = new DateTime(2026, 3, 11, 0, 0, 0, DateTimeKind.Utc),
                    LastModified = new DateTime(2026, 3, 11, 0, 0, 0, DateTimeKind.Utc)
                },
                new MusicRelease
                {
                    Id = 3,
                    UserId = TestUserId,
                    Title = "Owned Album 3",
                    ReleaseYear = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    Artists = "[1,2]",
                    Genres = "[1,2]",
                    LabelId = 1,
                    FormatId = 2,
                    CountryId = 2,
                    DateAdded = new DateTime(2026, 3, 12, 0, 0, 0, DateTimeKind.Utc),
                    LastModified = new DateTime(2026, 3, 12, 0, 0, 0, DateTimeKind.Utc)
                },
                new MusicRelease
                {
                    Id = 4,
                    UserId = otherUserId,
                    Title = "Other User Album",
                    ReleaseYear = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    Artists = "[9]",
                    Genres = "[9]",
                    LabelId = 2,
                    FormatId = 2,
                    CountryId = 2,
                    PurchaseInfo = "{\"Price\":99.99}",
                    DateAdded = new DateTime(2026, 3, 13, 0, 0, 0, DateTimeKind.Utc),
                    LastModified = new DateTime(2026, 3, 13, 0, 0, 0, DateTimeKind.Utc)
                });

            dbContext.SaveChanges();
        }
    }
}