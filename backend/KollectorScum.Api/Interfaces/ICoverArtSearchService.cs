using KollectorScum.Api.DTOs;

namespace KollectorScum.Api.Interfaces
{
    /// <summary>
    /// Searches for album cover art using the MusicBrainz, Cover Art Archive, and Discogs APIs.
    /// No paid external services are required; all lookups are performed against
    /// free, open-data sources.
    /// </summary>
    public interface ICoverArtSearchService
    {
        /// <summary>
        /// Searches MusicBrainz and optionally Discogs for releases matching the provided criteria
        /// and resolves cover art URLs from the Cover Art Archive or Discogs for each match.
        /// </summary>
        /// <param name="query">
        /// Free-text search string, typically composed of artist and/or album title.
        /// </param>
        /// <param name="catalogueNumber">
        /// Optional catalogue number to refine the search (searches via Discogs when provided).
        /// </param>
        /// <param name="limit">Maximum number of results to return (1–20, default 8).</param>
        /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
        /// <returns>
        /// An ordered list of <see cref="CoverArtSearchResultDto"/> with the highest-confidence
        /// matches first. May be empty if no matching releases with cover art are found.
        /// </returns>
        Task<IReadOnlyList<CoverArtSearchResultDto>> SearchAsync(
            string query,
            string? catalogueNumber = null,
            int limit = 8,
            CancellationToken cancellationToken = default);
    }
}
