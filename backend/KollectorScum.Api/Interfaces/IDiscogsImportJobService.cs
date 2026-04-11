using KollectorScum.Api.DTOs;

namespace KollectorScum.Api.Interfaces
{
    /// <summary>
    /// Coordinates Discogs import job submission and status lookup.
    /// </summary>
    public interface IDiscogsImportJobService
    {
        /// <summary>
        /// Enqueues a Discogs collection import for background processing.
        /// </summary>
        /// <param name="username">Discogs username.</param>
        /// <param name="userId">Authenticated user identifier.</param>
        /// <param name="personalToken">Optional Discogs personal access token for private collections.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<DiscogsImportJobStatusDto> EnqueueImportAsync(string username, Guid userId, string? personalToken = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current status for a specific Discogs import job.
        /// </summary>
        /// <param name="jobId">External job identifier.</param>
        /// <param name="userId">Authenticated user identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<DiscogsImportJobStatusDto?> GetJobStatusAsync(Guid jobId, Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the latest Discogs import job for the current user.
        /// </summary>
        /// <param name="userId">Authenticated user identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<DiscogsImportJobStatusDto?> GetLatestJobStatusAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}