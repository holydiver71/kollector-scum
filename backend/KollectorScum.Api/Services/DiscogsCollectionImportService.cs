using System.Diagnostics;
using System.Text.Json;
using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using System.Collections.Concurrent;
using KollectorScum.Api.Models;
using Microsoft.EntityFrameworkCore;
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

        private readonly IDiscogsService _discogsService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DiscogsCollectionImportService> _logger;
        private readonly IDiscogsImageService _imageService;
        private readonly IHostEnvironment _env;

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
            IHostEnvironment env)
        {
            _discogsService = discogsService ?? throw new ArgumentNullException(nameof(discogsService));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _imageService = imageService ?? throw new ArgumentNullException(nameof(imageService));
            _env = env ?? throw new ArgumentNullException(nameof(env));
        }

        /// <summary>
        /// Import user's collection from Discogs
        /// </summary>
        public async Task<DiscogsImportResult> ImportCollectionAsync(string username, Guid userId)
        {
            // Clear caches at the start of each import
            _artistCache.Clear();
            _genreCache.Clear();
            _formatCache.Clear();
            _labelCache.Clear();
            _countryCache.Clear();

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

                // Initialize progress snapshot for polling clients
                _progressStore[userId] = new DiscogsImportProgress
                {
                    TotalReleases = result.TotalReleases,
                    EffectiveTotal = effectiveTotal,
                    Imported = 0,
                    Skipped = 0,
                    Failed = 0,
                    Completed = false,
                    LastUpdatedUtc = DateTime.UtcNow
                };

                // Process first page (respect remainingToProcess)
                await ProcessReleasesAsync(firstPage.Releases, userId, result, remainingToProcess);

                // Process remaining pages
                var totalPages = firstPage.Pagination.Pages;
                for (int page = 2; page <= totalPages; page++)
                {
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
                        await ProcessReleasesAsync(pageData.Releases, userId, result, remainingToProcess);
                    }

                    // Add small delay to respect rate limits
                    await Task.Delay(1000);
                }

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

            return result;
        }

        private async Task ProcessReleasesAsync(List<DiscogsCollectionReleaseDto> releases, Guid userId, DiscogsImportResult result, int? maxToProcess = null)
        {
            if (releases == null || releases.Count == 0)
            {
                _logger.LogWarning("No releases to process");
                return;
            }
            // If a maximum number of releases to process was provided, trim the list
            if (maxToProcess.HasValue && maxToProcess.Value > 0 && releases.Count > maxToProcess.Value)
            {
                releases = releases.Take(maxToProcess.Value).ToList();
            }

            _logger.LogDebug("Processing {Count} releases", releases.Count);
            
            foreach (var release in releases)
            {
                try
                {
                    if (release == null)
                    {
                        result.FailedReleases++;
                        result.Errors.Add("Release object is null");
                        _logger.LogWarning("Encountered null release object");
                        continue;
                    }
                    
                    if (release.BasicInformation == null)
                    {
                        result.FailedReleases++;
                        result.Errors.Add($"Release missing basic information (InstanceId: {release.InstanceId})");
                        _logger.LogWarning("Release {InstanceId} missing BasicInformation", release.InstanceId ?? "unknown");
                        continue;
                    }

                    // Check if already imported
                    var existing = await _unitOfWork.MusicReleases
                        .GetAsync(mr => mr.UserId == userId && mr.DiscogsId == release.BasicInformation.Id);
                    
                    if (existing.Any())
                    {
                        result.SkippedReleases++;
                        if (_progressStore.ContainsKey(userId))
                        {
                            var snap = _progressStore[userId];
                            snap.Skipped = result.SkippedReleases;
                            snap.LastUpdatedUtc = DateTime.UtcNow;
                            _progressStore[userId] = snap;
                        }
                        continue;
                    }

                    // Map and import the release
                    var musicRelease = await MapToMusicReleaseAsync(release, userId);
                    if (musicRelease != null)
                    {
                        await _unitOfWork.MusicReleases.AddAsync(musicRelease);
                        await _unitOfWork.SaveChangesAsync();
                        result.ImportedReleases++;
                        if (_progressStore.ContainsKey(userId))
                        {
                            var snap = _progressStore[userId];
                            snap.Imported = result.ImportedReleases;
                            snap.LastUpdatedUtc = DateTime.UtcNow;
                            _progressStore[userId] = snap;
                        }
                        
                        _logger.LogDebug("Imported release: {Title} (Discogs ID: {DiscogsId})", 
                            musicRelease.Title, musicRelease.DiscogsId);
                    }
                    else
                    {
                        result.FailedReleases++;
                        if (_progressStore.ContainsKey(userId))
                        {
                            var snap = _progressStore[userId];
                            snap.Failed = result.FailedReleases;
                            snap.LastUpdatedUtc = DateTime.UtcNow;
                            _progressStore[userId] = snap;
                        }
                        var errorMsg = $"Failed to map release: {release.BasicInformation.Title}";
                        result.Errors.Add(errorMsg);
                        _logger.LogWarning("{ErrorMsg} (Discogs ID: {DiscogsId})", errorMsg, release.BasicInformation.Id);
                    }
                }
                catch (Exception ex)
                {
                    result.FailedReleases++;
                    if (_progressStore.ContainsKey(userId))
                    {
                        var snap = _progressStore[userId];
                        snap.Failed = result.FailedReleases;
                        snap.LastUpdatedUtc = DateTime.UtcNow;
                        _progressStore[userId] = snap;
                    }
                    var title = release.BasicInformation?.Title ?? "Unknown";
                    result.Errors.Add($"Error importing '{title}': {ex.Message}");
                    _logger.LogError(ex, "Error importing release: {Title}", title);
                }
            }
        }

        private async Task<MusicRelease?> MapToMusicReleaseAsync(DiscogsCollectionReleaseDto release, Guid userId)
        {
            if (release.BasicInformation == null) return null;

            var basicInfo = release.BasicInformation;

            try
            {
                // Fetch full release details to get tracklist
                // Add delay to respect Discogs rate limit (60 requests/minute = 1 request per second)
                DiscogsReleaseDto? fullRelease = null;
                if (basicInfo.Id > 0)
                {
                    try
                    {
                        await Task.Delay(1100); // 1.1 second delay to stay safely under rate limit
                        fullRelease = await _discogsService.GetReleaseDetailsAsync(basicInfo.Id.ToString());
                        _logger.LogDebug("Fetched full details for release {Title} (ID: {Id})", basicInfo.Title, basicInfo.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to fetch full details for release {Title} (ID: {Id}) - continuing without tracklist", basicInfo.Title, basicInfo.Id);
                        // Continue without tracklist rather than failing the entire import
                    }
                }

                // Resolve or create lookups (these methods now save immediately if creating new entities)
                var formatId = await GetOrCreateFormatAsync(basicInfo.Formats, userId);
                var labelId = await GetOrCreateLabelAsync(basicInfo.Labels, userId);
                var countryId = await GetOrCreateCountryAsync(basicInfo.Country, userId);
                var artistIds = await GetOrCreateArtistsAsync(basicInfo.Artists, userId);
                var genreIds = await GetOrCreateGenresAsync(basicInfo.Genres, basicInfo.Styles, userId);

                // Download cover art and upload to R2
                string? coverImageFilename = null;
                if (!string.IsNullOrEmpty(basicInfo.CoverImage))
                {
                    var artist = basicInfo.Artists?.FirstOrDefault()?.Name ?? "Unknown";
                    var year = basicInfo.Year?.ToString();
                    _logger.LogDebug("Attempting to download and store cover art for Discogs ID {Id} - {Title}", basicInfo.Id, basicInfo.Title);
                    var rawFilename = await _imageService.DownloadAndStoreCoverArtAsync(
                        basicInfo.CoverImage, artist, basicInfo.Title ?? "Unknown", year, userId);
                    if (!string.IsNullOrEmpty(rawFilename))
                    {
                        coverImageFilename = $"/cover-art/{userId}/{rawFilename}";
                    }
                    else
                    {
                        _logger.LogWarning("Cover art upload failed or was skipped for Discogs ID {Id}. Will attempt fallback to Discogs-hosted image URL if available.", basicInfo.Id);
                    }
                }

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

                // Create MusicRelease entity
                var musicRelease = new MusicRelease
                {
                    UserId = userId,
                    DiscogsId = basicInfo.Id,
                    Title = basicInfo.Title ?? string.Empty,
                    ReleaseYear = releaseYear,
                    FormatId = formatId,
                    LabelId = labelId,
                    CountryId = countryId,
                    Artists = artistIds.Count > 0 ? JsonSerializer.Serialize(artistIds) : null,
                    Genres = genreIds.Count > 0 ? JsonSerializer.Serialize(genreIds) : null,
                    Notes = notes,
                    DateAdded = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };

                // Set cover image if downloaded
                if (!string.IsNullOrEmpty(coverImageFilename))
                {
                    var images = new { CoverFront = coverImageFilename };
                    musicRelease.Images = JsonSerializer.Serialize(images);
                }
                else
                {
                    // Fallback: if upload failed, try to store Discogs-hosted cover/thumb URL so UI can fall back to it
                    var fallbackUrl = basicInfo.CoverImage ?? basicInfo.Thumb;
                    if (!string.IsNullOrEmpty(fallbackUrl))
                    {
                        _logger.LogInformation("Falling back to Discogs-hosted image for Discogs ID {Id}: {Url}", basicInfo.Id, fallbackUrl);
                        var images = new { CoverFront = fallbackUrl };
                        musicRelease.Images = JsonSerializer.Serialize(images);
                    }
                }

                // Build Media (tracks) from full release details
                if (fullRelease != null && fullRelease.Tracklist != null && fullRelease.Tracklist.Count > 0)
                {
                    _logger.LogInformation("Building tracklist for {Title} - {TrackCount} tracks found", basicInfo.Title, fullRelease.Tracklist.Count);
                    var media = BuildMediaFromTracklist(fullRelease.Tracklist, basicInfo.Title ?? string.Empty, formatId, artistIds, genreIds, releaseYear);
                    if (media != null)
                    {
                        musicRelease.Media = JsonSerializer.Serialize(media);
                        _logger.LogInformation("Successfully added tracklist to {Title}", basicInfo.Title);
                    }
                    else
                    {
                        _logger.LogWarning("BuildMediaFromTracklist returned null for {Title}", basicInfo.Title);
                    }
                }
                else
                {
                    if (fullRelease == null)
                    {
                        _logger.LogWarning("No full release data fetched for {Title} - skipping tracklist", basicInfo.Title);
                    }
                    else if (fullRelease.Tracklist == null || fullRelease.Tracklist.Count == 0)
                    {
                        _logger.LogInformation("Release {Title} has no tracklist in Discogs data", basicInfo.Title);
                    }
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
