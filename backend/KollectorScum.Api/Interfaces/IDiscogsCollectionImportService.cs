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
        /// <param name="cancellationToken">Cancellation token for the import operation</param>
        /// <returns>Import result with statistics</returns>
        Task<DiscogsImportResult> ImportCollectionAsync(string username, Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get current import progress snapshot for a user (if an import is running)
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Progress snapshot or null if no import in progress</returns>
        DiscogsImportProgress? GetProgress(Guid userId);
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

    /// <summary>
    /// Lightweight progress snapshot for an in-progress Discogs import
    /// </summary>
    public class DiscogsImportProgress
    {
        public int TotalReleases { get; set; }
        public int EffectiveTotal { get; set; }
        public int Imported { get; set; }
        public int Skipped { get; set; }
        public int Failed { get; set; }
        public bool Completed { get; set; }
        public DateTime LastUpdatedUtc { get; set; }
    }
}
