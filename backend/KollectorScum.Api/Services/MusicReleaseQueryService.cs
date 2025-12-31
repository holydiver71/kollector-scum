using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using KollectorScum.Api.Data;
using Microsoft.EntityFrameworkCore;
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
        private readonly KollectorScumDbContext _context;
        private readonly ILogger<MusicReleaseQueryService> _logger;
        private readonly IUserContext _userContext;

        public MusicReleaseQueryService(
            IRepository<MusicRelease> musicReleaseRepository,
            IRepository<Artist> artistRepository,
            IRepository<Label> labelRepository,
            IMusicReleaseMapperService mapper,
            ICollectionStatisticsService statisticsService,
            KollectorScumDbContext context,
            ILogger<MusicReleaseQueryService> logger,
            IUserContext userContext)
        {
            _musicReleaseRepository = musicReleaseRepository ?? throw new ArgumentNullException(nameof(musicReleaseRepository));
            _artistRepository = artistRepository ?? throw new ArgumentNullException(nameof(artistRepository));
            _labelRepository = labelRepository ?? throw new ArgumentNullException(nameof(labelRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _statisticsService = statisticsService ?? throw new ArgumentNullException(nameof(statisticsService));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        }

        public async Task<PagedResult<MusicReleaseSummaryDto>> GetMusicReleasesAsync(
            MusicReleaseQueryParameters parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            _logger.LogInformation("Getting music releases - Page: {Page}, PageSize: {PageSize}", 
                parameters.Pagination.PageNumber, parameters.Pagination.PageSize);

            // Build filter expression from parameters (without JSON array filters)
            var filter = BuildBaseFilter(parameters);

            // Get all filtered results (will apply JSON filtering client-side)
            var allFilteredReleases = await _musicReleaseRepository.GetAsync(
                filter,
                null,
                "Label,Country,Format"
            );

            // Apply client-side JSON filtering
            var clientFiltered = ApplyJsonFilters(allFilteredReleases, parameters).ToList();

            // Map to DTOs
            var allDtos = await Task.Run(() => clientFiltered.Select(mr => _mapper.MapToSummaryDto(mr)).ToList());

            // Apply sorting
            var sortBy = parameters.SortBy?.ToLower();
            var sortOrder = parameters.SortOrder?.ToLower();
            var isDescending = sortOrder == "desc";

            var sortedDtos = sortBy switch
            {
                "artist" => isDescending
                    ? allDtos.OrderByDescending(dto => dto.ArtistNames?.FirstOrDefault() ?? string.Empty).ToList()
                    : allDtos.OrderBy(dto => dto.ArtistNames?.FirstOrDefault() ?? string.Empty).ToList(),
                "dateadded" => isDescending
                    ? allDtos.OrderByDescending(dto => dto.DateAdded).ToList()
                    : allDtos.OrderBy(dto => dto.DateAdded).ToList(),
                "title" => isDescending
                    ? allDtos.OrderByDescending(dto => dto.Title).ToList()
                    : allDtos.OrderBy(dto => dto.Title).ToList(),
                "origreleaseyear" => isDescending
                    ? allDtos.OrderByDescending(dto => dto.OrigReleaseYear).ToList()
                    : allDtos.OrderBy(dto => dto.OrigReleaseYear).ToList(),
                _ => allDtos.OrderBy(dto => dto.Title).ToList()
            };

            // Apply pagination
            var totalCount = sortedDtos.Count;
            var pagedItems = sortedDtos
                .Skip((parameters.Pagination.PageNumber - 1) * parameters.Pagination.PageSize)
                .Take(parameters.Pagination.PageSize)
                .ToList();

            return new PagedResult<MusicReleaseSummaryDto>
            {
                Items = pagedItems,
                Page = parameters.Pagination.PageNumber,
                PageSize = parameters.Pagination.PageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)parameters.Pagination.PageSize)
            };
        }

        /// <summary>
        /// Applies client-side filtering for JSON array fields (Artists, Genres)
        /// </summary>
        private IEnumerable<MusicRelease> ApplyJsonFilters(
            IEnumerable<MusicRelease> releases,
            MusicReleaseQueryParameters parameters)
        {
            var result = releases;

            // Filter by artist
            if (parameters.ArtistId.HasValue)
            {
                result = result.Where(mr => CheckJsonArrayContains(mr.Artists, parameters.ArtistId.Value));
            }

            // Filter by genre
            if (parameters.GenreId.HasValue)
            {
                result = result.Where(mr => CheckJsonArrayContains(mr.Genres, parameters.GenreId.Value));
            }

            // Filter by kollection genres (OR logic)
            if (parameters.KollectionId.HasValue)
            {
                var kollectionGenreIds = _context.KollectionGenres
                    .Where(kg => kg.KollectionId == parameters.KollectionId.Value)
                    .Select(kg => kg.GenreId)
                    .ToList();

                if (kollectionGenreIds.Any())
                {
                    result = result.Where(mr =>
                        kollectionGenreIds.Any(gid => CheckJsonArrayContains(mr.Genres, gid)));
                }
            }

            return result;
        }

        /// <summary>
        /// Checks if a JSON array string contains a specific ID
        /// </summary>
        private static bool CheckJsonArrayContains(string? jsonArray, int id)
        {
            if (string.IsNullOrEmpty(jsonArray))
                return false;

            return jsonArray.Contains($"[{id}]") ||
                   jsonArray.Contains($"[{id},") ||
                   jsonArray.Contains($",{id}]") ||
                   jsonArray.Contains($",{id},");
        }

        /// <summary>
        /// Builds a base filter expression from query parameters (excluding JSON array filters)
        /// </summary>
        private Expression<Func<MusicRelease, bool>> BuildBaseFilter(MusicReleaseQueryParameters parameters)
        {
            // Always filter by current user
            var userId = _userContext.GetActingUserId();
            _logger.LogInformation("BuildBaseFilter: ActingUserId {UserId}", userId);

            if (!userId.HasValue)
            {
                // Security: If no user context, return no results
                _logger.LogWarning("BuildBaseFilter: No user context, returning false filter.");
                return mr => false;
            }

            Expression<Func<MusicRelease, bool>> filter = mr => mr.UserId == userId.Value;

            // Title search
            if (!string.IsNullOrEmpty(parameters.Search))
            {
                var searchLower = parameters.Search.ToLower();
                filter = CombineWithAnd(filter, mr => mr.Title.ToLower().Contains(searchLower));
            }

            // Simple property filters that EF Core can translate
            if (parameters.LabelId.HasValue)
            {
                filter = CombineWithAnd(filter, mr => mr.LabelId == parameters.LabelId.Value);
            }

            if (parameters.CountryId.HasValue)
            {
                filter = CombineWithAnd(filter, mr => mr.CountryId == parameters.CountryId.Value);
            }

            if (parameters.FormatId.HasValue)
            {
                filter = CombineWithAnd(filter, mr => mr.FormatId == parameters.FormatId.Value);
            }

            if (parameters.Live.HasValue)
            {
                filter = CombineWithAnd(filter, mr => mr.Live == parameters.Live.Value);
            }

            if (parameters.YearFrom.HasValue)
            {
                var yearFrom = new DateTime(parameters.YearFrom.Value, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                filter = CombineWithAnd(filter, mr => mr.ReleaseYear.HasValue && mr.ReleaseYear.Value >= yearFrom);
            }

            if (parameters.YearTo.HasValue)
            {
                var yearTo = new DateTime(parameters.YearTo.Value, 12, 31, 23, 59, 59, 999, DateTimeKind.Utc);
                filter = CombineWithAnd(filter, mr => mr.ReleaseYear.HasValue && mr.ReleaseYear.Value <= yearTo);
            }

            _logger.LogInformation("BuildBaseFilter: Filter built successfully");
            return filter;
        }

        /// <summary>
        /// Helper to combine two filter expressions with AND
        /// </summary>
        private Expression<Func<MusicRelease, bool>> CombineWithAnd(
            Expression<Func<MusicRelease, bool>> first,
            Expression<Func<MusicRelease, bool>> second)
        {
            var parameter = Expression.Parameter(typeof(MusicRelease));
            var leftVisitor = new ReplaceExpressionVisitor(first.Parameters[0], parameter);
            var left = leftVisitor.Visit(first.Body);
            var rightVisitor = new ReplaceExpressionVisitor(second.Parameters[0], parameter);
            var right = rightVisitor.Visit(second.Body);
            return Expression.Lambda<Func<MusicRelease, bool>>(
                Expression.AndAlso(left!, right!), parameter);
        }

        /// <summary>
        /// Expression visitor to replace parameters in lambda expressions
        /// </summary>
        private class ReplaceExpressionVisitor : ExpressionVisitor
        {
            private readonly Expression _oldValue;
            private readonly Expression _newValue;

            public ReplaceExpressionVisitor(Expression oldValue, Expression newValue)
            {
                _oldValue = oldValue;
                _newValue = newValue;
            }

            public override Expression? Visit(Expression? node)
            {
                return node == _oldValue ? _newValue : base.Visit(node);
            }
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

            // Check ownership
            var userId = _userContext.GetActingUserId();
            if (userId.HasValue && musicRelease.UserId != userId.Value)
            {
                _logger.LogWarning("Access denied for music release {Id}. User {UserId} does not own this release.", id, userId);
                return null;
            }

            var dto = await _mapper.MapToFullDtoAsync(musicRelease);

            // Get the last played date
            dto.LastPlayedAt = await _context.NowPlayings
                .Where(np => np.MusicReleaseId == id)
                .OrderByDescending(np => np.PlayedAt)
                .Select(np => (DateTime?)np.PlayedAt)
                .FirstOrDefaultAsync();

            return dto;
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

        public async Task<CollectionStatisticsDto> GetCollectionStatisticsAsync()
        {
            return await _statisticsService.GetCollectionStatisticsAsync();
        }

        public async Task<int?> GetRandomReleaseIdAsync()
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
    }
}
