using KollectorScum.Api.DTOs;

namespace KollectorScum.Api.Interfaces
{
    /// <summary>
    /// Interface for music release search operations
    /// </summary>
    public interface IMusicReleaseSearchService
    {
        /// <summary>
        /// Get search suggestions for autocomplete
        /// </summary>
        Task<List<SearchSuggestionDto>> GetSearchSuggestionsAsync(string query, int limit);
    }
}
