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
        /// Get detailed information about a specific release
        /// </summary>
        /// <param name="releaseId">The Discogs release ID</param>
        /// <returns>Full release details</returns>
        Task<DiscogsReleaseDto?> GetReleaseDetailsAsync(string releaseId);
    }
}
