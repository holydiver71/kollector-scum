using KollectorScum.Api.DTOs;

namespace KollectorScum.Api.Interfaces
{
    /// <summary>
    /// Interface for mapping Discogs API responses to DTOs
    /// </summary>
    public interface IDiscogsResponseMapper
    {
        /// <summary>
        /// Map search results JSON to DTOs
        /// </summary>
        /// <param name="jsonResponse">Raw JSON response from Discogs search</param>
        /// <returns>List of search result DTOs</returns>
        List<DiscogsSearchResultDto> MapSearchResults(string? jsonResponse);

        /// <summary>
        /// Map release details JSON to DTO
        /// </summary>
        /// <param name="jsonResponse">Raw JSON response from Discogs release endpoint</param>
        /// <returns>Release detail DTO or null if mapping fails</returns>
        DiscogsReleaseDto? MapReleaseDetails(string? jsonResponse);
    }
}
