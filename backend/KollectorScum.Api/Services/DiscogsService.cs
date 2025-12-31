using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;

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
    /// Orchestrates HTTP client and response mapper
    /// </summary>
    public class DiscogsService : IDiscogsService
    {
        private readonly IDiscogsHttpClient _httpClient;
        private readonly IDiscogsResponseMapper _mapper;
        private readonly ILogger<DiscogsService> _logger;

        public DiscogsService(
            IDiscogsHttpClient httpClient,
            IDiscogsResponseMapper mapper,
            ILogger<DiscogsService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
                _logger.LogInformation("Searching Discogs: CatalogNumber={CatalogNumber}, Format={Format}, Country={Country}, Year={Year}",
                    catalogNumber, format, country, year);

                var jsonResponse = await _httpClient.SearchReleasesAsync(catalogNumber, format, country, year);
                var results = _mapper.MapSearchResults(jsonResponse);

                _logger.LogInformation("Found {Count} results for catalog number: {CatalogNumber}", results.Count, catalogNumber);

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DiscogsService.SearchByCatalogNumberAsync for catalog number: {CatalogNumber}", catalogNumber);
                throw;
            }
        }

        /// <summary>
        /// Generic search for releases with various parameters
        /// </summary>
        public async Task<List<DiscogsSearchResultDto>> SearchGenericAsync(
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

                var jsonResponse = await _httpClient.SearchGenericAsync(query, type, genre, style, country, year, format);
                var results = _mapper.MapSearchResults(jsonResponse);

                _logger.LogInformation("Found {Count} results for generic search", results.Count);

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DiscogsService.SearchGenericAsync");
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
                _logger.LogInformation("Getting Discogs release details for ID: {ReleaseId}", releaseId);

                var jsonResponse = await _httpClient.GetReleaseDetailsAsync(releaseId);
                var releaseDto = _mapper.MapReleaseDetails(jsonResponse);

                if (releaseDto != null)
                {
                    _logger.LogInformation("Successfully retrieved release details for ID: {ReleaseId}", releaseId);
                }
                else
                {
                    _logger.LogWarning("No release details found for ID: {ReleaseId}", releaseId);
                }

                return releaseDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DiscogsService.GetReleaseDetailsAsync for release ID: {ReleaseId}", releaseId);
                throw;
            }
        }

        /// <summary>
        /// Get user's collection with pagination
        /// </summary>
        public async Task<DiscogsCollectionResponseDto?> GetUserCollectionAsync(string username, int page = 1, int perPage = 100)
        {
            try
            {
                _logger.LogInformation("Getting collection for user: {Username}, page: {Page}", username, page);

                var jsonResponse = await _httpClient.GetUserCollectionAsync(username, page, perPage);
                var collectionDto = _mapper.MapCollectionResponse(jsonResponse);

                if (collectionDto != null)
                {
                    _logger.LogInformation("Successfully retrieved collection for user: {Username}, {Count} releases", 
                        username, collectionDto.Releases.Count);
                }
                else
                {
                    _logger.LogWarning("No collection data found for user: {Username}", username);
                }

                return collectionDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DiscogsService.GetUserCollectionAsync for user: {Username}", username);
                throw;
            }
        }
    }
}

