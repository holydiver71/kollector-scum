using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using Microsoft.Extensions.Logging;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// Service for handling music release search and suggestions
    /// </summary>
    public class MusicReleaseSearchService : IMusicReleaseSearchService
    {
        private readonly IRepository<MusicRelease> _musicReleaseRepository;
        private readonly IRepository<Artist> _artistRepository;
        private readonly IRepository<Label> _labelRepository;
        private readonly ILogger<MusicReleaseSearchService> _logger;

        public MusicReleaseSearchService(
            IRepository<MusicRelease> musicReleaseRepository,
            IRepository<Artist> artistRepository,
            IRepository<Label> labelRepository,
            ILogger<MusicReleaseSearchService> logger)
        {
            _musicReleaseRepository = musicReleaseRepository ?? throw new ArgumentNullException(nameof(musicReleaseRepository));
            _artistRepository = artistRepository ?? throw new ArgumentNullException(nameof(artistRepository));
            _labelRepository = labelRepository ?? throw new ArgumentNullException(nameof(labelRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Get search suggestions for autocomplete
        /// </summary>
        public async Task<List<SearchSuggestionDto>> GetSearchSuggestionsAsync(string query, int limit)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            {
                return new List<SearchSuggestionDto>();
            }

            _logger.LogInformation("Getting search suggestions for query: {Query}", query);

            var queryLower = query.ToLower();
            var suggestions = new List<SearchSuggestionDto>();

            // Get release title suggestions
            var releases = await GetReleaseSuggestionsAsync(queryLower, limit);
            suggestions.AddRange(releases);

            // Get artist suggestions
            var artists = await GetArtistSuggestionsAsync(queryLower, limit);
            suggestions.AddRange(artists);

            // Get label suggestions
            var labels = await GetLabelSuggestionsAsync(queryLower, limit);
            suggestions.AddRange(labels);

            return RankAndLimitSuggestions(suggestions, queryLower, limit);
        }

        private async Task<List<SearchSuggestionDto>> GetReleaseSuggestionsAsync(string queryLower, int limit)
        {
            var releases = await _musicReleaseRepository.GetAsync(
                mr => mr.Title.ToLower().Contains(queryLower),
                mr => mr.OrderBy(x => x.Title)
            );

            return releases.Take(limit).Select(r => new SearchSuggestionDto
            {
                Type = "release",
                Id = r.Id,
                Name = r.Title,
                Subtitle = r.ReleaseYear?.Year.ToString()
            }).ToList();
        }

        private async Task<List<SearchSuggestionDto>> GetArtistSuggestionsAsync(string queryLower, int limit)
        {
            var artists = await _artistRepository.GetAsync(
                a => a.Name.ToLower().Contains(queryLower),
                a => a.OrderBy(x => x.Name)
            );

            return artists.Take(limit).Select(a => new SearchSuggestionDto
            {
                Type = "artist",
                Id = a.Id,
                Name = a.Name
            }).ToList();
        }

        private async Task<List<SearchSuggestionDto>> GetLabelSuggestionsAsync(string queryLower, int limit)
        {
            var labels = await _labelRepository.GetAsync(
                l => l.Name.ToLower().Contains(queryLower),
                l => l.OrderBy(x => x.Name)
            );

            return labels.Take(limit).Select(l => new SearchSuggestionDto
            {
                Type = "label",
                Id = l.Id,
                Name = l.Name
            }).ToList();
        }

        private List<SearchSuggestionDto> RankAndLimitSuggestions(
            List<SearchSuggestionDto> suggestions, 
            string queryLower, 
            int limit)
        {
            return suggestions
                .OrderBy(s => !s.Name.ToLower().StartsWith(queryLower))
                .ThenBy(s => s.Name)
                .Take(limit)
                .ToList();
        }
    }
}
