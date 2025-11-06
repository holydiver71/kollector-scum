using KollectorScum.Api.DTOs;
using KollectorScum.Api.Models;

namespace KollectorScum.Api.Interfaces
{
    /// <summary>
    /// Service interface for detecting duplicate music releases
    /// </summary>
    public interface IMusicReleaseDuplicateDetector
    {
        /// <summary>
        /// Finds potential duplicate music releases based on catalog number and title/artist matching
        /// </summary>
        /// <param name="catalogNumber">The catalog number to check</param>
        /// <param name="title">The release title</param>
        /// <param name="artistNames">List of artist names</param>
        /// <returns>List of potential duplicates, empty if none found</returns>
        Task<List<MusicRelease>> FindDuplicatesAsync(
            string? catalogNumber, 
            string title, 
            List<string>? artistNames);
    }
}
