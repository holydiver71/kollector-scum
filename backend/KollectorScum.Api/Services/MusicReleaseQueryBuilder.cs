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
                _query = _query.Where(mr => mr.Artists != null && 
                    mr.Artists.Contains(_parameters.ArtistId.Value.ToString()));
            }

            if (_parameters.GenreId.HasValue)
            {
                _query = _query.Where(mr => mr.Genres != null && 
                    mr.Genres.Contains(_parameters.GenreId.Value.ToString()));
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
                _query = _query.Where(mr => mr.ReleaseYear.HasValue && 
                    mr.ReleaseYear.Value.Year >= _parameters.YearFrom.Value);
            }

            if (_parameters.YearTo.HasValue)
            {
                _query = _query.Where(mr => mr.ReleaseYear.HasValue && 
                    mr.ReleaseYear.Value.Year <= _parameters.YearTo.Value);
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
