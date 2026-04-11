using KollectorScum.Api.Data;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace KollectorScum.Api.Repositories
{
    /// <summary>
    /// Domain-specific repository for MusicRelease entities.
    /// Encapsulates multi-tenant query logic and provides type-safe domain queries.
    /// </summary>
    public class MusicReleaseRepository : Repository<MusicRelease>, IMusicReleaseRepository
    {
        private readonly IUserContext _userContext;

        /// <summary>
        /// Initializes a new instance of the MusicReleaseRepository class.
        /// </summary>
        /// <param name="context">Database context</param>
        /// <param name="userContext">User context for multi-tenant filtering</param>
        public MusicReleaseRepository(KollectorScumDbContext context, IUserContext userContext)
            : base(context)
        {
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        }

        /// <summary>
        /// Gets a paginated list of music releases for the current user with optional filters.
        /// </summary>
        /// <param name="page">Page number (1-based)</param>
        /// <param name="pageSize">Number of results per page</param>
        /// <param name="search">Optional title search term</param>
        /// <param name="artistId">Optional artist ID filter</param>
        /// <param name="genreId">Optional genre ID filter</param>
        /// <param name="formatId">Optional format ID filter</param>
        /// <param name="countryId">Optional country ID filter</param>
        /// <param name="labelId">Optional label ID filter</param>
        /// <returns>Filtered and paginated music releases</returns>
        public async Task<IEnumerable<MusicRelease>> GetPaginatedAsync(
            int page, int pageSize,
            string? search = null,
            int? artistId = null, int? genreId = null, int? formatId = null,
            int? countryId = null, int? labelId = null)
        {
            var filter = BuildFilterExpression(search, artistId, genreId, formatId, countryId, labelId);
            if (filter == null)
                return Enumerable.Empty<MusicRelease>();

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            return await _dbSet
                .AsNoTracking()
                .Where(filter)
                .OrderBy(mr => mr.Title)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        /// <summary>
        /// Gets the total count of music releases for the current user matching optional filters.
        /// </summary>
        /// <param name="search">Optional title search term</param>
        /// <param name="artistId">Optional artist ID filter</param>
        /// <param name="genreId">Optional genre ID filter</param>
        /// <param name="formatId">Optional format ID filter</param>
        /// <param name="countryId">Optional country ID filter</param>
        /// <param name="labelId">Optional label ID filter</param>
        /// <returns>Total count of matching releases</returns>
        public async Task<int> GetTotalCountAsync(
            string? search = null,
            int? artistId = null, int? genreId = null,
            int? formatId = null, int? countryId = null, int? labelId = null)
        {
            var filter = BuildFilterExpression(search, artistId, genreId, formatId, countryId, labelId);
            if (filter == null)
                return 0;

            return await _dbSet.AsNoTracking().CountAsync(filter);
        }

        /// <summary>
        /// Gets a single music release with all navigation properties loaded, scoped to the current user.
        /// </summary>
        /// <param name="id">Release ID</param>
        /// <returns>The release with Label, Country, Format, and Packaging loaded, or null if not found or not owned</returns>
        public async Task<MusicRelease?> GetWithDetailsAsync(int id)
        {
            var userId = _userContext.GetActingUserId();
            if (!userId.HasValue)
                return null;

            return await _dbSet
                .AsNoTracking()
                .Include(mr => mr.Label)
                .Include(mr => mr.Country)
                .Include(mr => mr.Format)
                .Include(mr => mr.Packaging)
                .FirstOrDefaultAsync(mr => mr.Id == id && mr.UserId == userId.Value);
        }

        /// <summary>
        /// Searches for music releases by title for the current user.
        /// </summary>
        /// <param name="searchTerm">Title search term</param>
        /// <returns>Releases whose titles contain the search term (case-insensitive)</returns>
        public async Task<IEnumerable<MusicRelease>> SearchAsync(string searchTerm)
        {
            var userId = _userContext.GetActingUserId();
            if (!userId.HasValue)
                return Enumerable.Empty<MusicRelease>();

            var term = searchTerm?.ToLower() ?? string.Empty;

            return await _dbSet
                .AsNoTracking()
                .Where(mr => mr.UserId == userId.Value && mr.Title.ToLower().Contains(term))
                .OrderBy(mr => mr.Title)
                .ToListAsync();
        }

        /// <summary>
        /// Builds a LINQ expression tree that filters releases by the current user and the
        /// supplied optional parameters. Artists and Genres are stored as JSON-serialised integer
        /// arrays (e.g. "[1,2,3]"), so four string-contains patterns are OR-ed for each ID filter
        /// to correctly match single-element, first, last, and middle positions.
        /// </summary>
        /// <returns>A filter expression, or null when no user context is available (security guard).</returns>
        private Expression<Func<MusicRelease, bool>>? BuildFilterExpression(
            string? search,
            int? artistId, int? genreId, int? formatId,
            int? countryId, int? labelId)
        {
            var userId = _userContext.GetActingUserId();
            if (!userId.HasValue)
                return null;

            var param = Expression.Parameter(typeof(MusicRelease), "mr");
            var clauses = new List<Expression>();

            // Always scope to the current user
            var userIdProp = Expression.Property(param, nameof(MusicRelease.UserId));
            clauses.Add(Expression.Equal(userIdProp, Expression.Constant(userId.Value)));

            var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) })!;
            var toLowerMethod = typeof(string).GetMethod("ToLower", Array.Empty<Type>())!;

            // Title search (case-insensitive)
            if (!string.IsNullOrEmpty(search))
            {
                var titleProp = Expression.Property(param, nameof(MusicRelease.Title));
                var titleLower = Expression.Call(titleProp, toLowerMethod);
                var searchConst = Expression.Constant(search.ToLower());
                clauses.Add(Expression.Call(titleLower, containsMethod, searchConst));
            }

            // JSON array contains helper:
            // Matches "[id]", "[id,", ",id]", ",id," to correctly handle any list position.
            Expression BuildJsonContains(string propName, int id)
            {
                var prop = Expression.Property(param, propName);
                var notNull = Expression.NotEqual(prop, Expression.Constant(null, typeof(string)));
                var c1 = Expression.Call(prop, containsMethod, Expression.Constant("[" + id + "]"));
                var c2 = Expression.Call(prop, containsMethod, Expression.Constant("[" + id + ","));
                var c3 = Expression.Call(prop, containsMethod, Expression.Constant("," + id + "]"));
                var c4 = Expression.Call(prop, containsMethod, Expression.Constant("," + id + ","));
                var anyMatch = Expression.OrElse(Expression.OrElse(c1, c2), Expression.OrElse(c3, c4));
                return Expression.AndAlso(notNull, anyMatch);
            }

            if (artistId.HasValue)
                clauses.Add(BuildJsonContains(nameof(MusicRelease.Artists), artistId.Value));

            if (genreId.HasValue)
                clauses.Add(BuildJsonContains(nameof(MusicRelease.Genres), genreId.Value));

            if (labelId.HasValue)
            {
                var prop = Expression.Property(param, nameof(MusicRelease.LabelId));
                clauses.Add(Expression.Equal(prop, Expression.Constant(labelId.Value, typeof(int?))));
            }

            if (countryId.HasValue)
            {
                var prop = Expression.Property(param, nameof(MusicRelease.CountryId));
                clauses.Add(Expression.Equal(prop, Expression.Constant(countryId.Value, typeof(int?))));
            }

            if (formatId.HasValue)
            {
                var prop = Expression.Property(param, nameof(MusicRelease.FormatId));
                clauses.Add(Expression.Equal(prop, Expression.Constant(formatId.Value, typeof(int?))));
            }

            // Combine all clauses with AND
            var combined = clauses.Aggregate(Expression.AndAlso);
            return Expression.Lambda<Func<MusicRelease, bool>>(combined, param);
        }
    }
}
