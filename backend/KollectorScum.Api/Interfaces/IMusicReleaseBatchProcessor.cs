using KollectorScum.Api.DTOs;

namespace KollectorScum.Api.Interfaces
{
    /// <summary>
    /// Interface for processing batches of music release imports
    /// </summary>
    public interface IMusicReleaseBatchProcessor
    {
        /// <summary>
        /// Processes a batch of music release imports
        /// </summary>
        /// <param name="releases">Batch of releases to import</param>
        /// <returns>Number of releases successfully imported</returns>
        Task<int> ProcessBatchAsync(List<MusicReleaseImportDto> releases);

        /// <summary>
        /// Updates UPC values for existing releases
        /// </summary>
        /// <param name="releases">Releases with UPC values to update</param>
        /// <returns>Number of releases updated</returns>
        Task<int> UpdateUpcBatchAsync(List<MusicReleaseImportDto> releases);

        /// <summary>
        /// Validates that required lookup data exists for a batch
        /// </summary>
        /// <returns>Validation result with details</returns>
        Task<(bool IsValid, List<string> Errors)> ValidateLookupDataAsync();
    }
}
