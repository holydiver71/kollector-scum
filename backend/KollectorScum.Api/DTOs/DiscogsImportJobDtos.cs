namespace KollectorScum.Api.DTOs
{
    /// <summary>
    /// Represents the client-facing status for a Discogs import job.
    /// </summary>
    public class DiscogsImportJobStatusDto
    {
        /// <summary>
        /// Gets or sets the external job identifier.
        /// </summary>
        public Guid JobId { get; set; }

        /// <summary>
        /// Gets or sets the current job status.
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the total number of releases in the Discogs collection.
        /// </summary>
        public int TotalReleases { get; set; }

        /// <summary>
        /// Gets or sets the effective total used for progress.
        /// </summary>
        public int EffectiveTotal { get; set; }

        /// <summary>
        /// Gets or sets the number of imported releases.
        /// </summary>
        public int Imported { get; set; }

        /// <summary>
        /// Gets or sets the number of skipped releases.
        /// </summary>
        public int Skipped { get; set; }

        /// <summary>
        /// Gets or sets the number of failed releases.
        /// </summary>
        public int Failed { get; set; }

        /// <summary>
        /// Gets or sets whether the job has completed.
        /// </summary>
        public bool Completed { get; set; }

        /// <summary>
        /// Gets or sets whether the completed job succeeded.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the aggregated error messages.
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// Gets or sets a summary error message.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the import duration once complete.
        /// </summary>
        public TimeSpan? Duration { get; set; }

        /// <summary>
        /// Gets or sets when the job was created.
        /// </summary>
        public DateTime CreatedAtUtc { get; set; }

        /// <summary>
        /// Gets or sets when the job last changed.
        /// </summary>
        public DateTime LastUpdatedUtc { get; set; }
    }
}