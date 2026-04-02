using System.Net.Http.Headers;
using KollectorScum.Api.Interfaces;
using Microsoft.Extensions.Options;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// HTTP client for communicating with Discogs API.
    /// Includes automatic rate-limit awareness: responses are inspected for
    /// <c>X-Discogs-Ratelimit-Remaining</c> and the client throttles itself
    /// accordingly.  429 responses are retried with the <c>Retry-After</c>
    /// delay (or a 62-second back-off when the header is absent).
    /// </summary>
    public class DiscogsHttpClient : IDiscogsHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly DiscogsSettings _settings;
        private readonly ILogger<DiscogsHttpClient> _logger;

        // Rate-limit state observed from response headers.  Defaults assume
        // the authenticated limit of 60 req/min; updated on every response.
        private int _rateLimitRemaining = 60;
        private int _rateLimitTotal = 60;
        private const int RateLimitPauseThreshold = 3;
        private const int RateLimitWindowSeconds = 62;
        private const int MaxRetryAttempts = 3;

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
        /// Executes a GET request against the Discogs API with automatic rate-limit
        /// awareness and retry logic for 429 responses.
        /// </summary>
        /// <param name="requestUri">Relative URI to request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Response body string, or <c>null</c> when the request fails non-transiently.</returns>
        private async Task<string?> ExecuteGetAsync(string requestUri, CancellationToken cancellationToken = default)
        {
            for (int attempt = 0; attempt <= MaxRetryAttempts; attempt++)
            {
                // Throttle proactively when we're close to the rate-limit window ceiling.
                if (_rateLimitRemaining <= RateLimitPauseThreshold)
                {
                    _logger.LogInformation(
                        "Discogs rate limit nearly exhausted ({Remaining}/{Total}). Pausing {Seconds}s to allow window reset.",
                        _rateLimitRemaining, _rateLimitTotal, RateLimitWindowSeconds);
                    await Task.Delay(TimeSpan.FromSeconds(RateLimitWindowSeconds), cancellationToken);
                    _rateLimitRemaining = _rateLimitTotal;
                }

                var response = await _httpClient.GetAsync(requestUri, cancellationToken);

                // Update observed rate-limit state from response headers.
                UpdateRateLimitFromResponse(response);

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    var retryDelay = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(RateLimitWindowSeconds);
                    _logger.LogWarning(
                        "Discogs returned 429 (attempt {Attempt}/{Max}). Waiting {Delay}s before retry.",
                        attempt + 1, MaxRetryAttempts, (int)retryDelay.TotalSeconds);

                    if (attempt == MaxRetryAttempts)
                    {
                        _logger.LogError("Discogs rate limit exceeded after {Max} retries for URI {Uri}.", MaxRetryAttempts, requestUri);
                        return null;
                    }

                    await Task.Delay(retryDelay, cancellationToken);
                    _rateLimitRemaining = _rateLimitTotal; // Assume window has reset.
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Discogs API returned {StatusCode} for {Uri}.", response.StatusCode, requestUri);
                    return null;
                }

                return await response.Content.ReadAsStringAsync(cancellationToken);
            }

            return null;
        }

        /// <summary>
        /// Reads rate-limit headers from the response and updates the local tracking state.
        /// </summary>
        private void UpdateRateLimitFromResponse(HttpResponseMessage response)
        {
            if (response.Headers.TryGetValues("X-Discogs-Ratelimit", out var limitValues)
                && int.TryParse(limitValues.FirstOrDefault(), out var limit))
            {
                _rateLimitTotal = limit;
            }

            if (response.Headers.TryGetValues("X-Discogs-Ratelimit-Remaining", out var remainingValues)
                && int.TryParse(remainingValues.FirstOrDefault(), out var remaining))
            {
                _rateLimitRemaining = remaining;
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
                var content = await ExecuteGetAsync(requestUri);
                if (content != null)
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
        /// Generic search for releases with various parameters
        /// </summary>
        public async Task<string?> SearchGenericAsync(
            string? query = null,
            string? type = null,
            string? genre = null,
            string? style = null,
            string? country = null,
            int? year = null,
            string? format = null)
        {
            try
            {
                _logger.LogInformation("Searching Discogs (Generic): Query={Query}, Type={Type}, Genre={Genre}, Year={Year}",
                    query, type, genre, year);
                var requestUri = BuildGenericSearchRequestUri(query, type, genre, style, country, year, format);
                return await ExecuteGetAsync(requestUri);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching Discogs (Generic)");
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
                var content = await ExecuteGetAsync(requestUri);
                if (content != null)
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

        private string BuildGenericSearchRequestUri(
            string? query,
            string? type,
            string? genre,
            string? style,
            string? country,
            int? year,
            string? format)
        {
            var queryParams = new List<string>();

            if (!string.IsNullOrEmpty(query))
                queryParams.Add($"q={Uri.EscapeDataString(query)}");

            if (!string.IsNullOrEmpty(type))
                queryParams.Add($"type={Uri.EscapeDataString(type)}");

            if (!string.IsNullOrEmpty(genre))
                queryParams.Add($"genre={Uri.EscapeDataString(genre)}");

            if (!string.IsNullOrEmpty(style))
                queryParams.Add($"style={Uri.EscapeDataString(style)}");

            if (!string.IsNullOrEmpty(country))
                queryParams.Add($"country={Uri.EscapeDataString(country)}");

            if (year.HasValue)
                queryParams.Add($"year={year.Value}");

            if (!string.IsNullOrEmpty(format))
                queryParams.Add($"format={Uri.EscapeDataString(format)}");

            var queryString = string.Join("&", queryParams);
            return $"/database/search?{queryString}";
        }

        /// <summary>
        /// Get user's collection
        /// </summary>
        public async Task<string?> GetUserCollectionAsync(string username, int page = 1, int perPage = 100)
        {
            try
            {
                _logger.LogInformation("Fetching collection for user: {Username}, page: {Page}", username, page);
                perPage = Math.Clamp(perPage, 1, 100);
                var requestUri = $"/users/{Uri.EscapeDataString(username)}/collection/folders/0/releases?page={page}&per_page={perPage}";
                var content = await ExecuteGetAsync(requestUri);
                if (content != null)
                    _logger.LogInformation("Successfully fetched collection page {Page} for user: {Username}", page, username);
                return content;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error when fetching collection for user: {Username}", username);
                throw new Exception($"Failed to connect to Discogs API: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching collection for user: {Username}", username);
                throw;
            }
        }
    }
}
