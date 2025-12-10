using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// Query builder for constructing complex MusicRelease queries
    /// Implements the Builder pattern for composable query construction
    /// </summary>
    public class MusicReleaseQueryBuilder : IQueryBuilder<MusicRelease>
    {
        private IQueryable<MusicRelease> _query;
        private readonly MusicReleaseQueryParameters _parameters;

        public MusicReleaseQueryBuilder(
            IQueryable<MusicRelease> baseQuery,
            MusicReleaseQueryParameters parameters)
        {
            _query = baseQuery ?? throw new ArgumentNullException(nameof(baseQuery));
            _parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
        }

        public IQueryBuilder<MusicRelease> ApplySearch(string? searchTerm)
        {
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var lowerSearch = searchTerm.ToLower();
                _query = _query.Where(mr => mr.Title.ToLower().Contains(lowerSearch));
            }
            return this;
        }

        public IQueryBuilder<MusicRelease> ApplyFilters(Action<IQueryable<MusicRelease>>? filterAction)
        {
            // Apply individual filters from parameters
            if (_parameters.ArtistId.HasValue)
            {
                var id = _parameters.ArtistId.Value.ToString();
                var exact = "[" + id + "]";
                var start = "[" + id + ",";
                var middle = "," + id + ",";
                var end = "," + id + "]";

                _query = _query.Where(mr => mr.Artists != null && (
                    mr.Artists.Contains(exact) ||
                    mr.Artists.Contains(start) ||
                    mr.Artists.Contains(middle) ||
                    mr.Artists.Contains(end)));
            }

            if (_parameters.GenreId.HasValue)
            {
                var id = _parameters.GenreId.Value.ToString();
                var exact = "[" + id + "]";
                var start = "[" + id + ",";
                var middle = "," + id + ",";
                var end = "," + id + "]";

                _query = _query.Where(mr => mr.Genres != null && (
                    mr.Genres.Contains(exact) ||
                    mr.Genres.Contains(start) ||
                    mr.Genres.Contains(middle) ||
                    mr.Genres.Contains(end)));
            }

            if (_parameters.LabelId.HasValue)
            {
                _query = _query.Where(mr => mr.LabelId == _parameters.LabelId.Value);
            }

            if (_parameters.CountryId.HasValue)
            {
                _query = _query.Where(mr => mr.CountryId == _parameters.CountryId.Value);
            }

            if (_parameters.FormatId.HasValue)
            {
                _query = _query.Where(mr => mr.FormatId == _parameters.FormatId.Value);
            }

            if (_parameters.Live.HasValue)
            {
                _query = _query.Where(mr => mr.Live == _parameters.Live.Value);
            }

            if (_parameters.YearFrom.HasValue)
            {
                var start = new DateTime(_parameters.YearFrom.Value, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                _query = _query.Where(mr => mr.ReleaseYear.HasValue && mr.ReleaseYear.Value >= start);
            }

            if (_parameters.YearTo.HasValue)
            {
                var end = new DateTime(_parameters.YearTo.Value, 12, 31, 23, 59, 59, 999, DateTimeKind.Utc);
                _query = _query.Where(mr => mr.ReleaseYear.HasValue && mr.ReleaseYear.Value <= end);
            }

            // Apply custom filter action if provided
            filterAction?.Invoke(_query);

            return this;
        }

        public IQueryBuilder<MusicRelease> ApplyPagination(int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            var skip = (pageNumber - 1) * pageSize;
            _query = _query.Skip(skip).Take(pageSize);

            return this;
        }

        public IQueryBuilder<MusicRelease> ApplySorting(Func<IQueryable<MusicRelease>, IOrderedQueryable<MusicRelease>>? sortExpression)
        {
            if (sortExpression != null)
            {
                _query = sortExpression(_query);
            }
            else
            {
                // Default sorting by Title
                _query = _query.OrderBy(mr => mr.Title);
            }

            return this;
        }

        public IQueryable<MusicRelease> Build()
        {
            return _query;
        }
    }
}
