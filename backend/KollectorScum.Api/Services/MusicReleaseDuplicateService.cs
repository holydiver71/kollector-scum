using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// Service for detecting duplicate music releases
    /// </summary>
    public class MusicReleaseDuplicateService : IMusicReleaseDuplicateService
    {
        private readonly IRepository<MusicRelease> _musicReleaseRepository;
        private readonly ILogger<MusicReleaseDuplicateService> _logger;
        private readonly IUserContext _userContext;

        public MusicReleaseDuplicateService(
            IRepository<MusicRelease> musicReleaseRepository,
            ILogger<MusicReleaseDuplicateService> logger,
            IUserContext userContext)
        {
            _musicReleaseRepository = musicReleaseRepository ?? throw new ArgumentNullException(nameof(musicReleaseRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        }

        /// <summary>
        /// Check for potential duplicate releases based on title, catalog number, or artist
        /// </summary>
        public async Task<List<MusicRelease>> CheckForDuplicatesAsync(
            string title, 
            string? labelNumber, 
            List<int>? artistIds,
            int? excludeReleaseId = null)
        {
            _logger.LogInformation("Checking for duplicates - Title: {Title}, LabelNumber: {LabelNumber}", title, labelNumber);

            var userId = _userContext.GetActingUserId();
            if (!userId.HasValue) return new List<MusicRelease>();

            var duplicates = new List<MusicRelease>();

            // Check by catalog number first (most reliable)
            if (!string.IsNullOrWhiteSpace(labelNumber))
            {
                var catalogDuplicates = await CheckByCatalogNumberAsync(labelNumber, excludeReleaseId, userId.Value);
                duplicates.AddRange(catalogDuplicates);
            }

            // If no catalog matches, check by title and artist
            if (!duplicates.Any() && artistIds != null && artistIds.Any())
            {
                var titleArtistDuplicates = await CheckByTitleAndArtistAsync(title, artistIds, excludeReleaseId, userId.Value);
                duplicates.AddRange(titleArtistDuplicates);
            }

            return duplicates.Distinct().ToList();
        }

        /// <summary>
        /// Check for duplicates by catalog number
        /// </summary>
        private async Task<List<MusicRelease>> CheckByCatalogNumberAsync(string labelNumber, int? excludeReleaseId, Guid userId)
        {
            var normalizedCatalog = labelNumber.Trim().ToLower();
            var catalogMatches = await _musicReleaseRepository.GetAsync(
                filter: r => r.UserId == userId && r.LabelNumber != null && r.LabelNumber.ToLower() == normalizedCatalog);

            if (excludeReleaseId.HasValue)
            {
                catalogMatches = catalogMatches.Where(r => r.Id != excludeReleaseId.Value).ToList();
            }

            if (catalogMatches.Any())
            {
                _logger.LogWarning("Found {Count} duplicate(s) by catalog number: {LabelNumber}", 
                    catalogMatches.Count(), labelNumber);
            }

            return catalogMatches.ToList();
        }

        /// <summary>
        /// Check for duplicates by title and artist
        /// </summary>
        private async Task<List<MusicRelease>> CheckByTitleAndArtistAsync(
            string title, 
            List<int> artistIds, 
            int? excludeReleaseId,
            Guid userId)
        {
            var normalizedTitle = title.Trim().ToLower();
            var allReleases = await _musicReleaseRepository.GetAsync(r => r.UserId == userId);
            
            var titleArtistMatches = allReleases.Where(r =>
            {
                // Exclude the current release if updating
                if (excludeReleaseId.HasValue && r.Id == excludeReleaseId.Value)
                    return false;

                // Check title match
                if (r.Title.Trim().ToLower() != normalizedTitle)
                    return false;

                // Check artist match
                if (string.IsNullOrEmpty(r.Artists))
                    return false;

                try
                {
                    var releaseArtistIds = JsonSerializer.Deserialize<List<int>>(r.Artists);
                    return releaseArtistIds != null && releaseArtistIds.Intersect(artistIds).Any();
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize artists for release {ReleaseId}", r.Id);
                    return false;
                }
            }).ToList();

            if (titleArtistMatches.Any())
            {
                _logger.LogWarning("Found {Count} duplicate(s) by title and artist: {Title}", 
                    titleArtistMatches.Count, title);
            }

            return titleArtistMatches;
        }

        /// <summary>
        /// Check if a release would be a duplicate
        /// </summary>
        public async Task<bool> IsDuplicateAsync(
            string title, 
            string? labelNumber, 
            List<int>? artistIds,
            int? excludeReleaseId = null)
        {
            var duplicates = await CheckForDuplicatesAsync(title, labelNumber, artistIds, excludeReleaseId);
            return duplicates.Any();
        }
    }
}
