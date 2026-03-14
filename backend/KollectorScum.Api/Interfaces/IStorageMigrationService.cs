namespace KollectorScum.Api.Interfaces
{
    /// <summary>
    /// Service for migrating local cover art files to cloud storage.
    /// </summary>
    public interface IStorageMigrationService
    {
        /// <summary>
        /// Migrates cover art files from the legacy flat-file structure to the
        /// multi-tenant R2 storage structure.
        /// </summary>
        /// <param name="releaseId">
        /// When provided, only the specified release is migrated.
        /// When null, all eligible releases are migrated.
        /// </param>
        /// <returns>A result summary with counts and errors.</returns>
        Task<StorageMigrationResult> MigrateLocalStorageAsync(int? releaseId = null);
    }

    /// <summary>
    /// Summary of a local-storage migration run.
    /// </summary>
    public class StorageMigrationResult
    {
        /// <summary>Total releases considered for migration.</summary>
        public int TotalConsidered { get; set; }

        /// <summary>Number of releases successfully migrated.</summary>
        public int MigratedCount { get; set; }

        /// <summary>Number of releases skipped (already migrated or invalid).</summary>
        public int SkippedCount { get; set; }

        /// <summary>Error messages (capped at 10).</summary>
        public List<string> Errors { get; set; } = new();
    }
}
