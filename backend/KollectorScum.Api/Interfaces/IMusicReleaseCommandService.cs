using KollectorScum.Api.DTOs;

namespace KollectorScum.Api.Interfaces
{
    /// <summary>
    /// Service interface for music release write operations (commands)
    /// </summary>
    public interface IMusicReleaseCommandService
    {
        /// <summary>
        /// Creates a new music release with validation and duplicate checking
        /// </summary>
        Task<CreateMusicReleaseResponseDto> CreateMusicReleaseAsync(CreateMusicReleaseDto createDto);

        /// <summary>
        /// Updates an existing music release with validation
        /// </summary>
        Task<MusicReleaseDto?> UpdateMusicReleaseAsync(int id, UpdateMusicReleaseDto updateDto);

        /// <summary>
        /// Deletes a music release by ID
        /// </summary>
        Task<bool> DeleteMusicReleaseAsync(int id);
    }
}
