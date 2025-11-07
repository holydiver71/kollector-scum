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
        /// Get detailed information about a specific release
        /// </summary>
        /// <param name="releaseId">The Discogs release ID</param>
        /// <returns>Raw JSON response string</returns>
        Task<string?> GetReleaseDetailsAsync(string releaseId);
    }
}
