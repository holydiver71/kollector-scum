using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using KollectorScum.Api.Data;
using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace KollectorScum.Tests.Integration
{
    /// <summary>
    /// Integration tests for the add-release related endpoints.
    /// Tests the complete flow from frontend form submission to database storage.
    /// </summary>
    public class AddReleaseIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly JsonSerializerOptions _jsonOptions;

        public AddReleaseIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = "Test";
                        options.DefaultChallengeScheme = "Test";
                    })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });
                });
            });

            // Use camelCase to match frontend and backend JSON configuration
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        #region Discogs Integration Tests

        [Fact]
        public async Task SearchEndpoint_ReturnsOkAndResults_WhenDiscogsServiceReturnsData()
        {
            // Arrange
            var mockDiscogs = new Mock<IDiscogsService>();
            var sample = new List<DiscogsSearchResultDto>
            {
                new DiscogsSearchResultDto { Id = "1", Title = "Album", Artist = "Artist", CatalogNumber = "CAT1" }
            };

            mockDiscogs
                .Setup(s => s.SearchByCatalogNumberAsync("CAT1", null, null, null))
                .ReturnsAsync(sample);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton(mockDiscogs.Object);
                });
            }).CreateClient();

            // Act
            var response = await client.GetAsync("/api/discogs/search?catalogNumber=CAT1");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var results = await response.Content.ReadFromJsonAsync<List<DiscogsSearchResultDto>>(_jsonOptions);
            Assert.NotNull(results);
            Assert.Single(results);
            Assert.Equal("CAT1", results[0].CatalogNumber);
        }

        #endregion

        #region Create Music Release Integration Tests

        [Fact]
        public async Task CreateMusicRelease_WithMinimalRequiredFields_ReturnsCreatedRelease()
        {
            // Arrange
            var client = _factory.CreateClient();
            var createDto = new CreateMusicReleaseDto
            {
                Title = "Test Album",
                ArtistIds = new List<int> { 1 }, // Assuming artist with ID 1 exists in test DB
                GenreIds = new List<int>(),
                Live = false
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/musicreleases", createDto, _jsonOptions);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<CreateMusicReleaseResponseDto>(_jsonOptions);
            Assert.NotNull(result);
            Assert.NotNull(result.Release);
            Assert.True(result.Release.Id > 0);
            Assert.Equal("Test Album", result.Release.Title);
        }

        [Fact]
        public async Task CreateMusicRelease_WithOptionalFields_ReturnsCreatedReleaseWithAllData()
        {
            // Arrange
            var client = _factory.CreateClient();
            var uniqueSuffix = Guid.NewGuid().ToString().Substring(0, 8);
            
            var createDto = new CreateMusicReleaseDto
            {
                Title = $"Complete Test Album {uniqueSuffix}",
                ReleaseYear = new DateTime(1983, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                OrigReleaseYear = new DateTime(1982, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                ArtistNames = new List<string> { $"Test Artist {uniqueSuffix}" },
                GenreNames = new List<string> { $"Test Genre {uniqueSuffix}" },
                Live = false,
                LabelName = $"Test Label {uniqueSuffix}",
                CountryName = $"Test Country {uniqueSuffix}",
                LabelNumber = $"TEST-{uniqueSuffix}",
                Upc = "123456789012",
                LengthInSeconds = 3600,
                FormatName = $"Test Format {uniqueSuffix}",
                PackagingName = $"Test Packaging {uniqueSuffix}",
                Images = new MusicReleaseImageDto
                {
                    CoverFront = "front.jpg",
                    CoverBack = "back.jpg",
                    Thumbnail = "thumb.jpg"
                },
                Links = new List<MusicReleaseLinkDto>
                {
                    new MusicReleaseLinkDto
                    {
                        Url = "https://spotify.com/album/test",
                        Type = "spotify",
                        Description = "Spotify link"
                    }
                },
                Media = new List<MusicReleaseMediaDto>
                {
                    new MusicReleaseMediaDto
                    {
                        Name = "CD 1",
                        Tracks = new List<MusicReleaseTrackDto>
                        {
                            new MusicReleaseTrackDto
                            {
                                Title = "Track 1",
                                Index = 1,
                                LengthSecs = 240,
                                Artists = new List<string> { "Artist 1" },
                                Genres = new List<string> { "Rock" },
                                Live = false
                            }
                        }
                    }
                }
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/musicreleases", createDto, _jsonOptions);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<CreateMusicReleaseResponseDto>(_jsonOptions);
            Assert.NotNull(result);
            Assert.NotNull(result.Release);
            Assert.True(result.Release.Id > 0);
            Assert.Equal($"Complete Test Album {uniqueSuffix}", result.Release.Title);
            Assert.Equal(1983, result.Release.ReleaseYear?.Year);
            Assert.Equal(1982, result.Release.OrigReleaseYear?.Year);
            Assert.NotNull(result.Release.Images);
            Assert.Equal("front.jpg", result.Release.Images.CoverFront);
            Assert.NotNull(result.Release.Links);
            Assert.Single(result.Release.Links);
            Assert.NotNull(result.Release.Media);
            Assert.Single(result.Release.Media);
            Assert.NotNull(result.Release.Media[0].Tracks);
            Assert.Single(result.Release.Media[0].Tracks!);
        }

        [Fact]
        public async Task CreateMusicRelease_WithAutoCreation_CreatesNewLookupEntities()
        {
            // Arrange
            var client = _factory.CreateClient();
            var uniqueSuffix = Guid.NewGuid().ToString().Substring(0, 8);
            var createDto = new CreateMusicReleaseDto
            {
                Title = $"Auto-Creation Test Album {uniqueSuffix}",
                ArtistNames = new List<string> { $"New Artist {uniqueSuffix}" },
                GenreNames = new List<string> { $"New Genre {uniqueSuffix}" },
                LabelName = $"New Label {uniqueSuffix}",
                CountryName = $"New Country {uniqueSuffix}",
                FormatName = $"New Format {uniqueSuffix}",
                PackagingName = $"New Packaging {uniqueSuffix}",
                Live = false
                // Note: Store auto-creation is only supported in Update operations currently
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/musicreleases", createDto, _jsonOptions);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<CreateMusicReleaseResponseDto>(_jsonOptions);
            Assert.NotNull(result);
            Assert.NotNull(result.Release);
            
            // Verify auto-created entities are in the Created property
            Assert.NotNull(result.Created);
            Assert.NotNull(result.Created.Artists);
            Assert.Single(result.Created.Artists);
            Assert.Equal($"New Artist {uniqueSuffix}", result.Created.Artists[0].Name);
            
            Assert.NotNull(result.Created.Genres);
            Assert.Single(result.Created.Genres);
            Assert.Equal($"New Genre {uniqueSuffix}", result.Created.Genres[0].Name);
            
            Assert.NotNull(result.Created.Labels);
            Assert.Single(result.Created.Labels);
            Assert.Equal($"New Label {uniqueSuffix}", result.Created.Labels[0].Name);
            
            Assert.NotNull(result.Created.Countries);
            Assert.Single(result.Created.Countries);
            Assert.Equal($"New Country {uniqueSuffix}", result.Created.Countries[0].Name);
            
            Assert.NotNull(result.Created.Formats);
            Assert.Single(result.Created.Formats);
            Assert.Equal($"New Format {uniqueSuffix}", result.Created.Formats[0].Name);
            
            Assert.NotNull(result.Created.Packagings);
            Assert.Single(result.Created.Packagings);
            Assert.Equal($"New Packaging {uniqueSuffix}", result.Created.Packagings[0].Name);
        }

        [Fact]
        public async Task CreateMusicRelease_WithoutTitle_ReturnsBadRequest()
        {
            // Arrange
            var client = _factory.CreateClient();
            var createDto = new CreateMusicReleaseDto
            {
                Title = "", // Empty title
                ArtistIds = new List<int> { 1 },
                GenreIds = new List<int>(),
                Live = false
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/musicreleases", createDto, _jsonOptions);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateMusicRelease_WithoutArtists_ReturnsBadRequest()
        {
            // Arrange
            var client = _factory.CreateClient();
            var createDto = new CreateMusicReleaseDto
            {
                Title = "Test Album",
                GenreIds = new List<int>(),
                Live = false
                // No ArtistIds or ArtistNames
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/musicreleases", createDto, _jsonOptions);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateMusicRelease_WithInvalidImageUrl_ReturnsBadRequest()
        {
            // Arrange
            var client = _factory.CreateClient();
            var createDto = new CreateMusicReleaseDto
            {
                Title = "Test Album",
                ArtistIds = new List<int> { 1 },
                GenreIds = new List<int>(),
                Live = false,
                Images = new MusicReleaseImageDto
                {
                    // Validator rejects full URLs, expects filenames
                    CoverFront = "http://example.com/image.jpg"
                }
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/musicreleases", createDto, _jsonOptions);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateMusicRelease_WithNegativePrice_ReturnsBadRequest()
        {
            // Arrange
            var client = _factory.CreateClient();
            var createDto = new CreateMusicReleaseDto
            {
                Title = "Test Album",
                ArtistIds = new List<int> { 1 },
                GenreIds = new List<int>(),
                Live = false,
                PurchaseInfo = new MusicReleasePurchaseInfoDto
                {
                    Price = -10.00m, // Negative price
                    Currency = "USD"
                }
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/musicreleases", createDto, _jsonOptions);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateMusicRelease_WithYearAsString_ConvertsToDateTime()
        {
            // Arrange - This simulates what the frontend does
            var client = _factory.CreateClient();
            // Frontend converts "1983" to ISO DateTime format
            var releaseYear = new DateTime(1983, 1, 1).ToUniversalTime();
            
            var createDto = new CreateMusicReleaseDto
            {
                Title = "Year Conversion Test",
                ReleaseYear = releaseYear,
                ArtistIds = new List<int> { 1 },
                GenreIds = new List<int>(),
                Live = false
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/musicreleases", createDto, _jsonOptions);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<CreateMusicReleaseResponseDto>(_jsonOptions);
            Assert.NotNull(result);
            Assert.NotNull(result.Release);
            Assert.Equal(1983, result.Release.ReleaseYear?.Year);
        }

        [Fact]
        public async Task CreateMusicRelease_WithTracksContainingAllFields_StoresTracksCorrectly()
        {
            // Arrange
            var client = _factory.CreateClient();
            var createDto = new CreateMusicReleaseDto
            {
                Title = "Track Test Album",
                ArtistIds = new List<int> { 1 },
                GenreIds = new List<int>(),
                Live = false,
                Media = new List<MusicReleaseMediaDto>
                {
                    new MusicReleaseMediaDto
                    {
                        Name = "Disc 1",
                        Tracks = new List<MusicReleaseTrackDto>
                        {
                            new MusicReleaseTrackDto
                            {
                                Title = "Opening Track",
                                Index = 1,
                                LengthSecs = 180,
                                Artists = new List<string> { "Lead Artist", "Featured Artist" },
                                Genres = new List<string> { "Rock", "Progressive" },
                                Live = false
                            },
                            new MusicReleaseTrackDto
                            {
                                Title = "Live Track",
                                Index = 2,
                                LengthSecs = 420,
                                Artists = new List<string> { "Main Artist" },
                                Genres = new List<string> { "Rock" },
                                Live = true
                            }
                        }
                    }
                }
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/musicreleases", createDto, _jsonOptions);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<CreateMusicReleaseResponseDto>(_jsonOptions);
            Assert.NotNull(result);
            Assert.NotNull(result.Release.Media);
            Assert.Single(result.Release.Media);
            Assert.Equal(2, result.Release.Media[0].Tracks?.Count);
            
            var track1 = result.Release.Media[0].Tracks?[0];
            Assert.NotNull(track1);
            Assert.Equal("Opening Track", track1.Title);
            Assert.Equal(180, track1.LengthSecs);
            Assert.Equal(2, track1.Artists.Count);
            Assert.False(track1.Live);
            
            var track2 = result.Release.Media[0].Tracks?[1];
            Assert.NotNull(track2);
            Assert.True(track2.Live);
        }

        #endregion
    }
}
