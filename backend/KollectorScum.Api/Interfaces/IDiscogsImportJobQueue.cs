namespace KollectorScum.Api.Interfaces
{
    /// <summary>
    /// Queue abstraction for background Discogs import jobs.
    /// </summary>
    public interface IDiscogsImportJobQueue
    {
        /// <summary>
        /// Queues a Discogs import job for background processing.
        /// </summary>
        /// <param name="jobId">External job identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        ValueTask QueueAsync(Guid jobId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Dequeues the next Discogs import job.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken);
    }
}