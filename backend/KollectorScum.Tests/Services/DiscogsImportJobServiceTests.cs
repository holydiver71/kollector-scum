using KollectorScum.Api.Data;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using KollectorScum.Api.Repositories;
using KollectorScum.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace KollectorScum.Tests.Services
{
    public class DiscogsImportJobServiceTests
    {
        private static KollectorScumDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<KollectorScumDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new KollectorScumDbContext(options);
        }

        [Fact]
        public async Task EnqueueImportAsync_WhenNoActiveJob_CreatesAndQueuesJob()
        {
            using var context = CreateContext();
            var repository = new Repository<DiscogsImportJob>(context);
            using var unitOfWork = new UnitOfWork(context, new Mock<IUserContext>().Object);
            var queue = new Mock<IDiscogsImportJobQueue>();
            var importService = new Mock<IDiscogsCollectionImportService>();
            var logger = new Mock<ILogger<DiscogsImportJobService>>();
            var service = new DiscogsImportJobService(repository, unitOfWork, queue.Object, importService.Object, logger.Object);
            var userId = Guid.NewGuid();

            var result = await service.EnqueueImportAsync("discogs-user", userId);

            Assert.Equal("Queued", result.Status);
            Assert.Equal(result.JobId, (await context.DiscogsImportJobs.SingleAsync()).JobId);
            queue.Verify(item => item.QueueAsync(result.JobId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetJobStatusAsync_WhenRunning_UsesLiveProgressSnapshot()
        {
            using var context = CreateContext();
            var repository = new Repository<DiscogsImportJob>(context);
            using var unitOfWork = new UnitOfWork(context, new Mock<IUserContext>().Object);
            var queue = new Mock<IDiscogsImportJobQueue>();
            var importService = new Mock<IDiscogsCollectionImportService>();
            var logger = new Mock<ILogger<DiscogsImportJobService>>();
            var jobId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            context.DiscogsImportJobs.Add(new DiscogsImportJob
            {
                JobId = jobId,
                UserId = userId,
                Username = "discogs-user",
                Status = DiscogsImportJobStatus.Running,
                CreatedAtUtc = DateTime.UtcNow,
                LastUpdatedUtc = DateTime.UtcNow,
            });
            await context.SaveChangesAsync();

            importService.Setup(item => item.GetProgress(userId)).Returns(new DiscogsImportProgress
            {
                TotalReleases = 42,
                EffectiveTotal = 42,
                Imported = 12,
                Skipped = 4,
                Failed = 1,
                Completed = false,
                LastUpdatedUtc = DateTime.UtcNow,
            });

            var service = new DiscogsImportJobService(repository, unitOfWork, queue.Object, importService.Object, logger.Object);

            var result = await service.GetJobStatusAsync(jobId, userId);

            Assert.NotNull(result);
            Assert.Equal("Running", result!.Status);
            Assert.Equal(42, result.TotalReleases);
            Assert.Equal(12, result.Imported);
            Assert.Equal(4, result.Skipped);
            Assert.Equal(1, result.Failed);
            Assert.False(result.Completed);
        }

        [Fact]
        public async Task GetJobStatusAsync_WhenRunningAndLiveProgressCompleted_DoesNotCompleteUntilJobStatusIsTerminal()
        {
            using var context = CreateContext();
            var repository = new Repository<DiscogsImportJob>(context);
            using var unitOfWork = new UnitOfWork(context, new Mock<IUserContext>().Object);
            var queue = new Mock<IDiscogsImportJobQueue>();
            var importService = new Mock<IDiscogsCollectionImportService>();
            var logger = new Mock<ILogger<DiscogsImportJobService>>();
            var jobId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            context.DiscogsImportJobs.Add(new DiscogsImportJob
            {
                JobId = jobId,
                UserId = userId,
                Username = "discogs-user",
                Status = DiscogsImportJobStatus.Running,
                CreatedAtUtc = DateTime.UtcNow,
                LastUpdatedUtc = DateTime.UtcNow,
            });
            await context.SaveChangesAsync();

            importService.Setup(item => item.GetProgress(userId)).Returns(new DiscogsImportProgress
            {
                TotalReleases = 42,
                EffectiveTotal = 42,
                Imported = 42,
                Skipped = 0,
                Failed = 0,
                Percentage = 99,
                Completed = true,
                LastUpdatedUtc = DateTime.UtcNow,
            });

            var service = new DiscogsImportJobService(repository, unitOfWork, queue.Object, importService.Object, logger.Object);

            var result = await service.GetJobStatusAsync(jobId, userId);

            Assert.NotNull(result);
            Assert.Equal("Running", result!.Status);
            Assert.False(result.Completed);
            Assert.Equal(99, result.Percentage);
        }
    }
}