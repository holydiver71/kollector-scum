using System.Net.Http.Headers;
using System.Text.Json;
using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using Microsoft.Extensions.Options;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// Configuration settings for Discogs API
    /// </summary>
    public class DiscogsSettings
    {
        /// <summary>
        /// Base URL for Discogs API
        /// </summary>
        public string BaseUrl { get; set; } = "https://api.discogs.com";

        /// <summary>
        /// Personal access token for Discogs API
        /// </summary>
        public string? Token { get; set; }

        /// <summary>
        /// User agent string (required by Discogs API)
        /// </summary>
        public string UserAgent { get; set; } = "KollectorScum/1.0";

        /// <summary>
        /// Request timeout in seconds
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Rate limit: requests per minute
        /// </summary>
        public int RateLimitPerMinute { get; set; } = 60;
    }

    /// <summary>
    /// Service for integrating with the Discogs API
    /// </summary>
    public class DiscogsService : IDiscogsService
    {
        private readonly HttpClient _httpClient;
        private readonly DiscogsSettings _settings;
        private readonly ILogger<DiscogsService> _logger;

        public DiscogsService(
            HttpClient httpClient,
            IOptions<DiscogsSettings> settings,
            ILogger<DiscogsService> logger)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _logger = logger;

            // Configure HTTP client
            _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(_settings.UserAgent);
            
            // Add authorization token if provided
            if (!string.IsNullOrEmpty(_settings.Token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Discogs", $"token={_settings.Token}");
            }
        }

        /// <summary>
        /// Search for releases by catalog number
        /// </summary>
        public async Task<List<DiscogsSearchResultDto>> SearchByCatalogNumberAsync(
            string catalogNumber,
            string? format = null,
            string? country = null,
            int? year = null)
        {
            try
            {
                _logger.LogInformation("Searching Discogs for catalog number: {CatalogNumber}", catalogNumber);

                // Build query parameters
                var queryParams = new List<string>
                {
                    $"catno={Uri.EscapeDataString(catalogNumber)}",
                    "type=release"
                };

                if (!string.IsNullOrEmpty(format))
                    queryParams.Add($"format={Uri.EscapeDataString(format)}");
                
                if (!string.IsNullOrEmpty(country))
                    queryParams.Add($"country={Uri.EscapeDataString(country)}");
                
                if (year.HasValue)
                    queryParams.Add($"year={year.Value}");

                var queryString = string.Join("&", queryParams);
                var requestUri = $"/database/search?{queryString}";

                var response = await _httpClient.GetAsync(requestUri);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Discogs API returned status code: {StatusCode}", response.StatusCode);
                    return new List<DiscogsSearchResultDto>();
                }

                var content = await response.Content.ReadAsStringAsync();
                var searchResponse = JsonSerializer.Deserialize<DiscogsSearchResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (searchResponse?.Results == null)
                {
                    _logger.LogWarning("No results returned from Discogs API");
                    return new List<DiscogsSearchResultDto>();
                }

                _logger.LogInformation("Found {Count} results from Discogs", searchResponse.Results.Count);

                // Map results to DTOs
                var results = searchResponse.Results.Select(r => new DiscogsSearchResultDto
                {
                    Id = r.Id?.ToString() ?? string.Empty,
                    Title = r.Title ?? string.Empty,
                    Artist = string.Join(", ", r.Artist ?? Array.Empty<string>()),
                    Year = r.Year,
                    Format = r.Format?.FirstOrDefault(),
                    Label = r.Label?.FirstOrDefault(),
                    CatalogNumber = r.Catno,
                    Country = r.Country,
                    ThumbUrl = r.Thumb,
                    CoverImageUrl = r.CoverImage,
                    ResourceUrl = r.ResourceUrl
                }).ToList();

                return results;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error when searching Discogs for catalog number: {CatalogNumber}", catalogNumber);
                throw new Exception($"Failed to connect to Discogs API: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching Discogs for catalog number: {CatalogNumber}", catalogNumber);
                throw;
            }
        }

        /// <summary>
        /// Get detailed information about a specific release
        /// </summary>
        public async Task<DiscogsReleaseDto?> GetReleaseDetailsAsync(string releaseId)
        {
            try
            {
                _logger.LogInformation("Fetching Discogs release details for ID: {ReleaseId}", releaseId);

                var requestUri = $"/releases/{releaseId}";
                var response = await _httpClient.GetAsync(requestUri);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Discogs API returned status code: {StatusCode} for release: {ReleaseId}",
                        response.StatusCode, releaseId);
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var releaseResponse = JsonSerializer.Deserialize<DiscogsReleaseResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (releaseResponse == null)
                {
                    _logger.LogWarning("Failed to deserialize release details for ID: {ReleaseId}", releaseId);
                    return null;
                }

                _logger.LogInformation("Successfully fetched release details for ID: {ReleaseId}", releaseId);

                // Map to DTO
                var dto = new DiscogsReleaseDto
                {
                    Id = releaseResponse.Id?.ToString() ?? string.Empty,
                    Title = releaseResponse.Title ?? string.Empty,
                    Year = releaseResponse.Year,
                    Country = releaseResponse.Country,
                    ReleasedDate = releaseResponse.ReleasedFormatted,
                    ResourceUrl = releaseResponse.ResourceUrl,
                    Uri = releaseResponse.Uri,
                    Notes = releaseResponse.Notes,
                    Genres = releaseResponse.Genres ?? new List<string>(),
                    Styles = releaseResponse.Styles ?? new List<string>(),
                    Artists = releaseResponse.Artists?.Select(a => new DiscogsArtistDto
                    {
                        Name = a.Name ?? string.Empty,
                        Id = a.Id?.ToString(),
                        ResourceUrl = a.ResourceUrl
                    }).ToList() ?? new List<DiscogsArtistDto>(),
                    Labels = releaseResponse.Labels?.Select(l => new DiscogsLabelDto
                    {
                        Name = l.Name ?? string.Empty,
                        CatalogNumber = l.Catno,
                        Id = l.Id?.ToString(),
                        ResourceUrl = l.ResourceUrl
                    }).ToList() ?? new List<DiscogsLabelDto>(),
                    Formats = releaseResponse.Formats?.Select(f => new DiscogsFormatDto
                    {
                        Name = f.Name ?? string.Empty,
                        Qty = f.Qty,
                        Descriptions = f.Descriptions ?? new List<string>()
                    }).ToList() ?? new List<DiscogsFormatDto>(),
                    Images = releaseResponse.Images?.Select(i => new DiscogsImageDto
                    {
                        Type = i.Type ?? string.Empty,
                        Uri = i.Uri ?? string.Empty,
                        ResourceUrl = i.ResourceUrl,
                        Width = i.Width,
                        Height = i.Height
                    }).ToList() ?? new List<DiscogsImageDto>(),
                    Tracklist = releaseResponse.Tracklist?.Select(t => new DiscogsTrackDto
                    {
                        Position = t.Position ?? string.Empty,
                        Title = t.Title ?? string.Empty,
                        Duration = t.Duration,
                        Artists = t.Artists?.Select(a => new DiscogsArtistDto
                        {
                            Name = a.Name ?? string.Empty,
                            Id = a.Id?.ToString()
                        }).ToList()
                    }).ToList() ?? new List<DiscogsTrackDto>(),
                    Identifiers = releaseResponse.Identifiers?.Select(i => new DiscogsIdentifierDto
                    {
                        Type = i.Type ?? string.Empty,
                        Value = i.Value ?? string.Empty
                    }).ToList() ?? new List<DiscogsIdentifierDto>()
                };

                return dto;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error when fetching Discogs release: {ReleaseId}", releaseId);
                throw new Exception($"Failed to connect to Discogs API: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Discogs release: {ReleaseId}", releaseId);
                throw;
            }
        }

        #region Internal Response Models (for deserialization)

        private class DiscogsSearchResponse
        {
            public List<DiscogsSearchResult>? Results { get; set; }
        }

        private class DiscogsSearchResult
        {
            public long? Id { get; set; }
            public string? Title { get; set; }
            public string[]? Artist { get; set; }
            public string? Year { get; set; }
            public string[]? Format { get; set; }
            public string[]? Label { get; set; }
            public string? Catno { get; set; }
            public string? Country { get; set; }
            public string? Thumb { get; set; }
            public string? CoverImage { get; set; }
            public string? ResourceUrl { get; set; }
        }

        private class DiscogsReleaseResponse
        {
            public long? Id { get; set; }
            public string? Title { get; set; }
            public int? Year { get; set; }
            public string? Country { get; set; }
            public string? ReleasedFormatted { get; set; }
            public string? ResourceUrl { get; set; }
            public string? Uri { get; set; }
            public string? Notes { get; set; }
            public List<string>? Genres { get; set; }
            public List<string>? Styles { get; set; }
            public List<DiscogsArtistResponse>? Artists { get; set; }
            public List<DiscogsLabelResponse>? Labels { get; set; }
            public List<DiscogsFormatResponse>? Formats { get; set; }
            public List<DiscogsImageResponse>? Images { get; set; }
            public List<DiscogsTrackResponse>? Tracklist { get; set; }
            public List<DiscogsIdentifierResponse>? Identifiers { get; set; }
        }

        private class DiscogsArtistResponse
        {
            public string? Name { get; set; }
            public long? Id { get; set; }
            public string? ResourceUrl { get; set; }
        }

        private class DiscogsLabelResponse
        {
            public string? Name { get; set; }
            public string? Catno { get; set; }
            public long? Id { get; set; }
            public string? ResourceUrl { get; set; }
        }

        private class DiscogsFormatResponse
        {
            public string? Name { get; set; }
            public string? Qty { get; set; }
            public List<string>? Descriptions { get; set; }
        }

        private class DiscogsImageResponse
        {
            public string? Type { get; set; }
            public string? Uri { get; set; }
            public string? ResourceUrl { get; set; }
            public int? Width { get; set; }
            public int? Height { get; set; }
        }

        private class DiscogsTrackResponse
        {
            public string? Position { get; set; }
            public string? Title { get; set; }
            public string? Duration { get; set; }
            public List<DiscogsArtistResponse>? Artists { get; set; }
        }

        private class DiscogsIdentifierResponse
        {
            public string? Type { get; set; }
            public string? Value { get; set; }
        }

        #endregion
    }
}
