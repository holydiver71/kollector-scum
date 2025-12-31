using KollectorScum.Api.DTOs;

namespace KollectorScum.Api.Interfaces
{
    /// <summary>
    /// Service interface for integrating with the Discogs API
    /// </summary>
    public interface IDiscogsService
    {
        /// <summary>
        /// Search for releases by catalog number
        /// </summary>
        /// <param name="catalogNumber">The catalog number to search for</param>
        /// <param name="format">Optional format filter</param>
        /// <param name="country">Optional country filter</param>
        /// <param name="year">Optional year filter</param>
        /// <returns>List of matching releases</returns>
        Task<List<DiscogsSearchResultDto>> SearchByCatalogNumberAsync(
            string catalogNumber, 
            string? format = null, 
            string? country = null, 
            int? year = null);

        /// <summary>
        /// Generic search for releases with various parameters
        /// </summary>
        /// <param name="query">General search query</param>
        /// <param name="type">Type of item (release, master, artist, label)</param>
        /// <param name="genre">Genre filter</param>
        /// <param name="style">Style filter</param>
        /// <param name="country">Country filter</param>
        /// <param name="year">Year filter</param>
        /// <param name="format">Format filter</param>
        /// <returns>List of matching releases</returns>
        Task<List<DiscogsSearchResultDto>> SearchGenericAsync(
            string? query = null,
            string? type = null,
            string? genre = null,
            string? style = null,
            string? country = null,
            int? year = null,
            string? format = null);

        /// <summary>
        /// Get detailed information about a specific release
        /// </summary>
        /// <param name="releaseId">The Discogs release ID</param>
        /// <returns>Full release details</returns>
        Task<DiscogsReleaseDto?> GetReleaseDetailsAsync(string releaseId);

        /// <summary>
        /// Get user's collection with pagination
        /// </summary>
        /// <param name="username">Discogs username</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="perPage">Items per page (default: 100, max: 100)</param>
        /// <returns>Collection response with releases and pagination info</returns>
        Task<DiscogsCollectionResponseDto?> GetUserCollectionAsync(string username, int page = 1, int perPage = 100);
    }
}
