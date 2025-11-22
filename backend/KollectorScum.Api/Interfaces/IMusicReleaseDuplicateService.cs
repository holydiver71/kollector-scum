using KollectorScum.Api.Models;

namespace KollectorScum.Api.Interfaces
{
    /// <summary>
    /// Interface for duplicate detection operations
    /// </summary>
    public interface IMusicReleaseDuplicateService
    {
        /// <summary>
        /// Check for potential duplicate releases
        /// </summary>
        Task<List<MusicRelease>> CheckForDuplicatesAsync(
            string title, 
            string? labelNumber, 
            List<int>? artistIds,
            int? excludeReleaseId = null);

        /// <summary>
        /// Check if a release would be a duplicate
        /// </summary>
        Task<bool> IsDuplicateAsync(
            string title, 
            string? labelNumber, 
            List<int>? artistIds,
            int? excludeReleaseId = null);
    }
}
