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
        /// Gets paginated music releases with optional filters
        /// </summary>
        Task<PagedResult<MusicReleaseSummaryDto>> GetMusicReleasesAsync(
            string? search, int? artistId, int? genreId, int? labelId, 
            int? countryId, int? formatId, bool? live, int? yearFrom, 
            int? yearTo, int page, int pageSize);

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
    }
}
