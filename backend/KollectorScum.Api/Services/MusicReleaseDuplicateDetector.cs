using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// Service for detecting duplicate music releases
    /// Handles: Catalog number matching, title+artist matching
    /// </summary>
    public class MusicReleaseDuplicateDetector : IMusicReleaseDuplicateDetector
    {
        private readonly IRepository<MusicRelease> _musicReleaseRepository;
        private readonly ILogger<MusicReleaseDuplicateDetector> _logger;

        public MusicReleaseDuplicateDetector(
            IRepository<MusicRelease> musicReleaseRepository,
            ILogger<MusicReleaseDuplicateDetector> logger)
        {
            _musicReleaseRepository = musicReleaseRepository ?? throw new ArgumentNullException(nameof(musicReleaseRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<MusicRelease>> FindDuplicatesAsync(
            string? catalogNumber, 
            string title, 
            List<string>? artistNames)
        {
            _logger.LogInformation("Checking for duplicates - Catalog: {Catalog}, Title: {Title}", catalogNumber, title);

            var duplicates = new List<MusicRelease>();

            // First check: Exact catalog number match
            if (!string.IsNullOrWhiteSpace(catalogNumber))
            {
                var normalizedCatalog = catalogNumber.Trim().ToLower();
                var catalogMatches = await _musicReleaseRepository.GetAsync(
                    filter: r => r.LabelNumber != null && r.LabelNumber.ToLower() == normalizedCatalog);
                
                duplicates.AddRange(catalogMatches);
                
                if (duplicates.Any())
                {
                    _logger.LogWarning("Found {Count} duplicate(s) by catalog number: {Catalog}", 
                        duplicates.Count, catalogNumber);
                    return duplicates.Distinct().ToList();
                }
            }

            // Second check: Title + Artist combination (if no catalog matches)
            if (artistNames != null && artistNames.Any())
            {
                var normalizedTitle = title.Trim().ToLower();
                var allReleases = await _musicReleaseRepository.GetAllAsync();
                
                var titleArtistMatches = allReleases.Where(r =>
                {
                    // Check title match
                    if (r.Title.Trim().ToLower() != normalizedTitle)
                        return false;

                    // Check if release has artists
                    if (string.IsNullOrEmpty(r.Artists))
                        return false;

                    try
                    {
                        var releaseArtistIds = JsonSerializer.Deserialize<List<int>>(r.Artists);
                        
                        // For now, just check if there's any overlap in artists
                        // A more sophisticated approach would check artist names
                        return releaseArtistIds != null && releaseArtistIds.Any();
                    }
                    catch
                    {
                        return false;
                    }
                });

                duplicates.AddRange(titleArtistMatches);
                
                if (duplicates.Any())
                {
                    _logger.LogWarning("Found {Count} potential duplicate(s) by title+artist: {Title}", 
                        duplicates.Count, title);
                }
            }

            return duplicates.Distinct().ToList();
        }
    }
}
