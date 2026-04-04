using System.Linq.Expressions;
using KollectorScum.Api.Data;
using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using KollectorScum.Api.Repositories;
using KollectorScum.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace KollectorScum.Tests.Integration;

/// <summary>
/// Integration tests for the Discogs collection import service pipeline.
/// </summary>
public class DiscogsCollectionImportIntegrationTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public DiscogsCollectionImportIntegrationTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    [Fact]
    public async Task ImportCollectionAsync_MultiPageImport_UsesOneBatchInsertPerPage()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<KollectorScumDbContext>()
            .UseSqlite(_connection)
            .Options;

        await using var context = new KollectorScumDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var discogsService = new FakeDiscogsService(totalItems: 150, perPage: 100);
        var imageService = new FakeDiscogsImageService();
        var logger = Mock.Of<ILogger<DiscogsCollectionImportService>>();
        var env = new Mock<IHostEnvironment>();
        env.SetupGet(x => x.EnvironmentName).Returns("Production");

        using var unitOfWork = new CountingUnitOfWork(context);

        var service = new DiscogsCollectionImportService(
            discogsService,
            unitOfWork,
            logger,
            imageService,
            env.Object,
            cacheService: null);

        // Act
        var result = await service.ImportCollectionAsync("integration-user", Guid.NewGuid());

        // Assert
        Assert.True(result.Success);
        Assert.Equal(150, result.TotalReleases);
        Assert.Equal(150, result.ImportedReleases);

        Assert.Equal(2, unitOfWork.MusicReleasesAddRangeCallCount);

        var persisted = await context.MusicReleases.CountAsync();
        Assert.Equal(150, persisted);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    private sealed class CountingUnitOfWork : IUnitOfWork
    {
        private readonly KollectorScumDbContext _context;
        private readonly CountingMusicReleaseRepository _musicReleases;

        public CountingUnitOfWork(KollectorScumDbContext context)
        {
            _context = context;
            Countries = new Repository<Country>(_context);
            Stores = new Repository<Store>(_context);
            Formats = new Repository<Format>(_context);
            Genres = new Repository<Genre>(_context);
            Labels = new Repository<Label>(_context);
            Artists = new Repository<Artist>(_context);
            Packagings = new Repository<Packaging>(_context);
            _musicReleases = new CountingMusicReleaseRepository(_context);
        }

        public int MusicReleasesAddRangeCallCount => _musicReleases.AddRangeCallCount;

        public IRepository<Country> Countries { get; }
        public IRepository<Store> Stores { get; }
        public IRepository<Format> Formats { get; }
        public IRepository<Genre> Genres { get; }
        public IRepository<Label> Labels { get; }
        public IRepository<Artist> Artists { get; }
        public IRepository<Packaging> Packagings { get; }
        public IRepository<MusicRelease> MusicReleases => _musicReleases;

        public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => _context.SaveChangesAsync(cancellationToken);
        public void ClearChangeTracker() => _context.ChangeTracker.Clear();

        public Task BeginTransactionAsync() => Task.CompletedTask;
        public Task BeginTransactionAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task CommitTransactionAsync() => Task.CompletedTask;
        public Task CommitTransactionAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task RollbackTransactionAsync() => Task.CompletedTask;
        public Task RollbackTransactionAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public IRepository<T> GetRepository<T>() where T : class
        {
            if (typeof(T) == typeof(MusicRelease))
            {
                return (IRepository<T>)MusicReleases;
            }

            return new Repository<T>(_context);
        }

        public Task<bool> TableExistsAsync(string tableName, CancellationToken cancellationToken)
        {
            // Integration test uses an in-memory SQLite database and calls EnsureCreated,
            // so for the purposes of these tests assume the table exists.
            return Task.FromResult(true);
        }

        // This integration test uses releases with empty lookup payloads,
        // so these methods are defensive fallbacks and should not be called.
        public Task<int> UpsertFormatAsync(Guid userId, string name) => UpsertByNameAsync(_context, _context.Formats, userId, name);
        public Task<int> UpsertLabelAsync(Guid userId, string name) => UpsertByNameAsync(_context, _context.Labels, userId, name);
        public Task<int> UpsertCountryAsync(Guid userId, string name) => UpsertByNameAsync(_context, _context.Countries, userId, name);
        public Task<int> UpsertArtistAsync(Guid userId, string name) => UpsertByNameAsync(_context, _context.Artists, userId, name);
        public Task<int> UpsertGenreAsync(Guid userId, string name) => UpsertByNameAsync(_context, _context.Genres, userId, name);
        public Task<int> UpsertPackagingAsync(Guid userId, string name) => UpsertByNameAsync(_context, _context.Packagings, userId, name);
        public Task ResetSequencesAsync() => Task.CompletedTask;

        public void Dispose()
        {
            _context.Dispose();
        }

        private static async Task<int> UpsertByNameAsync<T>(KollectorScumDbContext context, DbSet<T> set, Guid userId, string name) where T : class
        {
            var normalized = name.Trim().ToUpperInvariant();
            var existing = await set.FirstOrDefaultAsync(e =>
                EF.Property<Guid>(e, "UserId") == userId && EF.Property<string>(e, "Name") == normalized);

            if (existing != null)
            {
                return EF.Property<int>(existing, "Id");
            }

            var entity = Activator.CreateInstance<T>();
            context.Entry(entity).Property("UserId").CurrentValue = userId;
            context.Entry(entity).Property("Name").CurrentValue = normalized;
            await set.AddAsync(entity);
            await context.SaveChangesAsync();
            return EF.Property<int>(entity, "Id");
        }
    }

    private sealed class CountingMusicReleaseRepository : Repository<MusicRelease>
    {
        public int AddRangeCallCount { get; private set; }

        public CountingMusicReleaseRepository(KollectorScumDbContext context) : base(context)
        {
        }

        public override async Task AddRangeAsync(IEnumerable<MusicRelease> entities)
        {
            AddRangeCallCount++;
            await base.AddRangeAsync(entities);
        }
    }

    private sealed class FakeDiscogsService : IDiscogsService
    {
        private readonly int _totalItems;
        private readonly int _perPage;

        public FakeDiscogsService(int totalItems, int perPage)
        {
            _totalItems = totalItems;
            _perPage = perPage;
        }

        public Task<List<DiscogsSearchResultDto>> SearchByCatalogNumberAsync(string catalogNumber, string? format = null, string? country = null, int? year = null)
            => Task.FromResult(new List<DiscogsSearchResultDto>());

        public Task<List<DiscogsSearchResultDto>> SearchGenericAsync(string? query = null, string? type = null, string? genre = null, string? style = null, string? country = null, int? year = null, string? format = null)
            => Task.FromResult(new List<DiscogsSearchResultDto>());

        public Task<DiscogsReleaseDto?> GetReleaseDetailsAsync(string releaseId)
            => Task.FromResult<DiscogsReleaseDto?>(null);

        public Task<DiscogsCollectionResponseDto?> GetUserCollectionAsync(string username, int page = 1, int perPage = 100)
        {
            var pages = (int)Math.Ceiling(_totalItems / (double)_perPage);
            var skip = (page - 1) * _perPage;
            var take = Math.Min(_perPage, Math.Max(0, _totalItems - skip));

            var releases = Enumerable.Range(skip + 1, take)
                .Select(i => new DiscogsCollectionReleaseDto
                {
                    InstanceId = Guid.NewGuid().ToString(),
                    BasicInformation = new DiscogsBasicInfoDto
                    {
                        Id = i,
                        Title = $"Release {i}",
                    }
                })
                .ToList();

            var response = new DiscogsCollectionResponseDto
            {
                Pagination = new DiscogsPaginationDto
                {
                    Page = page,
                    PerPage = _perPage,
                    Pages = pages,
                    Items = _totalItems,
                },
                Releases = releases,
            };

            return Task.FromResult<DiscogsCollectionResponseDto?>(response);
        }

        public void SetCooldownCallback(Action<DateTime?> callback) { /* no-op for tests */ }
    }

    private sealed class FakeDiscogsImageService : IDiscogsImageService
    {
        public Task<string?> DownloadAndStoreCoverArtAsync(string imageUrl, string artist, string title, string? year, Guid userId)
            => Task.FromResult<string?>(null);

        public string SanitizeFilename(string filename) => filename;
    }
}
