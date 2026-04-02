using System.Threading.Channels;
using KollectorScum.Api.Interfaces;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// In-process channel-backed queue for Discogs import jobs.
    /// </summary>
    public class DiscogsImportJobQueue : IDiscogsImportJobQueue
    {
        private readonly Channel<Guid> _channel;

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscogsImportJobQueue"/> class.
        /// </summary>
        public DiscogsImportJobQueue()
        {
            _channel = Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = false,
            });
        }

        /// <inheritdoc />
        public async ValueTask QueueAsync(Guid jobId, CancellationToken cancellationToken = default)
        {
            await _channel.Writer.WriteAsync(jobId, cancellationToken);
        }

        /// <inheritdoc />
        public async ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken)
        {
            return await _channel.Reader.ReadAsync(cancellationToken);
        }
    }
}