using KollectorScum.Api.DTOs;
using KollectorScum.Api.Models;

namespace KollectorScum.Api.Interfaces
{
    /// <summary>
    /// Service for mapping between MusicRelease entities and DTOs
    /// </summary>
    public interface IMusicReleaseMapperService
    {
        /// <summary>
        /// Maps a MusicRelease entity to a summary DTO
        /// </summary>
        MusicReleaseSummaryDto MapToSummaryDto(MusicRelease musicRelease);

        /// <summary>
        /// Maps a MusicRelease entity to a full DTO with all related data
        /// </summary>
        Task<MusicReleaseDto> MapToFullDtoAsync(MusicRelease musicRelease);
    }
}
