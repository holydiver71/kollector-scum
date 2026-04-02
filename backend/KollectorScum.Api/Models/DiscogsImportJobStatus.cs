namespace KollectorScum.Api.Models
{
    /// <summary>
    /// Represents the lifecycle state of a Discogs import job.
    /// </summary>
    public enum DiscogsImportJobStatus
    {
        /// <summary>
        /// The job has been accepted and is waiting to be processed.
        /// </summary>
        Queued = 0,

        /// <summary>
        /// The job is currently being processed.
        /// </summary>
        Running = 1,

        /// <summary>
        /// The job completed successfully.
        /// </summary>
        Succeeded = 2,

        /// <summary>
        /// The job completed with a terminal failure.
        /// </summary>
        Failed = 3,
    }
}