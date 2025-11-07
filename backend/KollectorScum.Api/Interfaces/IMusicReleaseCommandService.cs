using KollectorScum.Api.DTOs;
using KollectorScum.Api.Models;

namespace KollectorScum.Api.Interfaces
{
    /// <summary>
    /// Service interface for music release write operations (commands)
    /// </summary>
    public interface IMusicReleaseCommandService
    {
        /// <summary>
        /// Creates a new music release with validation and duplicate checking
        /// Returns a Result indicating success or failure with appropriate error information
        /// </summary>
        Task<Result<CreateMusicReleaseResponseDto>> CreateMusicReleaseAsync(CreateMusicReleaseDto createDto);

        /// <summary>
        /// Updates an existing music release with validation
        /// Returns a Result indicating success or failure with appropriate error information
        /// </summary>
        Task<Result<MusicReleaseDto>> UpdateMusicReleaseAsync(int id, UpdateMusicReleaseDto updateDto);

        /// <summary>
        /// Deletes a music release by ID
        /// Returns a Result indicating success or failure with appropriate error information
        /// </summary>
        Task<Result<bool>> DeleteMusicReleaseAsync(int id);
    }
}
