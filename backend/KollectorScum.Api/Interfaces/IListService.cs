using KollectorScum.Api.DTOs;
using KollectorScum.Api.Models;

namespace KollectorScum.Api.Interfaces
{
    /// <summary>
    /// Service interface for managing lists
    /// </summary>
    public interface IListService
    {
        /// <summary>
        /// Gets all lists
        /// </summary>
        /// <returns>List of list summaries</returns>
        Task<Result<List<ListSummaryDto>>> GetAllListsAsync();

        /// <summary>
        /// Gets a specific list by ID
        /// </summary>
        /// <param name="id">List ID</param>
        /// <returns>List details</returns>
        Task<Result<ListDto>> GetListAsync(int id);

        /// <summary>
        /// Gets releases in a specific list
        /// </summary>
        /// <param name="id">List ID</param>
        /// <returns>List of release IDs</returns>
        Task<Result<List<int>>> GetListReleasesAsync(int id);

        /// <summary>
        /// Creates a new list
        /// </summary>
        /// <param name="createDto">List creation data</param>
        /// <returns>Created list</returns>
        Task<Result<ListDto>> CreateListAsync(CreateListDto createDto);

        /// <summary>
        /// Updates an existing list
        /// </summary>
        /// <param name="id">List ID</param>
        /// <param name="updateDto">List update data</param>
        /// <returns>Updated list</returns>
        Task<Result<ListDto>> UpdateListAsync(int id, UpdateListDto updateDto);

        /// <summary>
        /// Deletes a list
        /// </summary>
        /// <param name="id">List ID</param>
        /// <returns>Result indicating success or failure</returns>
        Task<Result<bool>> DeleteListAsync(int id);

        /// <summary>
        /// Adds a release to a list
        /// </summary>
        /// <param name="listId">List ID</param>
        /// <param name="releaseId">Release ID</param>
        /// <returns>Result indicating success or failure</returns>
        Task<Result<bool>> AddReleaseToListAsync(int listId, int releaseId);

        /// <summary>
        /// Removes a release from a list
        /// </summary>
        /// <param name="listId">List ID</param>
        /// <param name="releaseId">Release ID</param>
        /// <returns>Result indicating success or failure</returns>
        Task<Result<bool>> RemoveReleaseFromListAsync(int listId, int releaseId);

        /// <summary>
        /// Gets all lists that contain a specific release
        /// </summary>
        /// <param name="releaseId">Release ID</param>
        /// <returns>List of list summaries</returns>
        Task<Result<List<ListSummaryDto>>> GetListsForReleaseAsync(int releaseId);
    }
}
