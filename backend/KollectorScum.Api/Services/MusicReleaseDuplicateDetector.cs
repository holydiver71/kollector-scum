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
        private readonly IRepository<Artist> _artistRepository;
        private readonly ILogger<MusicReleaseDuplicateDetector> _logger;
        private readonly IUserContext _userContext;

        public MusicReleaseDuplicateDetector(
            IRepository<MusicRelease> musicReleaseRepository,
            IRepository<Artist> artistRepository,
            ILogger<MusicReleaseDuplicateDetector> logger,
            IUserContext userContext)
        {
            _musicReleaseRepository = musicReleaseRepository ?? throw new ArgumentNullException(nameof(musicReleaseRepository));
            _artistRepository = artistRepository ?? throw new ArgumentNullException(nameof(artistRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        }

        public async Task<List<MusicRelease>> FindDuplicatesAsync(
            string? catalogNumber, 
            string title, 
            List<string>? artistNames)
        {
            _logger.LogInformation("Checking for duplicates - Catalog: {Catalog}, Title: {Title}", catalogNumber, title);

            var userId = _userContext.GetActingUserId();
            if (!userId.HasValue) return new List<MusicRelease>();

            var duplicates = new List<MusicRelease>();

            // First check: Exact catalog number match
            if (!string.IsNullOrWhiteSpace(catalogNumber))
            {
                var normalizedCatalog = catalogNumber.Trim().ToLower();
                var catalogMatches = await _musicReleaseRepository.GetAsync(
                    filter: r => r.UserId == userId.Value && r.LabelNumber != null && r.LabelNumber.ToLower() == normalizedCatalog);
                
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
                var normalizedArtistNames = artistNames.Select(a => a.Trim().ToLower()).ToList();
                
                var allReleases = await _musicReleaseRepository.GetAsync(r => r.UserId == userId.Value);
                var allArtists = await _artistRepository.GetAsync(a => a.UserId == userId.Value);
                
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
                        if (releaseArtistIds == null || !releaseArtistIds.Any())
                            return false;
                        
                        // Get the actual artist names for this release
                        var releaseArtistNames = allArtists
                            .Where(a => releaseArtistIds.Contains(a.Id))
                            .Select(a => a.Name.Trim().ToLower())
                            .ToList();
                        
                        // Check if there's significant overlap in artist names
                        // Consider it a duplicate if at least one artist matches
                        var hasMatchingArtist = releaseArtistNames.Any(ra => 
                            normalizedArtistNames.Any(na => na == ra));
                        
                        return hasMatchingArtist;
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
