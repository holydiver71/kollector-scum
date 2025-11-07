using System.Net.Http.Headers;
using KollectorScum.Api.Interfaces;
using Microsoft.Extensions.Options;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// HTTP client for communicating with Discogs API
    /// </summary>
    public class DiscogsHttpClient : IDiscogsHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly DiscogsSettings _settings;
        private readonly ILogger<DiscogsHttpClient> _logger;

        public DiscogsHttpClient(
            HttpClient httpClient,
            IOptions<DiscogsSettings> settings,
            ILogger<DiscogsHttpClient> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            ConfigureHttpClient();
        }

        private void ConfigureHttpClient()
        {
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
        /// Search for releases with the specified parameters
        /// </summary>
        public async Task<string?> SearchReleasesAsync(
            string catalogNumber,
            string? format = null,
            string? country = null,
            int? year = null)
        {
            try
            {
                _logger.LogInformation("Searching Discogs for catalog number: {CatalogNumber}", catalogNumber);

                var requestUri = BuildSearchRequestUri(catalogNumber, format, country, year);
                var response = await _httpClient.GetAsync(requestUri);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Discogs API returned status code: {StatusCode}", response.StatusCode);
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Successfully retrieved search results for catalog number: {CatalogNumber}", catalogNumber);
                
                return content;
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
        public async Task<string?> GetReleaseDetailsAsync(string releaseId)
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
                _logger.LogInformation("Successfully fetched release details for ID: {ReleaseId}", releaseId);
                
                return content;
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

        private string BuildSearchRequestUri(
            string catalogNumber,
            string? format,
            string? country,
            int? year)
        {
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
            return $"/database/search?{queryString}";
        }
    }
}
