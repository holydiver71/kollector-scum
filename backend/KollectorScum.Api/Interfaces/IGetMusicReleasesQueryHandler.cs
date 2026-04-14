using KollectorScum.Api.Application.Queries;
using KollectorScum.Api.DTOs;

namespace KollectorScum.Api.Interfaces
{
    /// <summary>
    /// Application-layer handler for the <see cref="GetMusicReleasesQuery"/>.
    /// Owns the read path for paginated music release lists.
    /// </summary>
    public interface IGetMusicReleasesQueryHandler
    {
        /// <summary>
        /// Handles the query and returns a paginated list of music release summaries.
        /// </summary>
        /// <param name="query">The query containing filter, sort, and pagination parameters.</param>
        /// <returns>A paged result of music release summary DTOs.</returns>
        Task<PagedResult<MusicReleaseSummaryDto>> HandleAsync(GetMusicReleasesQuery query);
    }
}
