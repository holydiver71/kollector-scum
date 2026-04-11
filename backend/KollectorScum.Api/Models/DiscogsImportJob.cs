using System.ComponentModel.DataAnnotations;

namespace KollectorScum.Api.Models
{
    /// <summary>
    /// Represents a durable Discogs collection import job.
    /// </summary>
    public class DiscogsImportJob : IUserOwnedEntity
    {
        /// <summary>
        /// Gets or sets the database identifier for the job.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the external job identifier returned to clients.
        /// </summary>
        public Guid JobId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Gets or sets the user identifier that owns this import.
        /// </summary>
        [Required]
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the Discogs username to import from.
        /// </summary>
        [Required]
        [StringLength(200)]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user's personal Discogs OAuth/personal-access token,
        /// used to authenticate collection requests on behalf of the owner.
        /// Stored encrypted at rest; <c>null</c> means fall back to the global app token.
        /// </summary>
        [StringLength(200)]
        public string? PersonalToken { get; set; }

        /// <summary>
        /// Gets or sets the current job status.
        /// </summary>
        public DiscogsImportJobStatus Status { get; set; } = DiscogsImportJobStatus.Queued;

        /// <summary>
        /// Gets or sets the total number of releases reported by Discogs.
        /// </summary>
        public int TotalReleases { get; set; }

        /// <summary>
        /// Gets or sets the effective total used for progress calculations.
        /// </summary>
        public int EffectiveTotal { get; set; }

        /// <summary>
        /// Gets or sets the number of imported releases.
        /// </summary>
        public int ImportedReleases { get; set; }

        /// <summary>
        /// Gets or sets the number of skipped releases.
        /// </summary>
        public int SkippedReleases { get; set; }

        /// <summary>
        /// Gets or sets the number of failed releases.
        /// </summary>
        public int FailedReleases { get; set; }

        /// <summary>
        /// Gets or sets whether the completed job was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets a summary error message for terminal failures.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the serialized error list captured during the import.
        /// </summary>
        public string? ErrorsJson { get; set; }

        /// <summary>
        /// Gets or sets when the job was created.
        /// </summary>
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets when the job started processing.
        /// </summary>
        public DateTime? StartedAtUtc { get; set; }

        /// <summary>
        /// Gets or sets when the job completed.
        /// </summary>
        public DateTime? CompletedAtUtc { get; set; }

        /// <summary>
        /// Gets or sets when the job was last updated.
        /// </summary>
        public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the total import duration once complete.
        /// </summary>
        public TimeSpan? Duration { get; set; }
    }
}