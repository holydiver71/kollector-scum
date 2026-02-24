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

        /// <summary>
        /// Resolves a raw stored image value (filename or path) to the appropriate public URL.
        /// For R2-backed environments this returns a full HTTPS URL; for local storage it returns
        /// the relative /cover-art/{userId}/{filename} path.
        /// </summary>
        /// <param name="imageValue">The raw value stored in the Images JSON column</param>
        /// <param name="userId">The owner of the release (used to build the storage path)</param>
        /// <returns>A resolved URL/path, or null when <paramref name="imageValue"/> is empty</returns>
        string? ResolveImageUrl(string? imageValue, Guid userId);
    }
}
