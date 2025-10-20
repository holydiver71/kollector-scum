using KollectorScum.Api.DTOs;
using KollectorScum.Api.Models;

namespace KollectorScum.Api.Interfaces
{
    /// <summary>
    /// Service interface for music release business logic
    /// </summary>
    public interface IMusicReleaseService
    {
        Task<PagedResult<MusicReleaseSummaryDto>> GetMusicReleasesAsync(
            string? search, int? artistId, int? genreId, int? labelId, 
            int? countryId, int? formatId, bool? live, int? yearFrom, 
            int? yearTo, int page, int pageSize);

        Task<MusicReleaseDto?> GetMusicReleaseAsync(int id);

        Task<List<SearchSuggestionDto>> GetSearchSuggestionsAsync(string query, int limit);

        Task<CollectionStatisticsDto> GetCollectionStatisticsAsync();

        Task<CreateMusicReleaseResponseDto> CreateMusicReleaseAsync(CreateMusicReleaseDto createDto);

        Task<MusicReleaseDto?> UpdateMusicReleaseAsync(int id, UpdateMusicReleaseDto updateDto);

        Task<bool> DeleteMusicReleaseAsync(int id);
    }
}
