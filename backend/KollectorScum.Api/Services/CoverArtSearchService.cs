using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// Searches for album cover art using the MusicBrainz search API and the
    /// Cover Art Archive.  No paid external services are required.
    /// </summary>
    public class CoverArtSearchService : ICoverArtSearchService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<CoverArtSearchService> _logger;

        /// <summary>Named <see cref="HttpClient"/> key for the MusicBrainz API.</summary>
        public const string MusicBrainzClientName = "musicbrainz";

        /// <summary>Named <see cref="HttpClient"/> key for the Cover Art Archive API.</summary>
        public const string CoverArtArchiveClientName = "coverartarchive";

        /// <summary>
        /// Initialises a new instance of <see cref="CoverArtSearchService"/>.
        /// </summary>
        public CoverArtSearchService(IHttpClientFactory httpClientFactory, ILogger<CoverArtSearchService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<CoverArtSearchResultDto>> SearchAsync(
            string query,
            int limit = 4,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Array.Empty<CoverArtSearchResultDto>();

            limit = Math.Clamp(limit, 1, 10);

            var releases = await SearchMusicBrainzAsync(query, limit * 3, cancellationToken);
            if (releases.Count == 0)
            {
                _logger.LogDebug("MusicBrainz returned no results for query: {Query}", query);
                return Array.Empty<CoverArtSearchResultDto>();
            }

            var results = new List<CoverArtSearchResultDto>();
            foreach (var release in releases)
            {
                if (results.Count >= limit) break;

                var coverUrls = await TryGetCoverArtAsync(release.Id, cancellationToken);
                if (coverUrls == null)
                {
                    _logger.LogDebug("No cover art found for MBID {MbId}", release.Id);
                    continue;
                }

                var artist = release.ArtistCredit?.FirstOrDefault()?.Name ?? string.Empty;
                var year = ParseYear(release.Date);
                var format = release.Media?.FirstOrDefault()?.Format;
                var country = release.Country;
                var label = release.LabelInfo?.FirstOrDefault()?.Label?.Name;
                var confidence = (release.Score ?? 0) / 100.0;

                results.Add(new CoverArtSearchResultDto
                {
                    MbId = release.Id,
                    Artist = artist,
                    Title = release.Title ?? string.Empty,
                    Year = year,
                    Format = format,
                    Country = country,
                    Label = label,
                    ImageUrl = coverUrls.Value.imageUrl,
                    ThumbnailUrl = coverUrls.Value.thumbnailUrl,
                    Confidence = Math.Round(confidence, 2),
                });
            }

            return results;
        }

        // ─── MusicBrainz ────────────────────────────────────────────────────────────

        private async Task<List<MbRelease>> SearchMusicBrainzAsync(
            string query,
            int limit,
            CancellationToken cancellationToken)
        {
            try
            {
                var client = _httpClientFactory.CreateClient(MusicBrainzClientName);
                var encodedQuery = Uri.EscapeDataString(query);
                var url = $"release/?query={encodedQuery}&limit={limit}&fmt=json";

                var response = await client.GetFromJsonAsync<MbSearchResponse>(
                    url,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                    cancellationToken);

                return response?.Releases ?? new List<MbRelease>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MusicBrainz search failed for query: {Query}", query);
                return new List<MbRelease>();
            }
        }

        // ─── Cover Art Archive ───────────────────────────────────────────────────────

        private async Task<(string imageUrl, string thumbnailUrl)?> TryGetCoverArtAsync(
            string mbid,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(mbid)) return null;
            try
            {
                var client = _httpClientFactory.CreateClient(CoverArtArchiveClientName);
                var response = await client.GetFromJsonAsync<CaaResponse>(
                    $"release/{mbid}",
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                    cancellationToken);

                var front = response?.Images?.FirstOrDefault(i => i.Front == true)
                         ?? response?.Images?.FirstOrDefault();

                if (front == null) return null;

                var imageUrl = front.Image ?? string.Empty;
                var thumbUrl = front.Thumbnails?.Large
                            ?? front.Thumbnails?.Small
                            ?? front.Image
                            ?? string.Empty;

                return (imageUrl, thumbUrl);
            }
            catch (HttpRequestException ex) when ((int?)ex.StatusCode == 404)
            {
                // Release has no cover art in the archive – expected, not an error
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Cover Art Archive lookup failed for MBID {MbId}", mbid);
                return null;
            }
        }

        // ─── Helpers ─────────────────────────────────────────────────────────────────

        private static int? ParseYear(string? date)
        {
            if (string.IsNullOrWhiteSpace(date)) return null;
            var yearPart = date.Split('-')[0];
            return int.TryParse(yearPart, out var year) ? year : null;
        }

        // ─── Internal MusicBrainz JSON models ────────────────────────────────────────

        private sealed class MbSearchResponse
        {
            [JsonPropertyName("releases")]
            public List<MbRelease> Releases { get; set; } = new();
        }

        private sealed class MbRelease
        {
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonPropertyName("title")]
            public string? Title { get; set; }

            [JsonPropertyName("date")]
            public string? Date { get; set; }

            [JsonPropertyName("country")]
            public string? Country { get; set; }

            [JsonPropertyName("score")]
            public int? Score { get; set; }

            [JsonPropertyName("artist-credit")]
            public List<MbArtistCredit>? ArtistCredit { get; set; }

            [JsonPropertyName("media")]
            public List<MbMedia>? Media { get; set; }

            [JsonPropertyName("label-info")]
            public List<MbLabelInfo>? LabelInfo { get; set; }
        }

        private sealed class MbArtistCredit
        {
            [JsonPropertyName("name")]
            public string? Name { get; set; }
        }

        private sealed class MbMedia
        {
            [JsonPropertyName("format")]
            public string? Format { get; set; }
        }

        private sealed class MbLabelInfo
        {
            [JsonPropertyName("label")]
            public MbLabel? Label { get; set; }
        }

        private sealed class MbLabel
        {
            [JsonPropertyName("name")]
            public string? Name { get; set; }
        }

        // ─── Internal Cover Art Archive JSON models ───────────────────────────────────

        private sealed class CaaResponse
        {
            [JsonPropertyName("images")]
            public List<CaaImage>? Images { get; set; }
        }

        private sealed class CaaImage
        {
            [JsonPropertyName("image")]
            public string? Image { get; set; }

            [JsonPropertyName("front")]
            public bool? Front { get; set; }

            [JsonPropertyName("thumbnails")]
            public CaaThumbnails? Thumbnails { get; set; }
        }

        private sealed class CaaThumbnails
        {
            [JsonPropertyName("large")]
            public string? Large { get; set; }

            [JsonPropertyName("small")]
            public string? Small { get; set; }
        }
    }
}
