using System.Text.Json;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// Executes queued Discogs import jobs in the background.
    /// </summary>
    public class DiscogsImportBackgroundService : BackgroundService
    {
        private readonly IDiscogsImportJobQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DiscogsImportBackgroundService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscogsImportBackgroundService"/> class.
        /// </summary>
        public DiscogsImportBackgroundService(
            IDiscogsImportJobQueue queue,
            IServiceScopeFactory scopeFactory,
            ILogger<DiscogsImportBackgroundService> logger)
        {
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await RecoverPendingJobsAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                var jobId = await _queue.DequeueAsync(stoppingToken);
                await ProcessJobAsync(jobId, stoppingToken);
            }
        }

        private async Task RecoverPendingJobsAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var jobRepository = unitOfWork.GetRepository<DiscogsImportJob>();

            var pendingJobs = await jobRepository.Query()
                .Where(job => job.Status == DiscogsImportJobStatus.Queued || job.Status == DiscogsImportJobStatus.Running)
                .OrderBy(job => job.CreatedAtUtc)
                .ToListAsync(cancellationToken);

            if (pendingJobs.Count == 0)
            {
                return;
            }

            foreach (var job in pendingJobs)
            {
                if (job.Status == DiscogsImportJobStatus.Running)
                {
                    job.Status = DiscogsImportJobStatus.Queued;
                    job.StartedAtUtc = null;
                    job.LastUpdatedUtc = DateTime.UtcNow;
                    jobRepository.Update(job);
                }
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            foreach (var job in pendingJobs)
            {
                await _queue.QueueAsync(job.JobId, cancellationToken);
            }

            _logger.LogInformation("Recovered {PendingJobCount} pending Discogs import jobs", pendingJobs.Count);
        }

        private async Task ProcessJobAsync(Guid jobId, CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var jobRepository = unitOfWork.GetRepository<DiscogsImportJob>();
            var importService = scope.ServiceProvider.GetRequiredService<IDiscogsCollectionImportService>();

            var job = await jobRepository.Query()
                .FirstOrDefaultAsync(item => item.JobId == jobId, cancellationToken);

            if (job == null)
            {
                _logger.LogWarning("Discarding unknown Discogs import job {JobId}", jobId);
                return;
            }

            if (job.Status == DiscogsImportJobStatus.Succeeded || job.Status == DiscogsImportJobStatus.Failed)
            {
                _logger.LogDebug("Skipping Discogs import job {JobId} because it already completed", jobId);
                return;
            }

            try
            {
                job.Status = DiscogsImportJobStatus.Running;
                job.StartedAtUtc = DateTime.UtcNow;
                job.LastUpdatedUtc = DateTime.UtcNow;
                jobRepository.Update(job);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Starting Discogs import job {JobId} for user {UserId}", job.JobId, job.UserId);

                var result = await importService.ImportCollectionAsync(job.Username, job.UserId, cancellationToken);

                job.TotalReleases = result.TotalReleases;
                job.EffectiveTotal = result.TotalReleases;
                job.ImportedReleases = result.ImportedReleases;
                job.SkippedReleases = result.SkippedReleases;
                job.FailedReleases = result.FailedReleases;
                job.Success = result.Success;
                job.ErrorMessage = result.Errors.FirstOrDefault();
                job.ErrorsJson = result.Errors.Count > 0 ? JsonSerializer.Serialize(result.Errors) : null;
                job.CompletedAtUtc = DateTime.UtcNow;
                job.LastUpdatedUtc = DateTime.UtcNow;
                job.Duration = result.Duration;
                job.Status = result.Success ? DiscogsImportJobStatus.Succeeded : DiscogsImportJobStatus.Failed;

                var liveProgress = importService.GetProgress(job.UserId);
                if (liveProgress != null)
                {
                    job.TotalReleases = liveProgress.TotalReleases > 0 ? liveProgress.TotalReleases : job.TotalReleases;
                    job.EffectiveTotal = liveProgress.EffectiveTotal > 0 ? liveProgress.EffectiveTotal : job.EffectiveTotal;
                    job.ImportedReleases = liveProgress.Imported;
                    job.SkippedReleases = liveProgress.Skipped;
                    job.FailedReleases = liveProgress.Failed;
                }

                jobRepository.Update(job);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Completed Discogs import job {JobId} with status {Status}", job.JobId, job.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Discogs import job {JobId} failed", jobId);

                job.Status = DiscogsImportJobStatus.Failed;
                job.Success = false;
                job.ErrorMessage = ex.Message;
                job.ErrorsJson = JsonSerializer.Serialize(new List<string> { ex.Message });
                job.CompletedAtUtc = DateTime.UtcNow;
                job.LastUpdatedUtc = DateTime.UtcNow;

                if (job.StartedAtUtc.HasValue)
                {
                    job.Duration = job.CompletedAtUtc.Value - job.StartedAtUtc.Value;
                }

                jobRepository.Update(job);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }
    }
}