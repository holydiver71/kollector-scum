using KollectorScum.Api.DTOs;

namespace KollectorScum.Api.Interfaces
{
    /// <summary>
    /// Service interface for validating music release operations
    /// </summary>
    public interface IMusicReleaseValidator
    {
        /// <summary>
        /// Validates a music release before creation, including duplicate checks
        /// </summary>
        /// <returns>Tuple with (isValid, errorMessage, duplicates)</returns>
        Task<(bool IsValid, string? ErrorMessage, List<MusicReleaseSummaryDto>? Duplicates)> 
            ValidateCreateAsync(CreateMusicReleaseDto createDto);

        /// <summary>
        /// Validates a music release before update
        /// </summary>
        /// <returns>Tuple with (isValid, errorMessage)</returns>
        Task<(bool IsValid, string? ErrorMessage)> 
            ValidateUpdateAsync(int id, UpdateMusicReleaseDto updateDto);
    }
}
