using System.Text.Json;
using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// Handles Discogs import job submission and status projection.
    /// </summary>
    public class DiscogsImportJobService : IDiscogsImportJobService
    {
        private readonly IRepository<DiscogsImportJob> _jobRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDiscogsImportJobQueue _queue;
        private readonly IDiscogsCollectionImportService _importService;
        private readonly ILogger<DiscogsImportJobService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscogsImportJobService"/> class.
        /// </summary>
        public DiscogsImportJobService(
            IRepository<DiscogsImportJob> jobRepository,
            IUnitOfWork unitOfWork,
            IDiscogsImportJobQueue queue,
            IDiscogsCollectionImportService importService,
            ILogger<DiscogsImportJobService> logger)
        {
            _jobRepository = jobRepository ?? throw new ArgumentNullException(nameof(jobRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
            _importService = importService ?? throw new ArgumentNullException(nameof(importService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<DiscogsImportJobStatusDto> EnqueueImportAsync(string username, Guid userId, string? personalToken = null, CancellationToken cancellationToken = default)
        {
            var normalizedUsername = username.Trim();

            var hasActiveJob = await _jobRepository.Query()
                .AnyAsync(job => job.UserId == userId &&
                    (job.Status == DiscogsImportJobStatus.Queued || job.Status == DiscogsImportJobStatus.Running), cancellationToken);

            if (hasActiveJob)
            {
                throw new InvalidOperationException("A Discogs import is already queued or running for this user.");
            }

            var job = new DiscogsImportJob
            {
                JobId = Guid.NewGuid(),
                UserId = userId,
                Username = normalizedUsername,
                PersonalToken = personalToken,
                Status = DiscogsImportJobStatus.Queued,
                CreatedAtUtc = DateTime.UtcNow,
                LastUpdatedUtc = DateTime.UtcNow,
            };

            await _jobRepository.AddAsync(job, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _queue.QueueAsync(job.JobId, cancellationToken);

            _logger.LogInformation("Queued Discogs import job {JobId} for user {UserId}", job.JobId, userId);

            return MapJob(job, _importService.GetProgress(userId));
        }

        /// <inheritdoc />
        public async Task<DiscogsImportJobStatusDto?> GetJobStatusAsync(Guid jobId, Guid userId, CancellationToken cancellationToken = default)
        {
            var job = await _jobRepository.Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.JobId == jobId && item.UserId == userId, cancellationToken);

            if (job == null)
            {
                return null;
            }

            return MapJob(job, _importService.GetProgress(userId));
        }

        /// <inheritdoc />
        public async Task<DiscogsImportJobStatusDto?> GetLatestJobStatusAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var job = await _jobRepository.Query()
                .AsNoTracking()
                .Where(item => item.UserId == userId)
                .OrderByDescending(item => item.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (job == null)
            {
                return null;
            }

            return MapJob(job, _importService.GetProgress(userId));
        }

        private static DiscogsImportJobStatusDto MapJob(DiscogsImportJob job, DiscogsImportProgress? liveProgress)
        {
            var errors = DeserializeErrors(job.ErrorsJson);

            var dto = new DiscogsImportJobStatusDto
            {
                JobId = job.JobId,
                Status = job.Status.ToString(),
                TotalReleases = job.TotalReleases,
                EffectiveTotal = job.EffectiveTotal,
                Imported = job.ImportedReleases,
                Skipped = job.SkippedReleases,
                Failed = job.FailedReleases,
                Percentage = job.Status == DiscogsImportJobStatus.Succeeded || job.Status == DiscogsImportJobStatus.Failed
                    ? 100
                    : CalculatePercentage(job.ImportedReleases, job.SkippedReleases, job.FailedReleases, job.EffectiveTotal),
                Completed = job.Status == DiscogsImportJobStatus.Succeeded || job.Status == DiscogsImportJobStatus.Failed,
                Success = job.Success,
                ErrorMessage = job.ErrorMessage,
                Errors = errors,
                Duration = job.Duration,
                CreatedAtUtc = job.CreatedAtUtc,
                LastUpdatedUtc = job.LastUpdatedUtc,
            };

            if (liveProgress != null && (job.Status == DiscogsImportJobStatus.Queued || job.Status == DiscogsImportJobStatus.Running))
            {
                dto.TotalReleases = liveProgress.TotalReleases > 0 ? liveProgress.TotalReleases : dto.TotalReleases;
                dto.EffectiveTotal = liveProgress.EffectiveTotal > 0 ? liveProgress.EffectiveTotal : dto.EffectiveTotal;
                dto.Imported = liveProgress.Imported;
                dto.Skipped = liveProgress.Skipped;
                dto.Failed = liveProgress.Failed;
                dto.Percentage = liveProgress.Percentage > 0
                    ? liveProgress.Percentage
                    : CalculatePercentage(dto.Imported, dto.Skipped, dto.Failed, dto.EffectiveTotal);
                dto.CooldownUntilUtc = liveProgress.CooldownUntilUtc;
            }

            return dto;
        }

        private static int CalculatePercentage(int imported, int skipped, int failed, int effectiveTotal)
        {
            var processed = imported + skipped + failed;
            if (effectiveTotal <= 0)
            {
                return processed > 0 ? 100 : 0;
            }

            return Math.Clamp((int)Math.Round((processed / (double)effectiveTotal) * 100), 0, 100);
        }

        private static List<string> DeserializeErrors(string? errorsJson)
        {
            if (string.IsNullOrWhiteSpace(errorsJson))
            {
                return new List<string>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<string>>(errorsJson) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }
    }
}