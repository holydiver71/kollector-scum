using System.Text.Json;
using KollectorScum.Api.Data;
using KollectorScum.Api.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// Migrates legacy flat-file cover art to the multi-tenant R2 storage structure.
    /// </summary>
    public class StorageMigrationService : IStorageMigrationService
    {
        private readonly KollectorScumDbContext _context;
        private readonly IStorageService _storageService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<StorageMigrationService> _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="StorageMigrationService"/>.
        /// </summary>
        public StorageMigrationService(
            KollectorScumDbContext context,
            IStorageService storageService,
            IConfiguration configuration,
            ILogger<StorageMigrationService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<StorageMigrationResult> MigrateLocalStorageAsync(int? releaseId = null)
        {
            var result = new StorageMigrationResult();

            List<Models.MusicRelease> releasesToMigrate;

            if (releaseId.HasValue)
            {
                var single = await _context.MusicReleases.FindAsync(releaseId.Value);
                if (single == null)
                {
                    result.Errors.Add($"Release {releaseId.Value} not found");
                    return result;
                }
                releasesToMigrate = new List<Models.MusicRelease> { single };
            }
            else
            {
                releasesToMigrate = await _context.MusicReleases
                    .Where(r => r.Images != null && r.Images.Contains("CoverFront"))
                    .ToListAsync();
            }

            result.TotalConsidered = releasesToMigrate.Count;

            if (releasesToMigrate.Count == 0)
            {
                _logger.LogInformation("No releases found with cover art to migrate");
                return result;
            }

            var imagesPath = _configuration["ImagesPath"] ?? "/home/andy/music-images";
            var oldCoverArtPath = Path.Combine(imagesPath, "covers");

            _logger.LogInformation("Migrating cover art from {OldPath} to R2 storage", oldCoverArtPath);

            foreach (var release in releasesToMigrate)
            {
                try
                {
                    if (release.UserId == Guid.Empty)
                    {
                        _logger.LogWarning("Skipping release {ReleaseId} - missing UserId", release.Id);
                        result.SkippedCount++;
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(release.Images)) continue;

                    var imagesObject = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(release.Images);
                    if (imagesObject == null || !imagesObject.TryGetValue("CoverFront", out var coverFrontElement)) continue;
                    if (coverFrontElement.ValueKind == JsonValueKind.Null || coverFrontElement.ValueKind == JsonValueKind.Undefined) continue;

                    var coverFrontValue = coverFrontElement.GetString();
                    if (string.IsNullOrWhiteSpace(coverFrontValue)) continue;

                    if (coverFrontValue.StartsWith("/cover-art/")) continue;

                    if (coverFrontValue.Contains('/') || coverFrontValue.StartsWith("http"))
                    {
                        _logger.LogWarning("Skipping release {ReleaseId} - unexpected URL format: {Url}", release.Id, coverFrontValue);
                        result.SkippedCount++;
                        continue;
                    }

                    var oldFilePath = Path.Combine(oldCoverArtPath, coverFrontValue);
                    if (!File.Exists(oldFilePath))
                    {
                        _logger.LogWarning("File not found for release {ReleaseId}: {FilePath}", release.Id, oldFilePath);
                        result.SkippedCount++;
                        continue;
                    }

                    using (var fileStream = File.OpenRead(oldFilePath))
                    {
                        var extension = Path.GetExtension(coverFrontValue).ToLowerInvariant();
                        var contentType = extension switch
                        {
                            ".jpg" or ".jpeg" => "image/jpeg",
                            ".png" => "image/png",
                            ".webp" => "image/webp",
                            ".gif" => "image/gif",
                            _ => "image/jpeg"
                        };

                        var newUrl = await _storageService.UploadFileAsync(
                            "cover-art", release.UserId.ToString(), coverFrontValue, fileStream, contentType);

                        imagesObject["CoverFront"] = JsonDocument.Parse($"\"{newUrl}\"").RootElement;
                        release.Images = JsonSerializer.Serialize(imagesObject);
                        _context.Entry(release).Property(r => r.Images).IsModified = true;

                        result.MigratedCount++;
                        _logger.LogInformation(
                            "Migrated cover art for release {ReleaseId}: {OldFilename} -> {NewUrl}",
                            release.Id, coverFrontValue, newUrl);
                    }
                }
                catch (Exception ex)
                {
                    var error = $"Failed to migrate release {release.Id} '{release.Title}': {ex.Message}";
                    _logger.LogError(ex, "Failed to migrate cover art for release {ReleaseId}", release.Id);
                    result.Errors.Add(error);
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Migration completed: {Migrated} migrated, {Skipped} skipped, {Errors} errors",
                result.MigratedCount, result.SkippedCount, result.Errors.Count);

            return result;
        }
    }
}
