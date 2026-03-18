using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace KollectorScum.Tests.Services
{
    /// <summary>
    /// Unit tests for <see cref="CoverArtSearchService"/> using mocked HTTP handlers.
    /// </summary>
    public class CoverArtSearchServiceTests
    {
        private readonly Mock<ILogger<CoverArtSearchService>> _mockLogger = new();
        private readonly Mock<IDiscogsService> _mockDiscogsService = new();

        // ─── Helpers ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Builds an <see cref="IHttpClientFactory"/> that returns a specific
        /// <see cref="HttpClient"/> for each named client key.
        /// </summary>
        private static IHttpClientFactory BuildFactory(
            HttpMessageHandler mbHandler,
            HttpMessageHandler caaHandler)
        {
            var mbClient = new HttpClient(mbHandler) { BaseAddress = new Uri("https://musicbrainz.org/ws/2/") };
            var caaClient = new HttpClient(caaHandler) { BaseAddress = new Uri("https://coverartarchive.org/") };

            var factory = new Mock<IHttpClientFactory>();
            factory.Setup(f => f.CreateClient(CoverArtSearchService.MusicBrainzClientName)).Returns(mbClient);
            factory.Setup(f => f.CreateClient(CoverArtSearchService.CoverArtArchiveClientName)).Returns(caaClient);
            return factory.Object;
        }

        private static HttpMessageHandler BuildHandler(HttpStatusCode status, string json)
        {
            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(status)
                {
                    Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
                });
            return handler.Object;
        }

        private CoverArtSearchService CreateService(
            HttpMessageHandler mbHandler,
            HttpMessageHandler caaHandler)
        {
            return new CoverArtSearchService(BuildFactory(mbHandler, caaHandler), _mockDiscogsService.Object, _mockLogger.Object);
        }

        // ─── SearchAsync – query validation ──────────────────────────────────────────

        [Fact]
        public async Task SearchAsync_NullQuery_ReturnsEmpty()
        {
            var service = CreateService(BuildHandler(HttpStatusCode.OK, "{}"), BuildHandler(HttpStatusCode.OK, "{}"));
            var result = await service.SearchAsync(null!);
            Assert.Empty(result);
        }

        [Fact]
        public async Task SearchAsync_EmptyQuery_ReturnsEmpty()
        {
            var service = CreateService(BuildHandler(HttpStatusCode.OK, "{}"), BuildHandler(HttpStatusCode.OK, "{}"));
            var result = await service.SearchAsync("   ");
            Assert.Empty(result);
        }

        // ─── SearchAsync – MusicBrainz failure paths ──────────────────────────────────

        [Fact]
        public async Task SearchAsync_MusicBrainzError_ReturnsEmpty()
        {
            var service = CreateService(
                BuildHandler(HttpStatusCode.ServiceUnavailable, ""),
                BuildHandler(HttpStatusCode.OK, "{}"));

            var result = await service.SearchAsync("Iron Maiden Killers");
            Assert.Empty(result);
        }

        [Fact]
        public async Task SearchAsync_MusicBrainzEmptyReleases_ReturnsEmpty()
        {
            var mbJson = JsonSerializer.Serialize(new { releases = Array.Empty<object>() });
            var service = CreateService(
                BuildHandler(HttpStatusCode.OK, mbJson),
                BuildHandler(HttpStatusCode.OK, "{}"));

            var result = await service.SearchAsync("Totally Unknown Album");
            Assert.Empty(result);
        }

        // ─── SearchAsync – happy path ─────────────────────────────────────────────────

        [Fact]
        public async Task SearchAsync_ValidResults_ReturnsMappedDtos()
        {
            var mbJson = """
                {
                  "releases": [
                    {
                      "id": "abc-123",
                      "title": "Killers",
                      "date": "1981-02-02",
                      "country": "GB",
                      "score": 100,
                      "artist-credit": [{ "name": "Iron Maiden" }],
                      "media": [{ "format": "Vinyl" }],
                      "label-info": [{ "label": { "name": "EMI" } }]
                    }
                  ]
                }
                """;

            var caaJson = """
                {
                  "images": [
                    {
                      "image": "https://coverartarchive.org/release/abc-123/front-500.jpg",
                      "front": true,
                      "thumbnails": {
                        "large": "https://coverartarchive.org/release/abc-123/front-250.jpg",
                        "small": "https://coverartarchive.org/release/abc-123/front-250.jpg"
                      }
                    }
                  ]
                }
                """;

            var service = CreateService(
                BuildHandler(HttpStatusCode.OK, mbJson),
                BuildHandler(HttpStatusCode.OK, caaJson));

            var results = await service.SearchAsync("Iron Maiden Killers");

            Assert.Single(results);
            var dto = results[0];
            Assert.Equal("abc-123", dto.MbId);
            Assert.Equal("Iron Maiden", dto.Artist);
            Assert.Equal("Killers", dto.Title);
            Assert.Equal(1981, dto.Year);
            Assert.Equal("GB", dto.Country);
            Assert.Equal("Vinyl", dto.Format);
            Assert.Equal("EMI", dto.Label);
            Assert.Equal(1.0, dto.Confidence);
            Assert.Equal("Exact match", dto.ConfidenceLabel);
            Assert.NotNull(dto.ImageUrl);
            Assert.NotNull(dto.ThumbnailUrl);
        }

        [Fact]
        public async Task SearchAsync_CaaReturns404_SkipsRelease()
        {
            var mbJson = """
                {
                  "releases": [
                    { "id": "no-cover-id", "title": "Obscure Album", "score": 90 }
                  ]
                }
                """;

            var service = CreateService(
                BuildHandler(HttpStatusCode.OK, mbJson),
                BuildHandler(HttpStatusCode.NotFound, ""));

            var results = await service.SearchAsync("Obscure Album");
            Assert.Empty(results);
        }

        [Fact]
        public async Task SearchAsync_LimitClamped_RespectsMaximum()
        {
            // Build MB response with 3 releases all with cover art
            var releases = Enumerable.Range(1, 3).Select(i => new
            {
                id = $"id-{i}",
                title = $"Album {i}",
                score = 90,
                date = "2000",
                country = "GB",
            }).ToArray();

            var mbJson = JsonSerializer.Serialize(new { releases });

            var caaJson = """
                {
                  "images": [
                    { "image": "https://example.com/img.jpg", "front": true,
                      "thumbnails": { "large": "https://example.com/thumb.jpg" } }
                  ]
                }
                """;

            var service = CreateService(
                BuildHandler(HttpStatusCode.OK, mbJson),
                BuildHandler(HttpStatusCode.OK, caaJson));

            var results = await service.SearchAsync("Album", limit: 2);
            Assert.True(results.Count <= 2);
        }

        // ─── ConfidenceLabel ─────────────────────────────────────────────────────────

        [Theory]
        [InlineData(1.00, "Exact match")]
        [InlineData(0.95, "Exact match")]
        [InlineData(0.94, "Good match")]
        [InlineData(0.75, "Good match")]
        [InlineData(0.74, "Possible match")]
        [InlineData(0.00, "Possible match")]
        public void ConfidenceLabel_ReturnsCorrectLabel(double confidence, string expected)
        {
            var dto = new KollectorScum.Api.DTOs.CoverArtSearchResultDto { Confidence = confidence };
            Assert.Equal(expected, dto.ConfidenceLabel);
        }

        // ─── SearchAsync – Discogs integration ────────────────────────────────────────

        [Fact]
        public async Task SearchAsync_WithCatalogueNumber_SearchesDiscogs()
        {
            var discogsResults = new List<DiscogsSearchResultDto>
            {
                new() { Id = "123", Title = "Album", Artist = "Artist", CatalogNumber = "CAT001",
                        CoverImageUrl = "https://img.discogs.com/cover.jpg", ThumbUrl = "https://img.discogs.com/thumb.jpg",
                        Year = "1990", Format = "CD", Country = "UK", Label = "EMI" }
            };
            _mockDiscogsService.Setup(d => d.SearchByCatalogNumberAsync("CAT001", null, null, null))
                .ReturnsAsync(discogsResults);

            var mbJson = JsonSerializer.Serialize(new { releases = Array.Empty<object>() });
            var service = CreateService(
                BuildHandler(HttpStatusCode.OK, mbJson),
                BuildHandler(HttpStatusCode.OK, "{}"));

            var results = await service.SearchAsync("Artist Album", "CAT001");

            Assert.Single(results);
            var dto = results[0];
            Assert.Equal("Album", dto.Title);
            Assert.Equal("Artist", dto.Artist);
            Assert.Equal("CAT001", dto.CatalogueNumber);
            Assert.Equal(0.95, dto.Confidence);
            Assert.NotNull(dto.ImageUrl);
            Assert.NotNull(dto.ThumbnailUrl);
        }

        [Fact]
        public async Task SearchAsync_WithCatalogueNumber_FallsBackToMusicBrainzIfDiscogsReturnsNothing()
        {
            _mockDiscogsService.Setup(d => d.SearchByCatalogNumberAsync("NOTFOUND", null, null, null))
                .ReturnsAsync(new List<DiscogsSearchResultDto>());

            var mbJson = """
                {
                  "releases": [
                    {
                      "id": "abc-123",
                      "title": "Fallback Album",
                      "score": 85,
                      "artist-credit": [{ "name": "MB Artist" }],
                      "media": [{ "format": "CD" }]
                    }
                  ]
                }
                """;

            var caaJson = """
                {
                  "images": [
                    { "image": "https://example.com/img.jpg", "front": true,
                      "thumbnails": { "large": "https://example.com/thumb.jpg" } }
                  ]
                }
                """;

            var service = CreateService(
                BuildHandler(HttpStatusCode.OK, mbJson),
                BuildHandler(HttpStatusCode.OK, caaJson));

            var results = await service.SearchAsync("MB Artist Fallback Album", "NOTFOUND");

            Assert.Single(results);
            Assert.Equal("Fallback Album", results[0].Title);
            Assert.Equal("MB Artist", results[0].Artist);
        }

        [Fact]
        public async Task SearchAsync_DiscogsSkipsResultsWithoutCoverImage()
        {
            var discogsResults = new List<DiscogsSearchResultDto>
            {
                new() { Id = "1", Title = "No Cover", Artist = "Artist", CatalogNumber = "CAT002",
                        CoverImageUrl = null, ThumbUrl = null },
                new() { Id = "2", Title = "Has Cover", Artist = "Artist", CatalogNumber = "CAT002",
                        CoverImageUrl = "https://img.discogs.com/cover.jpg", ThumbUrl = "https://img.discogs.com/thumb.jpg" }
            };
            _mockDiscogsService.Setup(d => d.SearchByCatalogNumberAsync("CAT002", null, null, null))
                .ReturnsAsync(discogsResults);

            var mbJson = JsonSerializer.Serialize(new { releases = Array.Empty<object>() });
            var service = CreateService(
                BuildHandler(HttpStatusCode.OK, mbJson),
                BuildHandler(HttpStatusCode.OK, "{}"));

            var results = await service.SearchAsync("Artist Album", "CAT002");

            Assert.Single(results);
            Assert.Equal("Has Cover", results[0].Title);
        }
    }
}
