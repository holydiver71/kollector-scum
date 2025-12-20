namespace KollectorScum.Api.Interfaces
{
    /// <summary>
    /// Interface for HTTP communication with Discogs API
    /// </summary>
    public interface IDiscogsHttpClient
    {
        /// <summary>
        /// Search for releases with the specified parameters
        /// </summary>
        /// <param name="catalogNumber">Catalog number to search for</param>
        /// <param name="format">Optional format filter</param>
        /// <param name="country">Optional country filter</param>
        /// <param name="year">Optional year filter</param>
        /// <returns>Raw JSON response string</returns>
        Task<string?> SearchReleasesAsync(string catalogNumber, string? format = null, string? country = null, int? year = null);

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
        /// <returns>Raw JSON response string</returns>
        Task<string?> SearchGenericAsync(string? query = null, string? type = null, string? genre = null, string? style = null, string? country = null, int? year = null, string? format = null);

        /// <summary>
        /// Get detailed information about a specific release
        /// </summary>
        /// <param name="releaseId">The Discogs release ID</param>
        /// <returns>Raw JSON response string</returns>
        Task<string?> GetReleaseDetailsAsync(string releaseId);

        /// <summary>
        /// Get user's collection
        /// </summary>
        /// <param name="username">Discogs username</param>
        /// <param name="page">Page number for pagination (default: 1)</param>
        /// <param name="perPage">Items per page (default: 100, max: 100)</param>
        /// <returns>Raw JSON response string</returns>
        Task<string?> GetUserCollectionAsync(string username, int page = 1, int perPage = 100);
    }
}
