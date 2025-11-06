using KollectorScum.Api.DTOs;

namespace KollectorScum.Api.Interfaces
{
    /// <summary>
    /// Interface for orchestrating music release imports from JSON files
    /// </summary>
    public interface IMusicReleaseImportOrchestrator
    {
        /// <summary>
        /// Imports all music releases from JSON file
        /// </summary>
        /// <returns>Number of releases imported</returns>
        Task<int> ImportMusicReleasesAsync();

        /// <summary>
        /// Imports a specific batch of music releases
        /// </summary>
        /// <param name="batchSize">Size of batch to import</param>
        /// <param name="skipCount">Number of records to skip</param>
        /// <returns>Number of releases imported in this batch</returns>
        Task<int> ImportMusicReleasesBatchAsync(int batchSize, int skipCount = 0);

        /// <summary>
        /// Gets the total count of music releases in the JSON file
        /// </summary>
        /// <returns>Total count of releases</returns>
        Task<int> GetMusicReleaseCountAsync();

        /// <summary>
        /// Gets import progress information
        /// </summary>
        /// <returns>Import progress details</returns>
        Task<ImportProgressInfo> GetImportProgressAsync();

        /// <summary>
        /// Updates UPC values for existing music releases from JSON file
        /// </summary>
        /// <returns>Number of releases updated</returns>
        Task<int> UpdateUpcValuesAsync();

        /// <summary>
        /// Validates that required lookup data exists for import
        /// </summary>
        /// <returns>Validation result with details</returns>
        Task<(bool IsValid, List<string> Errors)> ValidateLookupDataAsync();
    }
}
