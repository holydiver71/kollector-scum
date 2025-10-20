using KollectorScum.Api.DTOs;

namespace KollectorScum.Api.Interfaces
{
    /// <summary>
    /// Service for calculating collection statistics
    /// </summary>
    public interface ICollectionStatisticsService
    {
        /// <summary>
        /// Gets comprehensive collection statistics
        /// </summary>
        Task<CollectionStatisticsDto> GetCollectionStatisticsAsync();
    }
}
