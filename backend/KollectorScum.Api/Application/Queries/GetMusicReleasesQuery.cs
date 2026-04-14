using KollectorScum.Api.DTOs;

namespace KollectorScum.Api.Application.Queries
{
    /// <summary>
    /// Query object encapsulating all parameters for retrieving a paginated music release list.
    /// Part of the Application layer — represents caller intent, not implementation detail.
    /// </summary>
    /// <param name="Parameters">Pagination, filtering, and sort options.</param>
    /// <param name="CancellationToken">Optional cancellation token propagated to async DB calls.</param>
    public sealed record GetMusicReleasesQuery(
        MusicReleaseQueryParameters Parameters,
        CancellationToken CancellationToken = default);
}
