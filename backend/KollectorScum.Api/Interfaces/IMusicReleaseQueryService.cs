using KollectorScum.Api.DTOs;
using KollectorScum.Api.Models;

namespace KollectorScum.Api.Interfaces
{
    /// <summary>
    /// Service interface for music release read operations (queries)
    /// </summary>
    public interface IMusicReleaseQueryService
    {
        /// <summary>
        /// Gets paginated music releases with optional filters using query parameters object
        /// </summary>
        Task<PagedResult<MusicReleaseSummaryDto>> GetMusicReleasesAsync(MusicReleaseQueryParameters parameters);

        /// <summary>
        /// Gets a single music release by ID with full details
        /// </summary>
        Task<MusicReleaseDto?> GetMusicReleaseAsync(int id);

        /// <summary>
        /// Gets search suggestions for autocomplete
        /// </summary>
        Task<List<SearchSuggestionDto>> GetSearchSuggestionsAsync(string query, int limit);

        /// <summary>
        /// Gets collection statistics (delegated to statistics service)
        /// </summary>
        Task<CollectionStatisticsDto> GetCollectionStatisticsAsync();

        /// <summary>
        /// Gets the ID of a random music release from the collection
        /// </summary>
        /// <returns>Random release ID or null if collection is empty</returns>
        Task<int?> GetRandomReleaseIdAsync();
    }
}
