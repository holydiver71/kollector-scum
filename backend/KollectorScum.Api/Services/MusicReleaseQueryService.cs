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

            // Build filter expression from parameters
            Expression<Func<MusicRelease, bool>>? filter = BuildFilterExpression(parameters);

            if (filter == null)
            {
                _logger.LogError("GetMusicReleasesAsync: Filter is null! Returning empty result.");
                return new PagedResult<MusicReleaseSummaryDto>
                {
                    Items = new List<MusicReleaseSummaryDto>(),
                    Page = parameters.Pagination.PageNumber,
                    PageSize = parameters.Pagination.PageSize,
                    TotalCount = 0,
                    TotalPages = 0
                };
            }

            // For artist sorting, we need to get all filtered results first, then sort after mapping
            // because artist names are resolved from IDs during mapping
            var sortBy = parameters.SortBy?.ToLower();
            if (sortBy == "artist")
            {
                // Get all filtered results without pagination
                var allFilteredReleases = await _musicReleaseRepository.GetAsync(
                    filter,
                    null, // No ordering at DB level
                    "Label,Country,Format"
                );

                // Map to DTOs
                var allDtos = await Task.Run(() => allFilteredReleases.Select(mr => _mapper.MapToSummaryDto(mr)).ToList());

                // Sort by artist name
                var sortOrder = parameters.SortOrder?.ToLower();
                var sortedDtos = sortOrder == "desc"
                    ? allDtos.OrderByDescending(dto => dto.ArtistNames?.FirstOrDefault() ?? string.Empty).ToList()
                    : allDtos.OrderBy(dto => dto.ArtistNames?.FirstOrDefault() ?? string.Empty).ToList();

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

            // Build sort expression for other sort options
            Func<IQueryable<MusicRelease>, IOrderedQueryable<MusicRelease>> orderBy = BuildSortExpression(parameters);

            // Get paged results
            var pagedResult = await _musicReleaseRepository.GetPagedAsync(
                parameters.Pagination.PageNumber,
                parameters.Pagination.PageSize,
                filter,
                orderBy,
                "Label,Country,Format"
            );

            // Map to DTOs
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

        /// <summary>
        /// Builds a sort expression from query parameters
        /// </summary>
        private Func<IQueryable<MusicRelease>, IOrderedQueryable<MusicRelease>> BuildSortExpression(MusicReleaseQueryParameters parameters)
        {
            var sortBy = parameters.SortBy?.ToLower();
            var sortOrder = parameters.SortOrder?.ToLower();
            var isDescending = sortOrder == "desc";

            return sortBy switch
            {
                "dateadded" => isDescending
                    ? mr => mr.OrderByDescending(x => x.DateAdded)
                    : mr => mr.OrderBy(x => x.DateAdded),
                "title" => isDescending
                    ? mr => mr.OrderByDescending(x => x.Title)
                    : mr => mr.OrderBy(x => x.Title),
                "origreleaseyear" => isDescending
                    ? mr => mr.OrderByDescending(x => x.OrigReleaseYear)
                    : mr => mr.OrderBy(x => x.OrigReleaseYear),
                _ => mr => mr.OrderBy(x => x.Title) // Default to title ascending
            };
        }

        /// <summary>
        /// Builds a filter expression from query parameters
        /// </summary>
        private Expression<Func<MusicRelease, bool>>? BuildFilterExpression(MusicReleaseQueryParameters parameters)
        {
            // Get genre IDs from kollection if specified
            List<int>? kollectionGenreIds = null;
            if (parameters.KollectionId.HasValue)
            {
                // First check if the kollection exists
                var kollectionExists = _context.Kollections
                    .Any(k => k.Id == parameters.KollectionId.Value);
                
                if (!kollectionExists)
                {
                    _logger.LogWarning("BuildFilterExpression: Kollection {KollectionId} not found", parameters.KollectionId.Value);
                    return mr => false;
                }

                kollectionGenreIds = _context.KollectionGenres
                    .Where(kg => kg.KollectionId == parameters.KollectionId.Value)
                    .Select(kg => kg.GenreId)
                    .ToList();

                // If kollection has no genres, log it but continue - don't filter by genre
                if (kollectionGenreIds.Count == 0)
                {
                    _logger.LogInformation("BuildFilterExpression: Kollection {KollectionId} has no genres assigned, will not filter by genre", parameters.KollectionId.Value);
                    // Don't return false - just don't add genre filtering
                    kollectionGenreIds = null;
                }
            }

            // Build composite filter using expression tree so EF can translate constants
            // Note: Artists and Genres are stored as JSON arrays like "[1,2,3]"
            var param = Expression.Parameter(typeof(MusicRelease), "mr");
            var clauses = new List<Expression>();

            // Always filter by current user
            var userId = _userContext.GetActingUserId();
            _logger.LogInformation("BuildFilterExpression: ActingUserId {UserId}", userId);

            if (!userId.HasValue)
            {
                // Security: If no user context, return no results
                // We return a filter that always evaluates to false
                _logger.LogWarning("BuildFilterExpression: No user context, returning false filter.");
                return mr => false;
            }

            var userIdProp = Expression.Property(param, nameof(MusicRelease.UserId));
            clauses.Add(Expression.Equal(userIdProp, Expression.Constant(userId.Value)));

            var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) })!;
            var toLowerMethod = typeof(string).GetMethod("ToLower", Array.Empty<Type>());

            // Title search
            if (!string.IsNullOrEmpty(parameters.Search))
            {
                var titleProp = Expression.Property(param, nameof(MusicRelease.Title));
                Expression titleLower = titleProp;
                if (toLowerMethod != null)
                    titleLower = Expression.Call(titleProp, toLowerMethod!);

                var searchConst = Expression.Constant(parameters.Search.ToLower());
                var searchContains = Expression.Call(titleLower, containsMethod, searchConst);
                clauses.Add(searchContains);
            }

            // Helper for JSON-array contains checks
            Expression BuildJsonContainsExpression(string propName, int id)
            {
                var prop = Expression.Property(param, propName);
                var notNull = Expression.NotEqual(prop, Expression.Constant(null, typeof(string)));

                var c1 = Expression.Call(prop, containsMethod, Expression.Constant("[" + id + "]"));
                var c2 = Expression.Call(prop, containsMethod, Expression.Constant("[" + id + ","));
                var c3 = Expression.Call(prop, containsMethod, Expression.Constant("," + id + "]"));
                var c4 = Expression.Call(prop, containsMethod, Expression.Constant("," + id + ","));

                var anyForId = Expression.OrElse(Expression.OrElse(c1, c2), Expression.OrElse(c3, c4));
                return Expression.AndAlso(notNull, anyForId);
            }

            if (parameters.ArtistId.HasValue)
                clauses.Add(BuildJsonContainsExpression(nameof(MusicRelease.Artists), parameters.ArtistId.Value));

            if (parameters.GenreId.HasValue)
                clauses.Add(BuildJsonContainsExpression(nameof(MusicRelease.Genres), parameters.GenreId.Value));

            // Kollection genres ORed together
            if (kollectionGenreIds != null)
            {
                Expression? kollectionOr = null;
                foreach (var gid in kollectionGenreIds)
                {
                    var expr = BuildJsonContainsExpression(nameof(MusicRelease.Genres), gid);
                    kollectionOr = kollectionOr == null ? expr : Expression.OrElse(kollectionOr, expr);
                }

                if (kollectionOr != null)
                    clauses.Add(kollectionOr);
            }

            if (parameters.LabelId.HasValue)
            {
                var prop = Expression.Property(param, nameof(MusicRelease.LabelId));
                clauses.Add(Expression.Equal(prop, Expression.Constant(parameters.LabelId.Value, typeof(int?))));
            }

            if (parameters.CountryId.HasValue)
            {
                var prop = Expression.Property(param, nameof(MusicRelease.CountryId));
                clauses.Add(Expression.Equal(prop, Expression.Constant(parameters.CountryId.Value, typeof(int?))));
            }

            if (parameters.FormatId.HasValue)
            {
                var prop = Expression.Property(param, nameof(MusicRelease.FormatId));
                clauses.Add(Expression.Equal(prop, Expression.Constant(parameters.FormatId.Value, typeof(int?))));
            }

            if (parameters.Live.HasValue)
            {
                var prop = Expression.Property(param, nameof(MusicRelease.Live));
                clauses.Add(Expression.Equal(prop, Expression.Constant(parameters.Live.Value)));
            }

            if (parameters.YearFrom.HasValue)
            {
                var prop = Expression.Property(param, nameof(MusicRelease.ReleaseYear));
                var hasValue = Expression.Property(prop, "HasValue");
                var value = Expression.Property(prop, "Value");
                var compare = Expression.GreaterThanOrEqual(value, Expression.Constant(new DateTime(parameters.YearFrom.Value, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
                clauses.Add(Expression.AndAlso(hasValue, compare));
            }

            if (parameters.YearTo.HasValue)
            {
                var prop = Expression.Property(param, nameof(MusicRelease.ReleaseYear));
                var hasValue = Expression.Property(prop, "HasValue");
                var value = Expression.Property(prop, "Value");
                var compare = Expression.LessThanOrEqual(value, Expression.Constant(new DateTime(parameters.YearTo.Value, 12, 31, 23, 59, 59, 999, DateTimeKind.Utc)));
                clauses.Add(Expression.AndAlso(hasValue, compare));
            }

            if (!clauses.Any())
            {
                _logger.LogError("BuildFilterExpression: Clauses is empty! This should never happen.");
                return null;
            }

            Expression combined = clauses[0];
            for (int i = 1; i < clauses.Count; i++)
                combined = Expression.AndAlso(combined, clauses[i]);

            var lambda = Expression.Lambda<Func<MusicRelease, bool>>(combined, param);
            _logger.LogInformation("BuildFilterExpression: Filter: {Filter}", lambda.ToString());
            return lambda;
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
