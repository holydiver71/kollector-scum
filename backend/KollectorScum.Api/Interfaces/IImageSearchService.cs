using KollectorScum.Api.DTOs;

namespace KollectorScum.Api.Interfaces
{
    /// <summary>
    /// Interface for image search services
    /// </summary>
    public interface IImageSearchService
    {
        /// <summary>
        /// Search for images related to an artist and album
        /// </summary>
        /// <param name="artist">Artist name</param>
        /// <param name="album">Album name</param>
        /// <param name="year">Optional release year for better search results</param>
        /// <returns>Collection of image search results</returns>
        Task<IEnumerable<ImageSearchResultDto>> SearchImagesAsync(string artist, string album, string? year = null);

        /// <summary>
        /// Check if the image search service is available and properly configured
        /// </summary>
        /// <returns>True if service is available</returns>
        Task<bool> IsServiceAvailableAsync();
    }
}