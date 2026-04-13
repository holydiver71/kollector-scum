using KollectorScum.Api.Data;
using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using KollectorScum.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace KollectorScum.Api.Application.Queries
{
    /// <summary>
    /// Handles <see cref="GetMusicReleasesQuery"/> — coordinates filter building, kollection resolution,
    /// paging, and DTO projection for the music releases list read path.
    /// </summary>
    public class GetMusicReleasesQueryHandler : IGetMusicReleasesQueryHandler
    {
        private readonly IMusicReleaseRepository _repository;
        private readonly IMusicReleaseMapperService _mapper;
        private readonly KollectorScumDbContext _context;
        private readonly ILogger<GetMusicReleasesQueryHandler> _logger;
        private readonly IUserContext _userContext;

        /// <summary>
        /// Initialises the handler with its required dependencies.
        /// </summary>
        public GetMusicReleasesQueryHandler(
            IMusicReleaseRepository repository,
            IMusicReleaseMapperService mapper,
            KollectorScumDbContext context,
            ILogger<GetMusicReleasesQueryHandler> logger,
            IUserContext userContext)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        }

        /// <inheritdoc />
        public async Task<PagedResult<MusicReleaseSummaryDto>> HandleAsync(GetMusicReleasesQuery query)
        {
            var parameters = query.Parameters;
            var cancellationToken = query.CancellationToken;

            _logger.LogInformation(
                "GetMusicReleasesQueryHandler: Page={Page} PageSize={PageSize}",
                parameters.Pagination.PageNumber,
                parameters.Pagination.PageSize);

            var userId = _userContext.GetActingUserId();
            if (!userId.HasValue)
            {
                _logger.LogWarning("GetMusicReleasesQueryHandler: No user context, returning empty result.");
                return EmptyPagedResult(parameters);
            }

            var (shouldReturnEmpty, kollectionGenreIds) =
                await ResolveKollectionFilterAsync(parameters, cancellationToken);

            if (shouldReturnEmpty)
                return EmptyPagedResult(parameters);

            var filter = MusicReleaseFilterBuilder.Build(parameters, userId.Value, kollectionGenreIds);
            if (filter == null)
            {
                _logger.LogError("GetMusicReleasesQueryHandler: Filter is null — returning empty result.");
                return EmptyPagedResult(parameters);
            }

            var sortBy = parameters.SortBy?.ToLower();
            if (sortBy == "artist")
                return await HandleArtistSortAsync(parameters, filter, cancellationToken);

            return await HandleStandardPagedAsync(parameters, filter, cancellationToken);
        }

        // ── Artist sort ───────────────────────────────────────────────────────

        /// <summary>
        /// Fetches all filtered releases, maps to DTOs, sorts by artist name in-memory,
        /// then applies pagination. Required because artist names are resolved during mapping.
        /// </summary>
        private async Task<PagedResult<MusicReleaseSummaryDto>> HandleArtistSortAsync(
            MusicReleaseQueryParameters parameters,
            Expression<Func<MusicRelease, bool>> filter,
            CancellationToken cancellationToken)
        {
            var allReleases = await _repository.GetAsync(filter, null, "Label,Country,Format");
            var allDtos = await _mapper.MapToSummaryDtosAsync(allReleases);

            var descending = parameters.SortOrder?.ToLower() == "desc";
            var sorted = descending
                ? allDtos.OrderByDescending(dto => dto.ArtistNames?.FirstOrDefault() ?? string.Empty).ToList()
                : allDtos.OrderBy(dto => dto.ArtistNames?.FirstOrDefault() ?? string.Empty).ToList();

            var totalCount = sorted.Count;
            var pagedItems = sorted
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

        // ── Standard paged path ───────────────────────────────────────────────

        /// <summary>
        /// Delegates paging and ordering to the repository, then maps results to DTOs.
        /// Used for all sort options other than artist.
        /// </summary>
        private async Task<PagedResult<MusicReleaseSummaryDto>> HandleStandardPagedAsync(
            MusicReleaseQueryParameters parameters,
            Expression<Func<MusicRelease, bool>> filter,
            CancellationToken cancellationToken)
        {
            var orderBy = BuildSortExpression(parameters);

            var pagedResult = await _repository.GetPagedAsync(
                parameters.Pagination.PageNumber,
                parameters.Pagination.PageSize,
                filter,
                orderBy,
                "Label,Country,Format");

            var summaryDtos = await _mapper.MapToSummaryDtosAsync(pagedResult.Items);

            return new PagedResult<MusicReleaseSummaryDto>
            {
                Items = summaryDtos,
                Page = pagedResult.Page,
                PageSize = pagedResult.PageSize,
                TotalCount = pagedResult.TotalCount,
                TotalPages = pagedResult.TotalPages
            };
        }

        // ── Kollection resolution ─────────────────────────────────────────────

        /// <summary>
        /// Resolves the kollection genre filter.
        /// Returns <c>(shouldReturnEmpty: true, null)</c> when the requested kollection does not exist.
        /// Returns <c>(false, null)</c> when no kollection filter is requested or the kollection has no genres.
        /// Returns <c>(false, genreIds)</c> when the kollection has genres to filter by.
        /// </summary>
        private async Task<(bool shouldReturnEmpty, IReadOnlyList<int>? genreIds)> ResolveKollectionFilterAsync(
            MusicReleaseQueryParameters parameters,
            CancellationToken cancellationToken)
        {
            if (!parameters.KollectionId.HasValue)
                return (false, null);

            var kollectionExists = await _context.Kollections
                .AnyAsync(k => k.Id == parameters.KollectionId.Value, cancellationToken);

            if (!kollectionExists)
            {
                _logger.LogWarning(
                    "GetMusicReleasesQueryHandler: Kollection {KollectionId} not found",
                    parameters.KollectionId.Value);
                return (true, null);
            }

            var genreIds = await _context.KollectionGenres
                .Where(kg => kg.KollectionId == parameters.KollectionId.Value)
                .Select(kg => kg.GenreId)
                .ToListAsync(cancellationToken);

            if (genreIds.Count == 0)
            {
                _logger.LogInformation(
                    "GetMusicReleasesQueryHandler: Kollection {KollectionId} has no genres, skipping genre filter",
                    parameters.KollectionId.Value);
                return (false, null);
            }

            return (false, genreIds);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>Builds an EF orderBy delegate from the sort parameters.</summary>
        private static Func<IQueryable<MusicRelease>, IOrderedQueryable<MusicRelease>> BuildSortExpression(
            MusicReleaseQueryParameters parameters)
        {
            var sortBy = parameters.SortBy?.ToLower();
            var descending = parameters.SortOrder?.ToLower() == "desc";

            return sortBy switch
            {
                "dateadded" => descending
                    ? q => q.OrderByDescending(x => x.DateAdded)
                    : q => q.OrderBy(x => x.DateAdded),
                "title" => descending
                    ? q => q.OrderByDescending(x => x.Title)
                    : q => q.OrderBy(x => x.Title),
                "origreleaseyear" => descending
                    ? q => q.OrderByDescending(x => x.OrigReleaseYear)
                    : q => q.OrderBy(x => x.OrigReleaseYear),
                _ => q => q.OrderBy(x => x.Title)
            };
        }

        /// <summary>Returns an empty paged result shaped to the requested pagination parameters.</summary>
        private static PagedResult<MusicReleaseSummaryDto> EmptyPagedResult(MusicReleaseQueryParameters parameters) =>
            new()
            {
                Items = new List<MusicReleaseSummaryDto>(),
                Page = parameters.Pagination.PageNumber,
                PageSize = parameters.Pagination.PageSize,
                TotalCount = 0,
                TotalPages = 0
            };
    }
}
