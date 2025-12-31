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

        public UserProfileRepository(
            KollectorScumDbContext context,
            IConfiguration configuration,
            ILogger<UserProfileRepository> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
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
