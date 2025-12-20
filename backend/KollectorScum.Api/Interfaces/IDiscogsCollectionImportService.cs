namespace KollectorScum.Api.Interfaces
{
    /// <summary>
    /// Service interface for importing Discogs collections
    /// </summary>
    public interface IDiscogsCollectionImportService
    {
        /// <summary>
        /// Import user's collection from Discogs
        /// </summary>
        /// <param name="username">Discogs username</param>
        /// <param name="userId">User ID who owns the collection</param>
        /// <returns>Import result with statistics</returns>
        Task<DiscogsImportResult> ImportCollectionAsync(string username, Guid userId);
    }

    /// <summary>
    /// Result of a Discogs collection import operation
    /// </summary>
    public class DiscogsImportResult
    {
        /// <summary>
        /// Whether the import was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Total number of releases in the Discogs collection
        /// </summary>
        public int TotalReleases { get; set; }

        /// <summary>
        /// Number of releases successfully imported
        /// </summary>
        public int ImportedReleases { get; set; }

        /// <summary>
        /// Number of releases that were skipped (already exist)
        /// </summary>
        public int SkippedReleases { get; set; }

        /// <summary>
        /// Number of releases that failed to import
        /// </summary>
        public int FailedReleases { get; set; }

        /// <summary>
        /// List of error messages
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// Time taken for the import
        /// </summary>
        public TimeSpan Duration { get; set; }
    }
}
