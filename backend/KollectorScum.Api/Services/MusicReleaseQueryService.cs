using KollectorScum.Api.Application.Queries;
using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using KollectorScum.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using System.Text.Json;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// Service for music release read operations (queries)
    /// Handles: GET operations, search, statistics
    /// </summary>
    public class MusicReleaseQueryService : IMusicReleaseQueryService
    {
        private readonly IMusicReleaseRepository _musicReleaseRepository;
        private readonly IRepository<Artist> _artistRepository;
        private readonly IRepository<Label> _labelRepository;
        private readonly IMusicReleaseMapperService _mapper;
        private readonly ICollectionStatisticsService _statisticsService;
        private readonly KollectorScumDbContext _context;
        private readonly ILogger<MusicReleaseQueryService> _logger;
        private readonly IUserContext _userContext;
        private readonly IDiscogsService? _discogsService;
        private readonly IGetMusicReleasesQueryHandler _getMusicReleasesHandler;

        private static readonly JsonSerializerOptions CaseInsensitiveJson = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public MusicReleaseQueryService(
            IMusicReleaseRepository musicReleaseRepository,
            IRepository<Artist> artistRepository,
            IRepository<Label> labelRepository,
            IMusicReleaseMapperService mapper,
            ICollectionStatisticsService statisticsService,
            KollectorScumDbContext context,
            ILogger<MusicReleaseQueryService> logger,
            IUserContext userContext,
            IGetMusicReleasesQueryHandler getMusicReleasesHandler,
            IDiscogsService? discogsService = null)
        {
            _musicReleaseRepository = musicReleaseRepository ?? throw new ArgumentNullException(nameof(musicReleaseRepository));
            _artistRepository = artistRepository ?? throw new ArgumentNullException(nameof(artistRepository));
            _labelRepository = labelRepository ?? throw new ArgumentNullException(nameof(labelRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _statisticsService = statisticsService ?? throw new ArgumentNullException(nameof(statisticsService));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
            _getMusicReleasesHandler = getMusicReleasesHandler ?? throw new ArgumentNullException(nameof(getMusicReleasesHandler));
            _discogsService = discogsService;
        }

        /// <summary>
        /// Delegates to <see cref="IGetMusicReleasesQueryHandler"/> to retrieve a paginated
        /// list of music releases matching the given parameters.
        /// </summary>
        public async Task<PagedResult<MusicReleaseSummaryDto>> GetMusicReleasesAsync(
            MusicReleaseQueryParameters parameters,
            CancellationToken cancellationToken = default)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            return await _getMusicReleasesHandler.HandleAsync(
                new GetMusicReleasesQuery(parameters, cancellationToken));
        }

        /// <summary>
        /// Delegates to <see cref="IGetMusicReleasesQueryHandler"/> to retrieve a paginated
        /// list of music releases matching the given parameters.
        /// </summary>
        public async Task<PagedResult<MusicReleaseSummaryDto>> GetMusicReleasesAsync(MusicReleaseQueryParameters parameters)
            => await GetMusicReleasesAsync(parameters, CancellationToken.None);

        public async Task<MusicReleaseDto?> GetMusicReleaseAsync(int id, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting music release by ID: {Id}", id);

            var musicRelease = await _musicReleaseRepository.GetByIdAsync(id, "Label,Country,Format,Packaging");

            if (musicRelease == null)
            {
                _logger.LogWarning("Music release not found: {Id}", id);
                return null;
            }

            // Check ownership
            var userId = _userContext.GetActingUserId();
            if (userId.HasValue && musicRelease.UserId != userId.Value)
            {
                _logger.LogWarning("Access denied for music release {Id}. User {UserId} does not own this release.", id, userId);
                return null;
            }

            await TryBackfillTracklistFromDiscogsAsync(musicRelease, cancellationToken);

            var dto = await _mapper.MapToFullDtoAsync(musicRelease);

            // Get the last played date
            dto.LastPlayedAt = await _context.NowPlayings
                .Where(np => np.MusicReleaseId == id)
                .OrderByDescending(np => np.PlayedAt)
                .Select(np => (DateTime?)np.PlayedAt)
                .FirstOrDefaultAsync(cancellationToken);

            return dto;
        }

        public async Task<MusicReleaseDto?> GetMusicReleaseAsync(int id)
            => await GetMusicReleaseAsync(id, CancellationToken.None);

        private async Task TryBackfillTracklistFromDiscogsAsync(MusicRelease musicRelease, CancellationToken cancellationToken)
        {
            if (_discogsService == null || !NeedsTracklistBackfill(musicRelease.Media) || !musicRelease.DiscogsId.HasValue || musicRelease.DiscogsId.Value <= 0)
            {
                return;
            }

            try
            {
                var details = await _discogsService.GetReleaseDetailsAsync(musicRelease.DiscogsId.Value.ToString());
                if (details?.Tracklist == null || details.Tracklist.Count == 0)
                {
                    return;
                }

                var artistIds = DeserializeIds(musicRelease.Artists)
                    .Select(id => id.ToString())
                    .ToList();
                var genreIds = DeserializeIds(musicRelease.Genres)
                    .Select(id => id.ToString())
                    .ToList();

                var tracks = new List<object>();
                var trackIndex = 1;
                foreach (var track in details.Tracklist)
                {
                    if (string.IsNullOrWhiteSpace(track.Title))
                    {
                        continue;
                    }

                    tracks.Add(new
                    {
                        Title = track.Title,
                        ReleaseYear = musicRelease.ReleaseYear?.ToString("yyyy-MM-dd") ?? string.Empty,
                        Artists = artistIds,
                        Genres = genreIds,
                        Live = false,
                        LengthSecs = ParseDuration(track.Duration),
                        Index = trackIndex++
                    });
                }

                if (tracks.Count == 0)
                {
                    return;
                }

                var media = new List<object>
                {
                    new
                    {
                        Title = musicRelease.Title,
                        FormatId = musicRelease.FormatId ?? 0,
                        Index = 1,
                        Tracks = tracks
                    }
                };

                musicRelease.Media = JsonSerializer.Serialize(media);
                musicRelease.LastModified = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Backfilled tracklist from Discogs for release {Id} (DiscogsId={DiscogsId})", musicRelease.Id, musicRelease.DiscogsId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Tracklist backfill failed for release {Id} (DiscogsId={DiscogsId})", musicRelease.Id, musicRelease.DiscogsId);
            }
        }

        private static bool NeedsTracklistBackfill(string? mediaJson)
        {
            if (string.IsNullOrWhiteSpace(mediaJson))
            {
                return true;
            }

            try
            {
                var media = JsonSerializer.Deserialize<List<MusicReleaseMediaDto>>(mediaJson, CaseInsensitiveJson);
                if (media == null || media.Count == 0)
                {
                    return true;
                }

                return media.All(m => m.Tracks == null || m.Tracks.Count == 0);
            }
            catch (JsonException)
            {
                return true;
            }
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
            catch (JsonException)
            {
                return new List<int>();
            }
        }

        private static int ParseDuration(string? duration)
        {
            if (string.IsNullOrWhiteSpace(duration))
            {
                return 0;
            }

            var parts = duration.Split(':');
            if (parts.Length == 2
                && int.TryParse(parts[0], out var minutes)
                && int.TryParse(parts[1], out var seconds))
            {
                return (minutes * 60) + seconds;
            }

            if (parts.Length == 3
                && int.TryParse(parts[0], out var hours)
                && int.TryParse(parts[1], out var hhMinutes)
                && int.TryParse(parts[2], out var hhSeconds))
            {
                return (hours * 3600) + (hhMinutes * 60) + hhSeconds;
            }

            return 0;
        }

        public async Task<List<SearchSuggestionDto>> GetSearchSuggestionsAsync(string query, int limit, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            {
                return new List<SearchSuggestionDto>();
            }

            _logger.LogInformation("Getting search suggestions for query: {Query}", query);

            var queryLower = query.ToLower();
            var suggestions = new List<SearchSuggestionDto>();
            var userId = _userContext.GetActingUserId();

            if (!userId.HasValue)
            {
                return new List<SearchSuggestionDto>();
            }

            // Get release title suggestions
            var releases = await _musicReleaseRepository.GetAsync(
                mr => mr.UserId == userId.Value && mr.Title.ToLower().Contains(queryLower),
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
                a => a.UserId == userId.Value && a.Name.ToLower().Contains(queryLower),
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
                l => l.UserId == userId.Value && l.Name.ToLower().Contains(queryLower),
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

        public async Task<List<SearchSuggestionDto>> GetSearchSuggestionsAsync(string query, int limit)
            => await GetSearchSuggestionsAsync(query, limit, CancellationToken.None);

        public async Task<CollectionStatisticsDto> GetCollectionStatisticsAsync(CancellationToken cancellationToken = default)
        {
            return await _statisticsService.GetCollectionStatisticsAsync();
        }

        public async Task<CollectionStatisticsDto> GetCollectionStatisticsAsync()
            => await GetCollectionStatisticsAsync(CancellationToken.None);

        public async Task<int?> GetRandomReleaseIdAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting random music release ID");

            var userId = _userContext.GetActingUserId();
            if (!userId.HasValue)
            {
                return null;
            }

            var totalCount = await _musicReleaseRepository.CountAsync(mr => mr.UserId == userId.Value);
            
            if (totalCount == 0)
            {
                _logger.LogWarning("No music releases in collection for random selection");
                return null;
            }

            var random = new Random();
            var skip = random.Next(0, totalCount);

            // Use GetPagedAsync with skip+1 as page number and page size of 1
            // to efficiently get just one random release without loading all into memory
            var pagedResult = await _musicReleaseRepository.GetPagedAsync(
                pageNumber: skip + 1,
                pageSize: 1,
                filter: mr => mr.UserId == userId.Value,
                orderBy: q => q.OrderBy(r => r.Id)
            );

            var randomRelease = pagedResult.Items.FirstOrDefault();
            
            return randomRelease?.Id;
        }

        public async Task<int?> GetRandomReleaseIdAsync()
            => await GetRandomReleaseIdAsync(CancellationToken.None);
    }
}
