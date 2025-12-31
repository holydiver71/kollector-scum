using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// Service for importing Discogs collections into the application
    /// </summary>
    public class DiscogsCollectionImportService : IDiscogsCollectionImportService
    {
        private readonly IDiscogsService _discogsService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DiscogsCollectionImportService> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly string _coverArtPath;

        // Cache for lookups created during import to avoid duplicates
        private Dictionary<string, int> _artistCache = new();
        private Dictionary<string, int> _genreCache = new();
        private Dictionary<string, int> _formatCache = new();
        private Dictionary<string, int> _labelCache = new();
        private Dictionary<string, int> _countryCache = new();

        // Constants for filename sanitization
        private const int MaxFilenameLength = 200;

        public DiscogsCollectionImportService(
            IDiscogsService discogsService,
            IUnitOfWork unitOfWork,
            ILogger<DiscogsCollectionImportService> logger,
            IConfiguration configuration,
            HttpClient httpClient)
        {
            _discogsService = discogsService ?? throw new ArgumentNullException(nameof(discogsService));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            
            _coverArtPath = _configuration["CoverArtPath"] ?? "wwwroot/images/covers";
            
            // Ensure cover art directory exists
            Directory.CreateDirectory(_coverArtPath);
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

                // Process first page
                await ProcessReleasesAsync(firstPage.Releases, userId, result);

                // Process remaining pages
                var totalPages = firstPage.Pagination.Pages;
                for (int page = 2; page <= totalPages; page++)
                {
                    _logger.LogInformation("Processing page {Page} of {TotalPages}", page, totalPages);
                    
                    var pageData = await _discogsService.GetUserCollectionAsync(username, page, 100);
                    if (pageData?.Releases != null)
                    {
                        await ProcessReleasesAsync(pageData.Releases, userId, result);
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

        private async Task ProcessReleasesAsync(List<DiscogsCollectionReleaseDto> releases, Guid userId, DiscogsImportResult result)
        {
            if (releases == null || releases.Count == 0)
            {
                _logger.LogWarning("No releases to process");
                return;
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
                        continue;
                    }

                    // Map and import the release
                    var musicRelease = await MapToMusicReleaseAsync(release, userId);
                    if (musicRelease != null)
                    {
                        await _unitOfWork.MusicReleases.AddAsync(musicRelease);
                        await _unitOfWork.SaveChangesAsync();
                        result.ImportedReleases++;
                        
                        _logger.LogDebug("Imported release: {Title} (Discogs ID: {DiscogsId})", 
                            musicRelease.Title, musicRelease.DiscogsId);
                    }
                    else
                    {
                        result.FailedReleases++;
                        var errorMsg = $"Failed to map release: {release.BasicInformation.Title}";
                        result.Errors.Add(errorMsg);
                        _logger.LogWarning("{ErrorMsg} (Discogs ID: {DiscogsId})", errorMsg, release.BasicInformation.Id);
                    }
                }
                catch (Exception ex)
                {
                    result.FailedReleases++;
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
                // Resolve or create lookups (these methods now save immediately if creating new entities)
                var formatId = await GetOrCreateFormatAsync(basicInfo.Formats, userId);
                var labelId = await GetOrCreateLabelAsync(basicInfo.Labels, userId);
                var countryId = await GetOrCreateCountryAsync(basicInfo.Country, userId);
                var artistIds = await GetOrCreateArtistsAsync(basicInfo.Artists, userId);
                var genreIds = await GetOrCreateGenresAsync(basicInfo.Genres, basicInfo.Styles, userId);

                // Download cover art
                string? coverImageFilename = null;
                if (!string.IsNullOrEmpty(basicInfo.CoverImage))
                {
                    coverImageFilename = await DownloadCoverArtAsync(basicInfo.CoverImage, basicInfo);
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
                    Title = basicInfo.Title,
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

                return musicRelease;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error mapping release to MusicRelease: {Title}", basicInfo.Title);
                return null;
            }
        }

        private async Task<int?> GetOrCreateFormatAsync(List<DiscogsFormatDto> formats, Guid userId)
        {
            if (formats == null || formats.Count == 0) return null;

            var formatName = formats.First().Name;
            if (string.IsNullOrEmpty(formatName)) return null;

            var cacheKey = $"{userId}:{formatName}";

            // Check cache first
            if (_formatCache.TryGetValue(cacheKey, out var cachedId))
            {
                return cachedId;
            }

            // Try to find existing format
            var existing = await _unitOfWork.Formats
                .GetAsync(f => f.UserId == userId && f.Name == formatName);
            
            if (existing.Any())
            {
                var id = existing.First().Id;
                _formatCache[cacheKey] = id;
                return id;
            }

            // Create new format
            var format = new Format { UserId = userId, Name = formatName };
            await _unitOfWork.Formats.AddAsync(format);
            await _unitOfWork.SaveChangesAsync(); // Save immediately to get the generated ID
            _formatCache[cacheKey] = format.Id;
            
            return format.Id;
        }

        private async Task<int?> GetOrCreateLabelAsync(List<DiscogsLabelDto> labels, Guid userId)
        {
            if (labels == null || labels.Count == 0) return null;

            var labelName = labels.First().Name;
            if (string.IsNullOrEmpty(labelName)) return null;

            var cacheKey = $"{userId}:{labelName}";

            // Check cache first
            if (_labelCache.TryGetValue(cacheKey, out var cachedId))
            {
                return cachedId;
            }

            // Try to find existing label
            var existing = await _unitOfWork.Labels
                .GetAsync(l => l.UserId == userId && l.Name == labelName);
            
            if (existing.Any())
            {
                var id = existing.First().Id;
                _labelCache[cacheKey] = id;
                return id;
            }

            // Create new label
            var label = new Label { UserId = userId, Name = labelName };
            await _unitOfWork.Labels.AddAsync(label);
            await _unitOfWork.SaveChangesAsync(); // Save immediately to get the generated ID
            _labelCache[cacheKey] = label.Id;
            
            return label.Id;
        }

        private async Task<int?> GetOrCreateCountryAsync(string? countryName, Guid userId)
        {
            if (string.IsNullOrEmpty(countryName)) return null;

            var cacheKey = $"{userId}:{countryName}";

            // Check cache first
            if (_countryCache.TryGetValue(cacheKey, out var cachedId))
            {
                return cachedId;
            }

            // Try to find existing country
            var existing = await _unitOfWork.Countries
                .GetAsync(c => c.UserId == userId && c.Name == countryName);
            
            if (existing.Any())
            {
                var id = existing.First().Id;
                _countryCache[cacheKey] = id;
                return id;
            }

            // Create new country
            var country = new Country { UserId = userId, Name = countryName };
            await _unitOfWork.Countries.AddAsync(country);
            await _unitOfWork.SaveChangesAsync(); // Save immediately to get the generated ID
            _countryCache[cacheKey] = country.Id;
            
            return country.Id;
        }

        private async Task<List<int>> GetOrCreateArtistsAsync(List<DiscogsArtistDto> artists, Guid userId)
        {
            var artistIds = new List<int>();
            
            if (artists == null || artists.Count == 0) return artistIds;

            foreach (var artist in artists)
            {
                if (string.IsNullOrEmpty(artist.Name)) continue;

                var cacheKey = $"{userId}:{artist.Name}";

                // Check cache first
                if (_artistCache.TryGetValue(cacheKey, out var cachedId))
                {
                    artistIds.Add(cachedId);
                    continue;
                }

                // Try to find existing artist in database
                var existing = await _unitOfWork.Artists
                    .GetAsync(a => a.UserId == userId && a.Name == artist.Name);
                
                if (existing.Any())
                {
                    var id = existing.First().Id;
                    _artistCache[cacheKey] = id;
                    artistIds.Add(id);
                }
                else
                {
                    // Create new artist
                    var newArtist = new Artist { UserId = userId, Name = artist.Name };
                    await _unitOfWork.Artists.AddAsync(newArtist);
                    await _unitOfWork.SaveChangesAsync(); // Save immediately to get the generated ID
                    _artistCache[cacheKey] = newArtist.Id;
                    artistIds.Add(newArtist.Id);
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

                var cacheKey = $"{userId}:{genreName}";

                // Check cache first
                if (_genreCache.TryGetValue(cacheKey, out var cachedId))
                {
                    genreIds.Add(cachedId);
                    continue;
                }

                // Try to find existing genre
                var existing = await _unitOfWork.Genres
                    .GetAsync(g => g.UserId == userId && g.Name == genreName);
                
                if (existing.Any())
                {
                    var id = existing.First().Id;
                    _genreCache[cacheKey] = id;
                    genreIds.Add(id);
                }
                else
                {
                    // Create new genre
                    var newGenre = new Genre { UserId = userId, Name = genreName };
                    await _unitOfWork.Genres.AddAsync(newGenre);
                    await _unitOfWork.SaveChangesAsync(); // Save immediately to get the generated ID
                    _genreCache[cacheKey] = newGenre.Id;
                    genreIds.Add(newGenre.Id);
                }
            }

            return genreIds;
        }

        private async Task<string?> DownloadCoverArtAsync(string imageUrl, DiscogsBasicInfoDto basicInfo)
        {
            try
            {
                // Sanitize filename: {Artist}-{Title}-{Year}.jpg
                var artist = basicInfo.Artists?.FirstOrDefault()?.Name ?? "Unknown";
                var title = basicInfo.Title ?? "Unknown";
                var year = basicInfo.Year?.ToString() ?? "Unknown";
                
                var filename = SanitizeFilename($"{artist}-{title}-{year}.jpg");
                var filePath = Path.Combine(_coverArtPath, filename);

                // Skip if already exists
                if (File.Exists(filePath))
                {
                    _logger.LogDebug("Cover art already exists: {Filename}", filename);
                    return filename;
                }

                // Download image
                _logger.LogDebug("Downloading cover art from: {Url}", imageUrl);
                var response = await _httpClient.GetAsync(imageUrl);
                
                if (response.IsSuccessStatusCode)
                {
                    var imageBytes = await response.Content.ReadAsByteArrayAsync();
                    await File.WriteAllBytesAsync(filePath, imageBytes);
                    _logger.LogDebug("Saved cover art: {Filename}", filename);
                    return filename;
                }
                else
                {
                    _logger.LogWarning("Failed to download cover art from {Url}: {StatusCode}", 
                        imageUrl, response.StatusCode);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading cover art from {Url}", imageUrl);
                return null;
            }
        }

        private string SanitizeFilename(string filename)
        {
            // Remove invalid characters
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new StringBuilder();
            
            foreach (var c in filename)
            {
                if (!invalidChars.Contains(c))
                {
                    sanitized.Append(c);
                }
                else
                {
                    sanitized.Append('_');
                }
            }

            // Limit length
            var result = sanitized.ToString();
            if (result.Length > MaxFilenameLength)
            {
                var extension = Path.GetExtension(result);
                result = result.Substring(0, MaxFilenameLength - extension.Length) + extension;
            }

            return result;
        }
    }
}
