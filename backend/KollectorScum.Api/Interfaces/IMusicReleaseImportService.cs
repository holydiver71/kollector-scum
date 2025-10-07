using KollectorScum.Api.Models;

namespace KollectorScum.Api.Interfaces
{
    /// <summary>
    /// Interface for importing MusicRelease data from JSON files
    /// </summary>
    public interface IMusicReleaseImportService
    {
        /// <summary>
        /// Imports music releases from JSON file
        /// </summary>
        /// <returns>Number of releases imported</returns>
        Task<int> ImportMusicReleasesAsync();

        /// <summary>
        /// Imports a batch of music releases
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
        /// Validates that all required lookup data exists for import
        /// </summary>
        /// <returns>Validation result with details</returns>
        Task<(bool IsValid, List<string> Errors)> ValidateLookupDataAsync();

        /// <summary>
        /// Gets import progress information
        /// </summary>
        /// <returns>Import progress details</returns>
        Task<ImportProgressInfo> GetImportProgressAsync();
    }

    /// <summary>
    /// Information about import progress
    /// </summary>
    public class ImportProgressInfo
    {
        public int TotalRecords { get; set; }
        public int ImportedRecords { get; set; }
        public double ProgressPercentage => TotalRecords > 0 ? (double)ImportedRecords / TotalRecords * 100 : 0;
        public List<string> Errors { get; set; } = new();
        public DateTime? LastImportDate { get; set; }
    }
}
