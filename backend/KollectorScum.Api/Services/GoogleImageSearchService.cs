using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// Implementation of image search using Google Custom Search API
    /// </summary>
    public class GoogleImageSearchService : IImageSearchService
    {
        private readonly HttpClient _httpClient;
        private readonly GoogleSearchSettings _settings;
        private readonly ILogger<GoogleImageSearchService> _logger;
        private static readonly Regex _fileExtensionRegex = new(@"\.(jpg|jpeg|png|gif|webp|bmp)(\?.*)?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public GoogleImageSearchService(
            HttpClient httpClient, 
            IOptions<GoogleSearchSettings> settings, 
            ILogger<GoogleImageSearchService> logger)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _logger = logger;
            
            // Configure HTTP client
            _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
        }

        public async Task<IEnumerable<ImageSearchResultDto>> SearchImagesAsync(string artist, string album, string? year = null)
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(artist) || string.IsNullOrWhiteSpace(album))
                {
                    _logger.LogWarning("Invalid search parameters: Artist='{Artist}', Album='{Album}'", artist, album);
                    return Enumerable.Empty<ImageSearchResultDto>();
                }

                // Check service availability
                if (!await IsServiceAvailableAsync())
                {
                    _logger.LogWarning("Google Image Search service is not available");
                    return Enumerable.Empty<ImageSearchResultDto>();
                }

                // Build search query
                var query = BuildSearchQuery(artist, album, year);
                _logger.LogInformation("Searching for images: {Query}", query);

                // Build API request URL
                var requestUrl = BuildApiUrl(query);

                // Make API request
                var response = await _httpClient.GetAsync(requestUrl);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Google Custom Search API returned {StatusCode}: {ReasonPhrase}", 
                        response.StatusCode, response.ReasonPhrase);
                    return Enumerable.Empty<ImageSearchResultDto>();
                }

                var jsonContent = await response.Content.ReadAsStringAsync();
                var searchResponse = JsonSerializer.Deserialize<GoogleSearchResponse>(jsonContent);

                if (searchResponse?.Items == null || !searchResponse.Items.Any())
                {
                    _logger.LogInformation("No images found for query: {Query}", query);
                    return Enumerable.Empty<ImageSearchResultDto>();
                }

                // Convert to our DTOs
                var results = searchResponse.Items
                    .Where(item => IsValidImageResult(item))
                    .Select(ConvertToImageSearchResult)
                    .Where(result => result != null)
                    .Cast<ImageSearchResultDto>()
                    .ToList();

                _logger.LogInformation("Found {Count} valid images for query: {Query}", results.Count, query);
                return results;
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError("Google Image Search request timed out after {Timeout} seconds", _settings.TimeoutSeconds);
                return Enumerable.Empty<ImageSearchResultDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for images with query: {Artist} {Album} {Year}", artist, album, year);
                return Enumerable.Empty<ImageSearchResultDto>();
            }
        }

        public async Task<bool> IsServiceAvailableAsync()
        {
            try
            {
                // Check if we have required configuration
                if (string.IsNullOrWhiteSpace(_settings.ApiKey) || string.IsNullOrWhiteSpace(_settings.SearchEngineId))
                {
                    _logger.LogWarning("Google Custom Search API is not configured. Missing ApiKey or SearchEngineId.");
                    return false;
                }

                // Test with a simple query to verify the service is working
                var testUrl = $"{_settings.BaseUrl}?key={_settings.ApiKey}&cx={_settings.SearchEngineId}&q=test&searchType=image&num=1";
                
                var response = await _httpClient.GetAsync(testUrl);
                var isAvailable = response.IsSuccessStatusCode;

                if (!isAvailable)
                {
                    _logger.LogWarning("Google Custom Search API health check failed: {StatusCode}", response.StatusCode);
                }

                return isAvailable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Google Image Search service availability");
                return false;
            }
        }

        private string BuildSearchQuery(string artist, string album, string? year)
        {
            // Clean inputs
            artist = SanitizeSearchInput(artist);
            album = SanitizeSearchInput(album);
            
            var queryParts = new List<string> { artist, album };
            
            if (!string.IsNullOrWhiteSpace(year))
            {
                queryParts.Add(year);
            }

            // Add specific terms for better album cover results
            queryParts.Add("album cover");

            return string.Join(" ", queryParts);
        }

        private string SanitizeSearchInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // Remove or replace special characters that might interfere with search
            var sanitized = input.Trim()
                .Replace("&", "and")
                .Replace("\"", "")
                .Replace("'", "");

            // Remove extra whitespace
            return Regex.Replace(sanitized, @"\s+", " ");
        }

        private string BuildApiUrl(string query)
        {
            var queryParams = HttpUtility.ParseQueryString(string.Empty);
            queryParams["key"] = _settings.ApiKey;
            queryParams["cx"] = _settings.SearchEngineId;
            queryParams["q"] = query;
            queryParams["searchType"] = "image";
            queryParams["num"] = Math.Min(_settings.MaxResults, 10).ToString(); // Google API max is 10 per request
            queryParams["imgSize"] = "medium"; // Prefer medium-sized images
            queryParams["imgType"] = "photo"; // Prefer photos over graphics
            queryParams["safe"] = "active"; // Enable safe search

            return $"{_settings.BaseUrl}?{queryParams}";
        }

        private bool IsValidImageResult(GoogleSearchItem item)
        {
            if (string.IsNullOrWhiteSpace(item.Link))
                return false;

            // Check if URL looks like an image
            if (!_fileExtensionRegex.IsMatch(item.Link))
                return false;

            // Basic size validation - prefer images that aren't too small
            if (item.Image?.Width < 200 || item.Image?.Height < 200)
                return false;

            return true;
        }

        private ImageSearchResultDto? ConvertToImageSearchResult(GoogleSearchItem item)
        {
            try
            {
                return new ImageSearchResultDto
                {
                    ImageUrl = item.Link,
                    ThumbnailUrl = item.Image?.ThumbnailLink,
                    Width = item.Image?.Width,
                    Height = item.Image?.Height,
                    Title = item.Title,
                    SourceUrl = item.Image?.ContextLink,
                    FileSize = item.Image?.ByteSize,
                    Format = ExtractFileFormat(item.Link)
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error converting search result: {Link}", item.Link);
                return null;
            }
        }

        private string? ExtractFileFormat(string url)
        {
            var match = _fileExtensionRegex.Match(url);
            return match.Success ? match.Groups[1].Value.ToLowerInvariant() : null;
        }

        #region Google API Response Models

        private class GoogleSearchResponse
        {
            public GoogleSearchItem[]? Items { get; set; }
        }

        private class GoogleSearchItem
        {
            public string Link { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public GoogleImageInfo? Image { get; set; }
        }

        private class GoogleImageInfo
        {
            public string? ContextLink { get; set; }
            public int? Width { get; set; }
            public int? Height { get; set; }
            public long? ByteSize { get; set; }
            public string? ThumbnailLink { get; set; }
        }

        #endregion
    }
}