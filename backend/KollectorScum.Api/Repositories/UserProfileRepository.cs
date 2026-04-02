using KollectorScum.Api.Data;
using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace KollectorScum.Api.Repositories
{
    /// <summary>
    /// Repository implementation for UserProfile operations
    /// </summary>
    public class UserProfileRepository : IUserProfileRepository
    {
        private readonly KollectorScumDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UserProfileRepository> _logger;
        private readonly Interfaces.ICacheService? _cacheService;

        public UserProfileRepository(
            KollectorScumDbContext context,
            IConfiguration configuration,
            ILogger<UserProfileRepository> logger,
            Interfaces.ICacheService? cacheService = null)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _cacheService = cacheService;
        }

        /// <inheritdoc />
        public async Task<UserProfile?> GetByUserIdAsync(Guid userId)
        {
            return await _context.UserProfiles
                .Include(up => up.SelectedKollection)
                .FirstOrDefaultAsync(up => up.UserId == userId);
        }

        /// <inheritdoc />
        public async Task<UserProfile> CreateAsync(UserProfile profile)
        {
            _context.UserProfiles.Add(profile);
            await _context.SaveChangesAsync();
            return profile;
        }

        /// <inheritdoc />
        public async Task<UserProfile> UpdateAsync(UserProfile profile)
        {
            _context.UserProfiles.Update(profile);
            await _context.SaveChangesAsync();
            return profile;
        }

        /// <inheritdoc />
        public async Task<bool> KollectionExistsAsync(int kollectionId)
        {
            return await _context.Kollections.AnyAsync(k => k.Id == kollectionId);
        }

        /// <inheritdoc />
        public async Task<int> GetUserMusicReleaseCountAsync(Guid userId)
        {
            return await _context.MusicReleases.CountAsync(mr => mr.UserId == userId);
        }

        /// <inheritdoc />
        public async Task<int> DeleteAllUserMusicReleasesAsync(Guid userId)
        {
            var releases = await _context.MusicReleases
                .Where(mr => mr.UserId == userId)
                .ToListAsync();

            var count = releases.Count;

            // Delete image files for each release before deleting database records
            foreach (var release in releases)
            {
                await DeleteImageFilesAsync(release);
            }

            _context.MusicReleases.RemoveRange(releases);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted {Count} releases and their associated image files for user {UserId}", count, userId);

            // Also remove user-owned lookup data (artists, genres, labels) so the
            // user's dashboard and lookup lists do not show stale entries after a
            // full collection wipe. These lookup tables are per-user (contain
            // UserId) so it's safe to delete them here.
            var deletedLookups = 0;

            var userArtists = await _context.Artists.Where(a => a.UserId == userId).ToListAsync();
            if (userArtists.Any())
            {
                deletedLookups += userArtists.Count;
                _context.Artists.RemoveRange(userArtists);
            }

            var userGenres = await _context.Genres.Where(g => g.UserId == userId).ToListAsync();
            if (userGenres.Any())
            {
                deletedLookups += userGenres.Count;
                _context.Genres.RemoveRange(userGenres);
            }

            var userLabels = await _context.Labels.Where(l => l.UserId == userId).ToListAsync();
            if (userLabels.Any())
            {
                deletedLookups += userLabels.Count;
                _context.Labels.RemoveRange(userLabels);
            }

            if (deletedLookups > 0)
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Deleted {LookupCount} lookup rows (artists/genres/labels) for user {UserId}", deletedLookups, userId);

                // Invalidate cache groups for lookup lists so API responses reflect
                // the deletions immediately (GenericCrudService caches paged
                // lookup results per user under the group key). Use the same
                // cache group naming convention as GenericCrudService.
                try
                {
                    var groupArtist = $"{nameof(Models.Artist)}:all:{userId}";
                    var groupGenre = $"{nameof(Models.Genre)}:all:{userId}";
                    var groupLabel = $"{nameof(Models.Label)}:all:{userId}";
                    _cacheService?.InvalidateGroup(groupArtist);
                    _cacheService?.InvalidateGroup(groupGenre);
                    _cacheService?.InvalidateGroup(groupLabel);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to invalidate lookup cache groups for user {UserId}", userId);
                }
            }

            return count;
        }

        /// <summary>
        /// Deletes image files associated with a music release
        /// </summary>
        private async Task DeleteImageFilesAsync(MusicRelease musicRelease)
        {
            if (string.IsNullOrWhiteSpace(musicRelease.Images))
            {
                return;
            }

            try
            {
                var imagesPath = _configuration["ImagesPath"] ?? "wwwroot/images";
                var coversPath = Path.Combine(imagesPath, "covers");
                var thumbnailsPath = Path.Combine(imagesPath, "thumbnails");

                // Parse the Images JSON
                var imageData = JsonSerializer.Deserialize<MusicReleaseImageDto>(musicRelease.Images);
                if (imageData == null)
                {
                    _logger.LogWarning("Failed to deserialize images JSON for release ID: {Id}", musicRelease.Id);
                    return;
                }

                // Delete front cover
                if (!string.IsNullOrWhiteSpace(imageData.CoverFront))
                {
                    var filename = ExtractFilenameFromUrl(imageData.CoverFront);
                    var fullPath = Path.Combine(coversPath, filename);
                    DeleteImageFile(fullPath, "front cover");
                }

                // Delete back cover
                if (!string.IsNullOrWhiteSpace(imageData.CoverBack))
                {
                    var filename = ExtractFilenameFromUrl(imageData.CoverBack);
                    var fullPath = Path.Combine(coversPath, filename);
                    DeleteImageFile(fullPath, "back cover");
                }

                // Delete thumbnail (stored in separate thumbnails folder)
                if (!string.IsNullOrWhiteSpace(imageData.Thumbnail))
                {
                    var filename = ExtractFilenameFromUrl(imageData.Thumbnail);
                    var fullPath = Path.Combine(thumbnailsPath, filename);
                    DeleteImageFile(fullPath, "thumbnail");
                }

                await Task.CompletedTask; // Make method async-compatible
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing or deleting image files for release: {Id}", musicRelease.Id);
                // Don't fail the entire delete operation if image deletion fails
            }
        }

        /// <summary>
        /// Extracts the filename from a URL (e.g., "http://localhost:5072/api/images/covers/file.jpg" -> "file.jpg")
        /// </summary>
        private string ExtractFilenameFromUrl(string urlOrFilename)
        {
            // If it's already just a filename (no protocol), return as-is
            if (!urlOrFilename.StartsWith("http://") && !urlOrFilename.StartsWith("https://"))
            {
                return urlOrFilename;
            }

            // Extract filename from URL
            try
            {
                var uri = new Uri(urlOrFilename);
                return Path.GetFileName(uri.LocalPath);
            }
            catch
            {
                // If URL parsing fails, try to get the part after the last slash
                var lastSlashIndex = urlOrFilename.LastIndexOf('/');
                return lastSlashIndex >= 0 ? urlOrFilename.Substring(lastSlashIndex + 1) : urlOrFilename;
            }
        }

        /// <summary>
        /// Helper method to delete a single image file
        /// </summary>
        private void DeleteImageFile(string fullPath, string imageType)
        {
            try
            {
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    _logger.LogInformation("Deleted {ImageType} file: {FilePath}", imageType, fullPath);
                }
                else
                {
                    _logger.LogDebug("{ImageType} file not found (skipping): {FilePath}", imageType, fullPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete {ImageType} file: {FilePath}", imageType, fullPath);
                // Don't throw - we want to continue deleting other files
            }
        }
    }
}
