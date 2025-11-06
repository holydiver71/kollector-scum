using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// Service for music release read operations (queries)
    /// Handles: GET operations, search, statistics
    /// </summary>
    public class MusicReleaseQueryService : IMusicReleaseQueryService
    {
        private readonly IRepository<MusicRelease> _musicReleaseRepository;
        private readonly IRepository<Artist> _artistRepository;
        private readonly IRepository<Label> _labelRepository;
        private readonly IMusicReleaseMapperService _mapper;
        private readonly ICollectionStatisticsService _statisticsService;
        private readonly ILogger<MusicReleaseQueryService> _logger;

        public MusicReleaseQueryService(
            IRepository<MusicRelease> musicReleaseRepository,
            IRepository<Artist> artistRepository,
            IRepository<Label> labelRepository,
            IMusicReleaseMapperService mapper,
            ICollectionStatisticsService statisticsService,
            ILogger<MusicReleaseQueryService> logger)
        {
            _musicReleaseRepository = musicReleaseRepository ?? throw new ArgumentNullException(nameof(musicReleaseRepository));
            _artistRepository = artistRepository ?? throw new ArgumentNullException(nameof(artistRepository));
            _labelRepository = labelRepository ?? throw new ArgumentNullException(nameof(labelRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _statisticsService = statisticsService ?? throw new ArgumentNullException(nameof(statisticsService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<PagedResult<MusicReleaseSummaryDto>> GetMusicReleasesAsync(
            string? search, int? artistId, int? genreId, int? labelId, 
            int? countryId, int? formatId, bool? live, int? yearFrom, 
            int? yearTo, int page, int pageSize)
        {
            _logger.LogInformation("Getting music releases - Page: {Page}, PageSize: {PageSize}", page, pageSize);

            Expression<Func<MusicRelease, bool>>? filter = null;

            if (!string.IsNullOrEmpty(search) || artistId.HasValue || genreId.HasValue || 
                labelId.HasValue || countryId.HasValue || formatId.HasValue || live.HasValue ||
                yearFrom.HasValue || yearTo.HasValue)
            {
                filter = mr => 
                    (string.IsNullOrEmpty(search) || mr.Title.ToLower().Contains(search.ToLower())) &&
                    (!artistId.HasValue || (mr.Artists != null && mr.Artists.Contains(artistId.Value.ToString()))) &&
                    (!genreId.HasValue || (mr.Genres != null && mr.Genres.Contains(genreId.Value.ToString()))) &&
                    (!labelId.HasValue || mr.LabelId == labelId.Value) &&
                    (!countryId.HasValue || mr.CountryId == countryId.Value) &&
                    (!formatId.HasValue || mr.FormatId == formatId.Value) &&
                    (!live.HasValue || mr.Live == live.Value) &&
                    (!yearFrom.HasValue || (mr.ReleaseYear.HasValue && mr.ReleaseYear.Value.Year >= yearFrom.Value)) &&
                    (!yearTo.HasValue || (mr.ReleaseYear.HasValue && mr.ReleaseYear.Value.Year <= yearTo.Value));
            }

            var pagedResult = await _musicReleaseRepository.GetPagedAsync(
                page,
                pageSize,
                filter,
                mr => mr.OrderBy(x => x.Title),
                "Label,Country,Format"
            );

            var summaryDtos = await Task.Run(() => pagedResult.Items.Select(mr => _mapper.MapToSummaryDto(mr)).ToList());

            return new PagedResult<MusicReleaseSummaryDto>
            {
                Items = summaryDtos,
                Page = pagedResult.Page,
                PageSize = pagedResult.PageSize,
                TotalCount = pagedResult.TotalCount,
                TotalPages = pagedResult.TotalPages
            };
        }

        public async Task<MusicReleaseDto?> GetMusicReleaseAsync(int id)
        {
            _logger.LogInformation("Getting music release by ID: {Id}", id);

            var musicRelease = await _musicReleaseRepository.GetByIdAsync(id, "Label,Country,Format,Packaging");

            if (musicRelease == null)
            {
                _logger.LogWarning("Music release not found: {Id}", id);
                return null;
            }

            return await _mapper.MapToFullDtoAsync(musicRelease);
        }

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
            var releases = await _musicReleaseRepository.GetAsync(
                mr => mr.Title.ToLower().Contains(queryLower),
                mr => mr.OrderBy(x => x.Title)
            );

            suggestions.AddRange(releases.Take(limit).Select(r => new SearchSuggestionDto
            {
                Type = "release",
                Id = r.Id,
                Name = r.Title,
                Subtitle = r.ReleaseYear?.Year.ToString()
            }));

            // Get artist suggestions
            var artists = await _artistRepository.GetAsync(
                a => a.Name.ToLower().Contains(queryLower),
                a => a.OrderBy(x => x.Name)
            );

            suggestions.AddRange(artists.Take(limit).Select(a => new SearchSuggestionDto
            {
                Type = "artist",
                Id = a.Id,
                Name = a.Name
            }));

            // Get label suggestions
            var labels = await _labelRepository.GetAsync(
                l => l.Name.ToLower().Contains(queryLower),
                l => l.OrderBy(x => x.Name)
            );

            suggestions.AddRange(labels.Take(limit).Select(l => new SearchSuggestionDto
            {
                Type = "label",
                Id = l.Id,
                Name = l.Name
            }));

            return suggestions
                .OrderBy(s => !s.Name.ToLower().StartsWith(queryLower))
                .ThenBy(s => s.Name)
                .Take(limit)
                .ToList();
        }

        public async Task<CollectionStatisticsDto> GetCollectionStatisticsAsync()
        {
            return await _statisticsService.GetCollectionStatisticsAsync();
        }
    }
}
