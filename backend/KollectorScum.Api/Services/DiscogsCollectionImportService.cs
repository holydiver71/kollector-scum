using System.Diagnostics;
using System.Text.Json;
using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using System.Collections.Concurrent;
using KollectorScum.Api.Models;
using Microsoft.Extensions.Hosting;
using System.Linq;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// Service for importing Discogs collections into the application
    /// </summary>
    public class DiscogsCollectionImportService : IDiscogsCollectionImportService
    {
        // In-memory progress store to allow clients to poll import progress
        private static readonly ConcurrentDictionary<Guid, DiscogsImportProgress> _progressStore = new();
        private const int ImportPhaseMaxPercentage = 85;
        private const int TracklistPhaseSpanPercentage = 10;
        private const int CoverArtPhaseSpanPercentage = 4;

        private readonly IDiscogsService _discogsService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DiscogsCollectionImportService> _logger;
        private readonly IDiscogsImageService _imageService;
        private readonly IHostEnvironment _env;
        private readonly ICacheService? _cacheService;

        // Cache for lookups created during import to avoid duplicates
        private Dictionary<string, int> _artistCache = new();
        private Dictionary<string, int> _genreCache = new();
        private Dictionary<string, int> _formatCache = new();
        private Dictionary<string, int> _labelCache = new();
        private Dictionary<string, int> _countryCache = new();

        public DiscogsCollectionImportService(
            IDiscogsService discogsService,
            IUnitOfWork unitOfWork,
            ILogger<DiscogsCollectionImportService> logger,
            IDiscogsImageService imageService,
            IHostEnvironment env,
            ICacheService? cacheService = null)
        {
            _discogsService = discogsService ?? throw new ArgumentNullException(nameof(discogsService));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _imageService = imageService ?? throw new ArgumentNullException(nameof(imageService));
            _env = env ?? throw new ArgumentNullException(nameof(env));
            _cacheService = cacheService;
        }

        /// <summary>
        /// Import user's collection from Discogs
        /// </summary>
        public async Task<DiscogsImportResult> ImportCollectionAsync(string username, Guid userId, CancellationToken cancellationToken = default)
        {
            // Clear caches at the start of each import
            _artistCache.Clear();
            _genreCache.Clear();
            _formatCache.Clear();
            _labelCache.Clear();
            _countryCache.Clear();

            // Register a cooldown callback so the progress store reflects API throttle pauses.
            _discogsService.SetCooldownCallback(cooldownUntil =>
            {
                if (_progressStore.TryGetValue(userId, out var snap))
                {
                    snap.CooldownUntilUtc = cooldownUntil;
                    _progressStore[userId] = snap;
                }
            });

            var stopwatch = Stopwatch.StartNew();
            var result = new DiscogsImportResult();

            try
            {
                _logger.LogInformation("Starting Discogs import for user {Username}", username);

                // Fetch first page to get total count
                var firstPage = await _discogsService.GetUserCollectionAsync(username, 1, 100);
                if (firstPage?.Pagination == null)
                {
                    result.Success = false;
                    result.Errors.Add("Failed to fetch collection from Discogs");
                    return result;
                }

                result.TotalReleases = firstPage.Pagination.Items;
                _logger.LogInformation("Found {TotalReleases} releases in collection for {Username}", 
                    result.TotalReleases, username);

                const int NEW_USER_IMPORT_LIMIT = 100;

                // Determine if this is a new user (no existing releases)
                var existingCount = await _unitOfWork.MusicReleases.CountAsync(mr => mr.UserId == userId);
                var isNewUser = existingCount == 0;

                // Apply strict limit for new user imports in Development (local) and Staging environments
                var applyLimit = isNewUser && (_env.IsDevelopment() || _env.IsStaging());
                if (applyLimit)
                {
                    _logger.LogInformation("Applying new-user import limit of {Limit} releases for environment {Env}", NEW_USER_IMPORT_LIMIT, _env.EnvironmentName);
                }

                // Remaining to process (null/unbounded when not limiting)
                int? remainingToProcess = applyLimit ? NEW_USER_IMPORT_LIMIT : null;

                // Effective total for progress reporting (100 for new users in dev/staging)
                var effectiveTotal = applyLimit ? NEW_USER_IMPORT_LIMIT : result.TotalReleases;
                var importedDiscogsIds = new HashSet<int>();

                // Initialize progress snapshot for polling clients
                _progressStore[userId] = new DiscogsImportProgress
                {
                    TotalReleases = result.TotalReleases,
                    EffectiveTotal = effectiveTotal,
                    Imported = 0,
                    Skipped = 0,
                    Failed = 0,
                    Percentage = 0,
                    Completed = false,
                    LastUpdatedUtc = DateTime.UtcNow
                };

                // Process first page (respect remainingToProcess)
                await ProcessReleasesAsync(firstPage.Releases, userId, result, importedDiscogsIds, remainingToProcess, cancellationToken);

                // Process remaining pages
                var totalPages = firstPage.Pagination.Pages;
                for (int page = 2; page <= totalPages; page++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // If we have a remaining limit, break when reached
                    if (remainingToProcess.HasValue)
                    {
                        var processedSoFar = result.ImportedReleases + result.SkippedReleases + result.FailedReleases;
                        var remaining = NEW_USER_IMPORT_LIMIT - processedSoFar;
                        if (remaining <= 0)
                        {
                            _logger.LogInformation("Reached new-user import limit of {Limit}; stopping further page processing", NEW_USER_IMPORT_LIMIT);
                            break;
                        }
                        remainingToProcess = remaining;
                    }

                    _logger.LogInformation("Processing page {Page} of {TotalPages}", page, totalPages);
                    
                    var pageData = await _discogsService.GetUserCollectionAsync(username, page, 100);
                    if (pageData?.Releases != null)
                    {
                        await ProcessReleasesAsync(pageData.Releases, userId, result, importedDiscogsIds, remainingToProcess, cancellationToken);
                    }
                }

                await EnrichTracklistsAsync(userId, importedDiscogsIds, cancellationToken);
                await EnrichCoverArtAsync(userId, importedDiscogsIds, cancellationToken);

                // Import is only successful if at least one release was imported
                result.Success = result.ImportedReleases > 0;
                
                if (!result.Success && result.TotalReleases > 0)
                {
                    result.Errors.Add("No releases could be imported. All releases failed to import.");
                }
                
                _logger.LogInformation("Discogs import completed for {Username}: {Imported} imported, {Skipped} skipped, {Failed} failed",
                    username, result.ImportedReleases, result.SkippedReleases, result.FailedReleases);

                // Mark progress completed
                if (_progressStore.ContainsKey(userId))
                {
                    _progressStore[userId] = new DiscogsImportProgress
                    {
                        TotalReleases = result.TotalReleases,
                        EffectiveTotal = (_env.IsDevelopment() || _env.IsStaging()) && (await _unitOfWork.MusicReleases.CountAsync(mr => mr.UserId == userId) == 0) ? NEW_USER_IMPORT_LIMIT : result.TotalReleases,
                        Imported = result.ImportedReleases,
                        Skipped = result.SkippedReleases,
                        Failed = result.FailedReleases,
                        Percentage = 100,
                        Completed = true,
                        LastUpdatedUtc = DateTime.UtcNow
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Discogs import for user {Username}", username);
                result.Success = false;
                result.Errors.Add($"Import failed: {ex.Message}");
            }
            finally
            {
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
            }

            // Invalidate cached lookup lists for this user so the UI picks up newly created
            // artists/genres/labels without requiring a manual refresh.
            try
            {
                if (_cacheService != null)
                {
                    var userKey = userId;
                    _cacheService.InvalidateGroup($"Artist:all:{userKey}");
                    _cacheService.InvalidateGroup($"Genre:all:{userKey}");
                    _cacheService.InvalidateGroup($"Label:all:{userKey}");
                    _cacheService.InvalidateGroup($"Format:all:{userKey}");
                    _cacheService.InvalidateGroup($"Country:all:{userKey}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to invalidate cache groups after import for user {Username}", username);
            }

            return result;
        }

        private async Task ProcessReleasesAsync(
            List<DiscogsCollectionReleaseDto> releases,
            Guid userId,
            DiscogsImportResult result,
            ISet<int> importedDiscogsIds,
            int? maxToProcess = null,
            CancellationToken cancellationToken = default)
        {
            if (releases == null || releases.Count == 0)
            {
                _logger.LogWarning("No releases to process");
                return;
            }
            
            // Track every Discogs ID seen in this batch (not only new inserts)
            // so deferred enrichment can backfill releases that already existed.
            foreach (var release in releases)
            {
                var discogsId = release.BasicInformation?.Id;
                if (discogsId.HasValue && discogsId.Value > 0)
                {
                    importedDiscogsIds.Add(discogsId.Value);
                }
            }

            // Trim to the max if a limit applies.
            if (maxToProcess.HasValue && maxToProcess.Value > 0 && releases.Count > maxToProcess.Value)
                releases = releases.Take(maxToProcess.Value).ToList();

            _logger.LogDebug("Processing {Count} releases", releases.Count);

            // Filter out structurally invalid items upfront.
            var validReleases = new List<DiscogsCollectionReleaseDto>();
            foreach (var r in releases)
            {
                if (r == null)
                {
                    result.FailedReleases++;
                    result.Errors.Add("Release object is null");
                    _logger.LogWarning("Encountered null release object");
                    continue;
                }
                if (r.BasicInformation == null)
                {
                    result.FailedReleases++;
                    result.Errors.Add($"Release missing basic information (InstanceId: {r.InstanceId})");
                    _logger.LogWarning("Release {InstanceId} missing BasicInformation", r.InstanceId ?? "unknown");
                    continue;
                }
                validReleases.Add(r);
            }

            if (validReleases.Count == 0) return;

            // --- Bulk duplicate check ---
            // Fetch the DiscogsIds that already exist for this user in a single query,
            // then skip those releases without issuing one SELECT per release.
            var batchDiscogsIds = validReleases
                .Select(r => r.BasicInformation!.Id)
                .Distinct()
                .ToHashSet();

            var existingIds = (await _unitOfWork.MusicReleases
                .GetAsync(mr => mr.UserId == userId && mr.DiscogsId != null && batchDiscogsIds.Contains(mr.DiscogsId.Value),
                          null, "", cancellationToken))
                .Where(mr => mr.DiscogsId.HasValue)
                .Select(mr => mr.DiscogsId!.Value)
                .ToHashSet();

            // Count skipped and update progress snapshot once.
            var skippedInBatch = validReleases.Count(r => existingIds.Contains(r.BasicInformation!.Id));
            if (skippedInBatch > 0)
            {
                result.SkippedReleases += skippedInBatch;
                UpdateProgressSnapshot(userId, result);
            }

            var toImport = validReleases
                .Where(r => !existingIds.Contains(r.BasicInformation!.Id))
                .ToList();

            // Discogs collections can contain multiple copies of the same release.
            // Keep only the first occurrence per Discogs ID in this batch so we don't
            // hit the unique index on (UserId, DiscogsId) during bulk insert.
            var dedupedToImport = new List<DiscogsCollectionReleaseDto>(toImport.Count);
            var seenDiscogsIds = new HashSet<int>();
            var duplicateInBatchCount = 0;
            foreach (var release in toImport)
            {
                var discogsId = release.BasicInformation!.Id;
                if (seenDiscogsIds.Add(discogsId))
                {
                    dedupedToImport.Add(release);
                }
                else
                {
                    duplicateInBatchCount++;
                }
            }

            if (duplicateInBatchCount > 0)
            {
                result.SkippedReleases += duplicateInBatchCount;
                UpdateProgressSnapshot(userId, result);
            }

            toImport = dedupedToImport;

            if (toImport.Count == 0) return;

            // --- Map and collect ---
            var newReleases = new List<MusicRelease>();
            var pendingImportedInBatch = 0;
            foreach (var release in toImport)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var musicRelease = await MapToMusicReleaseAsync(release, userId, cancellationToken);
                    if (musicRelease != null)
                    {
                        newReleases.Add(musicRelease);
                        pendingImportedInBatch++;
                        UpdateProgressSnapshot(userId, result, pendingImportedInBatch);
                        _logger.LogDebug("Mapped release: {Title} (Discogs ID: {DiscogsId})",
                            musicRelease.Title, musicRelease.DiscogsId);
                    }
                    else
                    {
                        result.FailedReleases++;
                        UpdateProgressSnapshot(userId, result, pendingImportedInBatch);
                        var errorMsg = $"Failed to map release: {release.BasicInformation!.Title}";
                        result.Errors.Add(errorMsg);
                        _logger.LogWarning("{ErrorMsg} (Discogs ID: {DiscogsId})", errorMsg, release.BasicInformation.Id);
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    result.FailedReleases++;
                    UpdateProgressSnapshot(userId, result, pendingImportedInBatch);
                    var title = release.BasicInformation?.Title ?? "Unknown";
                    result.Errors.Add($"Error importing '{title}': {ex.Message}");
                    _logger.LogError(ex, "Error importing release: {Title}", title);
                }
            }

            if (newReleases.Count == 0) return;

            // --- Batch insert ---
            try
            {
                await _unitOfWork.MusicReleases.AddRangeAsync(newReleases);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                UpdateProgressSnapshot(userId, result);
                throw;
            }

            result.ImportedReleases += newReleases.Count;
            foreach (var newRelease in newReleases)
            {
                if (newRelease.DiscogsId.HasValue && newRelease.DiscogsId.Value > 0)
                {
                    importedDiscogsIds.Add(newRelease.DiscogsId.Value);
                }
            }

            // Detach all tracked entities so the enrichment phase can freely call Update()
            // on AsNoTracking copies without causing EF tracking conflicts.
            _unitOfWork.ClearChangeTracker();

            UpdateProgressSnapshot(userId, result);
            _logger.LogInformation("Batch-inserted {Count} releases into the database.", newReleases.Count);
        }

        private async Task EnrichTracklistsAsync(Guid userId, ISet<int> importedDiscogsIds, CancellationToken cancellationToken)
        {
            if (importedDiscogsIds.Count == 0)
            {
                return;
            }

            var importedReleases = (await _unitOfWork.MusicReleases.GetAsync(
                    mr => mr.UserId == userId
                        && mr.DiscogsId.HasValue
                        && mr.DiscogsId.Value > 0
                        && importedDiscogsIds.Contains(mr.DiscogsId.Value),
                    null,
                    "",
                    cancellationToken))
                .ToList();

            var releasesToEnrich = importedReleases
                .Where(r => NeedsTracklistEnrichment(r) || !r.CountryId.HasValue || string.IsNullOrEmpty(r.Upc))
                .ToList();

            if (releasesToEnrich.Count == 0)
            {
                return;
            }

            _logger.LogInformation("Starting deferred tracklist enrichment for {Count} releases", releasesToEnrich.Count);

            var enrichedCount = 0;
            var modifiedCount = 0;
            var processedCount = 0;
            foreach (var release in releasesToEnrich)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!release.DiscogsId.HasValue)
                {
                    processedCount++;
                    UpdatePhaseProgress(userId, ImportPhaseMaxPercentage, TracklistPhaseSpanPercentage, processedCount, releasesToEnrich.Count);
                    continue;
                }

                try
                {
                    var fullRelease = await _discogsService.GetReleaseDetailsAsync(release.DiscogsId.Value.ToString());
                    if (fullRelease == null)
                    {
                        continue;
                    }

                    var releaseModified = false;

                    // The Discogs collection API does not include country in basic_information,
                    // so we populate it here from the full release details.
                    if (!release.CountryId.HasValue && !string.IsNullOrEmpty(fullRelease.Country))
                    {
                        var countryId = await GetOrCreateCountryAsync(fullRelease.Country, userId);
                        if (countryId.HasValue)
                        {
                            release.CountryId = countryId;
                            releaseModified = true;
                        }
                    }

                    // Barcode / UPC is only available from the full release details (identifiers list).
                    if (string.IsNullOrEmpty(release.Upc))
                    {
                        var barcode = fullRelease.Identifiers
                            ?.FirstOrDefault(id => id.Type.Equals("Barcode", StringComparison.OrdinalIgnoreCase))
                            ?.Value;
                        if (!string.IsNullOrWhiteSpace(barcode))
                        {
                            release.Upc = barcode.Trim();
                            releaseModified = true;
                        }
                    }

                    if (fullRelease.Tracklist?.Count > 0)
                    {
                        var artistIds = DeserializeIds(release.Artists);
                        var genreIds = DeserializeIds(release.Genres);

                        var media = BuildMediaFromTracklist(
                            fullRelease.Tracklist,
                            release.Title,
                            release.FormatId,
                            artistIds,
                            genreIds,
                            release.ReleaseYear);

                        if (media != null)
                        {
                            release.Media = JsonSerializer.Serialize(media);
                            releaseModified = true;
                            enrichedCount++;
                        }
                    }

                    if (releaseModified)
                    {
                        release.LastModified = DateTime.UtcNow;
                        _unitOfWork.MusicReleases.Update(release);
                        modifiedCount++;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Deferred tracklist enrichment failed for Discogs ID {DiscogsId}", release.DiscogsId);
                }

                processedCount++;
                UpdatePhaseProgress(userId, ImportPhaseMaxPercentage, TracklistPhaseSpanPercentage, processedCount, releasesToEnrich.Count);
            }

            if (modifiedCount > 0)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Completed deferred enrichment: {EnrichedCount} tracklists added, {ModifiedCount} releases updated", enrichedCount, modifiedCount);
            }

            UpdatePhaseProgress(userId, ImportPhaseMaxPercentage + TracklistPhaseSpanPercentage, 0, 1, 1);
        }

        private async Task EnrichCoverArtAsync(Guid userId, ISet<int> importedDiscogsIds, CancellationToken cancellationToken)
        {
            if (importedDiscogsIds.Count == 0)
            {
                return;
            }

            var releasesToMirror = (await _unitOfWork.MusicReleases.GetAsync(
                    mr => mr.UserId == userId
                        && mr.DiscogsId.HasValue
                        && mr.DiscogsId.Value > 0
                        && importedDiscogsIds.Contains(mr.DiscogsId.Value)
                        && !string.IsNullOrWhiteSpace(mr.Images),
                    null,
                    "",
                    cancellationToken))
                .ToList();

            if (releasesToMirror.Count == 0)
            {
                return;
            }

            _logger.LogInformation("Starting deferred cover-art mirroring for {Count} releases", releasesToMirror.Count);

            var mirroredCount = 0;
            var processedCount = 0;
            foreach (var release in releasesToMirror)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var coverUrl = ExtractCoverFrontFromImages(release.Images);
                if (string.IsNullOrWhiteSpace(coverUrl) || !coverUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    processedCount++;
                    UpdatePhaseProgress(userId, ImportPhaseMaxPercentage + TracklistPhaseSpanPercentage, CoverArtPhaseSpanPercentage, processedCount, releasesToMirror.Count);
                    continue;
                }

                try
                {
                    var year = release.ReleaseYear?.Year > 0 ? release.ReleaseYear.Value.Year.ToString() : null;

                    var artistName = "Unknown";
                    var artistIds = DeserializeIds(release.Artists);
                    if (artistIds.Count > 0)
                    {
                        var firstId = artistIds[0];
                        var matchedArtists = await _unitOfWork.Artists.GetAsync(a => a.Id == firstId && a.UserId == userId);
                        artistName = matchedArtists.FirstOrDefault()?.Name ?? "Unknown";
                    }

                    var mirrored = await _imageService.DownloadAndStoreCoverArtAsync(
                        coverUrl,
                        artistName,
                        release.Title,
                        year,
                        userId);

                    var normalizedPath = NormalizeMirroredImagePath(mirrored, userId);
                    if (string.IsNullOrWhiteSpace(normalizedPath))
                    {
                        continue;
                    }

                    release.Images = JsonSerializer.Serialize(new { CoverFront = normalizedPath });
                    release.LastModified = DateTime.UtcNow;
                    _unitOfWork.MusicReleases.Update(release);
                    mirroredCount++;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Deferred cover-art mirroring failed for Discogs ID {DiscogsId}", release.DiscogsId);
                }

                processedCount++;
                UpdatePhaseProgress(userId, ImportPhaseMaxPercentage + TracklistPhaseSpanPercentage, CoverArtPhaseSpanPercentage, processedCount, releasesToMirror.Count);
            }

            if (mirroredCount > 0)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Completed deferred cover-art mirroring for {Count} releases", mirroredCount);
            }

            UpdatePhaseProgress(userId, 99, 0, 1, 1);
        }

        private static List<int> DeserializeIds(string? serializedIds)
        {
            if (string.IsNullOrWhiteSpace(serializedIds))
            {
                return new List<int>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<int>>(serializedIds) ?? new List<int>();
            }
            catch
            {
                return new List<int>();
            }
        }

        private static string? ExtractCoverFrontFromImages(string? serializedImages)
        {
            if (string.IsNullOrWhiteSpace(serializedImages))
            {
                return null;
            }

            try
            {
                using var doc = JsonDocument.Parse(serializedImages);
                if (doc.RootElement.TryGetProperty("CoverFront", out var coverFront)
                    && coverFront.ValueKind == JsonValueKind.String)
                {
                    return coverFront.GetString();
                }
            }
            catch
            {
                return null;
            }

            return null;
        }

        private static string? NormalizeMirroredImagePath(string? mirroredResult, Guid userId)
        {
            if (string.IsNullOrWhiteSpace(mirroredResult))
            {
                return null;
            }

            var trimmed = mirroredResult.Trim();
            if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("/cover-art/", StringComparison.OrdinalIgnoreCase))
            {
                return trimmed;
            }

            var fileName = trimmed.Contains('/') ? trimmed.Split('/').Last() : trimmed;
            return $"/cover-art/{userId}/{fileName}";
        }

        /// <summary>
        /// Updates the in-memory progress snapshot used by polling clients.
        /// </summary>
        private void UpdateProgressSnapshot(Guid userId, DiscogsImportResult result, int pendingImportedInBatch = 0)
        {
            if (_progressStore.TryGetValue(userId, out var snap))
            {
                snap.Imported = result.ImportedReleases + pendingImportedInBatch;
                snap.Skipped = result.SkippedReleases;
                snap.Failed = result.FailedReleases;
                var processed = snap.Imported + snap.Skipped + snap.Failed;
                var importPhasePercentage = snap.EffectiveTotal > 0
                    ? (int)Math.Round((processed / (double)snap.EffectiveTotal) * ImportPhaseMaxPercentage)
                    : 0;
                snap.Percentage = Math.Max(snap.Percentage, Math.Clamp(importPhasePercentage, 0, ImportPhaseMaxPercentage));
                snap.LastUpdatedUtc = DateTime.UtcNow;
                _progressStore[userId] = snap;
            }
        }

        private void UpdatePhaseProgress(Guid userId, int basePercentage, int phaseSpan, int processedCount, int totalCount)
        {
            if (!_progressStore.TryGetValue(userId, out var snap))
            {
                return;
            }

            var phaseProgress = phaseSpan <= 0 || totalCount <= 0
                ? 0
                : (int)Math.Round((processedCount / (double)totalCount) * phaseSpan);

            var target = Math.Clamp(basePercentage + phaseProgress, 0, 99);
            if (target > snap.Percentage)
            {
                snap.Percentage = target;
                snap.LastUpdatedUtc = DateTime.UtcNow;
                _progressStore[userId] = snap;
            }
        }

        private async Task<MusicRelease?> MapToMusicReleaseAsync(DiscogsCollectionReleaseDto release, Guid userId, CancellationToken cancellationToken = default)
        {
            if (release.BasicInformation == null) return null;

            var basicInfo = release.BasicInformation;

            try
            {
                // Resolve or create lookups (these methods now save immediately if creating new entities)
                var formatId = await GetOrCreateFormatAsync(basicInfo.Formats, userId);
                var labelId = await GetOrCreateLabelAsync(basicInfo.Labels, userId);
                var countryId = await GetOrCreateCountryAsync(basicInfo.Country, userId);
                var artistIds = await GetOrCreateArtistsAsync(basicInfo.Artists, userId);
                var genreIds = await GetOrCreateGenresAsync(basicInfo.Genres, basicInfo.Styles, userId);

                // Extract notes
                var notes = release.Notes?.FirstOrDefault()?.Value;

                // Validate and parse year
                DateTime? releaseYear = null;
                if (basicInfo.Year.HasValue && basicInfo.Year.Value >= 1 && basicInfo.Year.Value <= 9999)
                {
                    try
                    {
                        releaseYear = DateTime.SpecifyKind(new DateTime(basicInfo.Year.Value, 1, 1), DateTimeKind.Utc);
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        _logger.LogWarning(ex, "Invalid year value {Year} for release {Title}", basicInfo.Year.Value, basicInfo.Title);
                    }
                }
                else if (basicInfo.Year.HasValue)
                {
                    _logger.LogWarning("Year value {Year} out of valid range for release {Title}", basicInfo.Year.Value, basicInfo.Title);
                }

                // Catalog number from the first label (available in collection response)
                var labelNumber = basicInfo.Labels?.FirstOrDefault()?.CatalogNumber;

                // Create MusicRelease entity
                var musicRelease = new MusicRelease
                {
                    UserId = userId,
                    DiscogsId = basicInfo.Id,
                    Title = basicInfo.Title ?? string.Empty,
                    ReleaseYear = releaseYear,
                    FormatId = formatId,
                    LabelId = labelId,
                    LabelNumber = string.IsNullOrWhiteSpace(labelNumber) ? null : labelNumber.Trim(),
                    CountryId = countryId,
                    Artists = artistIds.Count > 0 ? JsonSerializer.Serialize(artistIds) : null,
                    Genres = genreIds.Count > 0 ? JsonSerializer.Serialize(genreIds) : null,
                    Notes = notes,
                    DateAdded = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };

                // Set the Discogs web URL in the links list so it is surfaced in the release detail view.
                // The web URL can be constructed directly from the Discogs release ID – no extra API call needed.
                var discogsUrl = $"https://www.discogs.com/release/{basicInfo.Id}";
                musicRelease.Links = JsonSerializer.Serialize(new[]
                {
                    new MusicReleaseLinkDto { Url = discogsUrl, Type = "Discogs", Description = "" }
                });

                // Keep the initial import fast by storing the Discogs-hosted image URLs.
                // Mirroring to R2 is handled as deferred enrichment work.
                var coverUrl = basicInfo.CoverImage;
                var thumbUrl = basicInfo.Thumb;
                if (!string.IsNullOrEmpty(coverUrl) || !string.IsNullOrEmpty(thumbUrl))
                {
                    var images = new
                    {
                        CoverFront = coverUrl ?? thumbUrl,
                        Thumbnail = thumbUrl ?? coverUrl
                    };
                    musicRelease.Images = JsonSerializer.Serialize(images);
                }

                return musicRelease;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error mapping release to MusicRelease: {Title}", basicInfo.Title);
                return null;
            }
        }

        private List<object>? BuildMediaFromTracklist(List<DiscogsTrackDto> tracklist, string releaseTitle, int? formatId, List<int> artistIds, List<int> genreIds, DateTime? releaseYear)
        {
            if (tracklist == null || tracklist.Count == 0) return null;

            var tracks = new List<object>();
            int trackIndex = 1;

            // Convert IDs to strings to match existing data format
            var artistIdsAsStrings = artistIds.Select(id => id.ToString()).ToList();
            var genreIdsAsStrings = genreIds.Select(id => id.ToString()).ToList();

            foreach (var track in tracklist)
            {
                // Skip non-track items (like headings)
                if (string.IsNullOrEmpty(track.Title)) continue;

                var trackObj = new
                {
                    Title = track.Title,
                    ReleaseYear = releaseYear?.ToString("yyyy-MM-dd") ?? "",
                    Artists = artistIdsAsStrings,
                    Genres = genreIdsAsStrings,
                    Live = false,
                    LengthSecs = ParseDuration(track.Duration),
                    Index = trackIndex++
                };

                tracks.Add(trackObj);
            }

            if (tracks.Count == 0) return null;

            var mediaList = new List<object>
            {
                new
                {
                    Title = releaseTitle,
                    FormatId = formatId ?? 0,
                    Index = 1,
                    Tracks = tracks
                }
            };

            return mediaList;
        }

        private static bool NeedsTracklistEnrichment(MusicRelease release)
        {
            if (string.IsNullOrWhiteSpace(release.Media))
            {
                return true;
            }

            try
            {
                var media = JsonSerializer.Deserialize<List<MusicReleaseMediaDto>>(release.Media);
                if (media == null || media.Count == 0)
                {
                    return true;
                }

                return media.All(m => m.Tracks == null || m.Tracks.Count == 0);
            }
            catch (JsonException)
            {
                // Invalid/legacy media JSON should be eligible for refresh.
                return true;
            }
        }

        private int ParseDuration(string? duration)
        {
            if (string.IsNullOrWhiteSpace(duration)) return 0;

            try
            {
                // Duration format can be: "3:45", "1:23:45", etc.
                var parts = duration.Split(':');
                
                if (parts.Length == 2)
                {
                    // MM:SS format
                    if (int.TryParse(parts[0], out var minutes) && int.TryParse(parts[1], out var seconds))
                    {
                        return (minutes * 60) + seconds;
                    }
                }
                else if (parts.Length == 3)
                {
                    // HH:MM:SS format
                    if (int.TryParse(parts[0], out var hours) && int.TryParse(parts[1], out var minutes) && int.TryParse(parts[2], out var seconds))
                    {
                        return (hours * 3600) + (minutes * 60) + seconds;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse duration: {Duration}", duration);
            }

            return 0;
        }

        private async Task<int?> GetOrCreateFormatAsync(List<DiscogsFormatDto> formats, Guid userId)
        {
            if (formats == null || formats.Count == 0) return null;

            var formatName = formats.First().Name;
            if (string.IsNullOrEmpty(formatName)) return null;
            // Normalize (trim + case) before using as a key or storing
            var normalizedFormatName = formatName.Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(normalizedFormatName)) return null;

            var cacheKey = $"{userId}:{normalizedFormatName}";

            // Check cache first
            if (_formatCache.TryGetValue(cacheKey, out var cachedId))
            {
                return cachedId;
            }

            // Try to find existing format using normalized name
            var existing = await _unitOfWork.Formats
                .GetAsync(f => f.UserId == userId && f.Name == normalizedFormatName);
            
            if (existing.Any())
            {
                var id = existing.First().Id;
                _formatCache[cacheKey] = id;
                return id;
            }

            // Atomically insert or return existing id using the database upsert helper
            var newId = await _unitOfWork.UpsertFormatAsync(userId, normalizedFormatName);
            _formatCache[cacheKey] = newId;
            return newId;
        }

        private async Task<int?> GetOrCreateLabelAsync(List<DiscogsLabelDto> labels, Guid userId)
        {
            if (labels == null || labels.Count == 0) return null;

            var labelName = labels.First().Name;
            if (string.IsNullOrEmpty(labelName)) return null;

            // Normalize (trim + case)
            var normalizedLabelName = labelName.Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(normalizedLabelName)) return null;

            var cacheKey = $"{userId}:{normalizedLabelName}";

            // Check cache first
            if (_labelCache.TryGetValue(cacheKey, out var cachedId))
            {
                return cachedId;
            }

            // Try to find existing label
            var existing = await _unitOfWork.Labels
                .GetAsync(l => l.UserId == userId && l.Name == normalizedLabelName);
            
            if (existing.Any())
            {
                var id = existing.First().Id;
                _labelCache[cacheKey] = id;
                return id;
            }

            // Atomically insert or return existing id using the database upsert helper
            var newId = await _unitOfWork.UpsertLabelAsync(userId, normalizedLabelName);
            _labelCache[cacheKey] = newId;
            return newId;
        }

        private async Task<int?> GetOrCreateCountryAsync(string? countryName, Guid userId)
        {
            if (string.IsNullOrEmpty(countryName)) return null;

            var normalizedCountryName = countryName.Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(normalizedCountryName)) return null;

            var cacheKey = $"{userId}:{normalizedCountryName}";

            // Check cache first
            if (_countryCache.TryGetValue(cacheKey, out var cachedId))
            {
                return cachedId;
            }

            // Try to find existing country
            var existing = await _unitOfWork.Countries
                .GetAsync(c => c.UserId == userId && c.Name == normalizedCountryName);
            
            if (existing.Any())
            {
                var id = existing.First().Id;
                _countryCache[cacheKey] = id;
                return id;
            }

            // Atomically insert or return existing id using the database upsert helper
            var newCountryId = await _unitOfWork.UpsertCountryAsync(userId, normalizedCountryName);
            _countryCache[cacheKey] = newCountryId;
            return newCountryId;
        }

        private async Task<List<int>> GetOrCreateArtistsAsync(List<DiscogsArtistDto> artists, Guid userId)
        {
            var artistIds = new List<int>();
            
            if (artists == null || artists.Count == 0) return artistIds;

            foreach (var artist in artists)
            {
                if (string.IsNullOrEmpty(artist.Name)) continue;

                // Normalize artist name (trim + case)
                var normalizedArtistName = artist.Name.Trim().ToUpperInvariant();
                if (string.IsNullOrEmpty(normalizedArtistName)) continue;

                var cacheKey = $"{userId}:{normalizedArtistName}";

                // Check cache first
                if (_artistCache.TryGetValue(cacheKey, out var cachedId))
                {
                    artistIds.Add(cachedId);
                    continue;
                }

                // Try to find existing artist in database
                var existing = await _unitOfWork.Artists
                    .GetAsync(a => a.UserId == userId && a.Name == normalizedArtistName);
                
                if (existing.Any())
                {
                    var id = existing.First().Id;
                    _artistCache[cacheKey] = id;
                    artistIds.Add(id);
                }
                else
                {
                    // Atomically insert or return existing id using the upsert helper
                    var newArtistId = await _unitOfWork.UpsertArtistAsync(userId, normalizedArtistName);
                    _artistCache[cacheKey] = newArtistId;
                    artistIds.Add(newArtistId);
                }
            }

            return artistIds;
        }

        private async Task<List<int>> GetOrCreateGenresAsync(List<string> genres, List<string> styles, Guid userId)
        {
            var genreIds = new List<int>();
            var allGenres = new List<string>();
            
            if (genres != null) allGenres.AddRange(genres);
            if (styles != null) allGenres.AddRange(styles);

            foreach (var genreName in allGenres.Distinct())
            {
                if (string.IsNullOrEmpty(genreName)) continue;

                var normalizedGenreName = genreName.Trim().ToUpperInvariant();
                if (string.IsNullOrEmpty(normalizedGenreName)) continue;

                var cacheKey = $"{userId}:{normalizedGenreName}";

                // Check cache first
                if (_genreCache.TryGetValue(cacheKey, out var cachedId))
                {
                    genreIds.Add(cachedId);
                    continue;
                }

                // Try to find existing genre
                var existing = await _unitOfWork.Genres
                    .GetAsync(g => g.UserId == userId && g.Name == normalizedGenreName);
                
                if (existing.Any())
                {
                    var id = existing.First().Id;
                    _genreCache[cacheKey] = id;
                    genreIds.Add(id);
                }
                else
                {
                    // Atomically insert or return existing id using the upsert helper
                    var newGenreId = await _unitOfWork.UpsertGenreAsync(userId, normalizedGenreName);
                    _genreCache[cacheKey] = newGenreId;
                    genreIds.Add(newGenreId);
                }
            }

            return genreIds;
        }

        /// <summary>
        /// Return progress snapshot for a user's current import (if any)
        /// </summary>
        public DiscogsImportProgress? GetProgress(Guid userId)
        {
            if (_progressStore.TryGetValue(userId, out var snap)) return snap;
            return null;
        }
}
}
