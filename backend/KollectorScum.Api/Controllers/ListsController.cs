using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;

namespace KollectorScum.Api.Controllers
{
    /// <summary>
    /// API controller for managing user-defined lists
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ListsController : ControllerBase
    {
        private readonly IListService _listService;
        private readonly ILogger<ListsController> _logger;

        public ListsController(IListService listService, ILogger<ListsController> logger)
        {
            _listService = listService ?? throw new ArgumentNullException(nameof(listService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets all lists
        /// </summary>
        /// <returns>List of list summaries</returns>
        [HttpGet]
        [ProducesResponseType(typeof(List<ListSummaryDto>), 200)]
        public async Task<ActionResult<List<ListSummaryDto>>> GetLists()
        {
            try
            {
                var result = await _listService.GetAllListsAsync();

                if (result.IsFailure)
                {
                    return StatusCode(500, result.ErrorMessage);
                }

                return Ok(result.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting lists");
                return StatusCode(500, "An error occurred while retrieving lists");
            }
        }

        /// <summary>
        /// Gets a specific list by ID
        /// </summary>
        /// <param name="id">List ID</param>
        /// <returns>List details</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ListDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ListDto>> GetList(int id)
        {
            try
            {
                var result = await _listService.GetListAsync(id);

                if (result.IsFailure)
                {
                    return result.ErrorType switch
                    {
                        ErrorType.NotFound => NotFound(result.ErrorMessage),
                        _ => StatusCode(500, result.ErrorMessage)
                    };
                }

                return Ok(result.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting list {ListId}", id);
                return StatusCode(500, "An error occurred while retrieving the list");
            }
        }

        /// <summary>
        /// Gets release IDs in a specific list
        /// </summary>
        /// <param name="id">List ID</param>
        /// <returns>List of release IDs</returns>
        [HttpGet("{id}/releases")]
        [ProducesResponseType(typeof(List<int>), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<List<int>>> GetListReleases(int id)
        {
            try
            {
                var result = await _listService.GetListReleasesAsync(id);

                if (result.IsFailure)
                {
                    return result.ErrorType switch
                    {
                        ErrorType.NotFound => NotFound(result.ErrorMessage),
                        _ => StatusCode(500, result.ErrorMessage)
                    };
                }

                return Ok(result.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting releases for list {ListId}", id);
                return StatusCode(500, "An error occurred while retrieving list releases");
            }
        }

        /// <summary>
        /// Creates a new list
        /// </summary>
        /// <param name="createDto">List creation data</param>
        /// <returns>Created list</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ListDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        public async Task<ActionResult<ListDto>> CreateList([FromBody] CreateListDto createDto)
        {
            try
            {
                var result = await _listService.CreateListAsync(createDto);

                if (result.IsFailure)
                {
                    return result.ErrorType switch
                    {
                        ErrorType.ValidationError => BadRequest(result.ErrorMessage),
                        ErrorType.DuplicateError => Conflict(result.ErrorMessage),
                        _ => StatusCode(500, result.ErrorMessage)
                    };
                }

                return CreatedAtAction(nameof(GetList), new { id = result.Value!.Id }, result.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating list");
                return StatusCode(500, "An error occurred while creating the list");
            }
        }

        /// <summary>
        /// Updates an existing list
        /// </summary>
        /// <param name="id">List ID</param>
        /// <param name="updateDto">List update data</param>
        /// <returns>Updated list</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ListDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<ActionResult<ListDto>> UpdateList(int id, [FromBody] UpdateListDto updateDto)
        {
            try
            {
                var result = await _listService.UpdateListAsync(id, updateDto);

                if (result.IsFailure)
                {
                    return result.ErrorType switch
                    {
                        ErrorType.NotFound => NotFound(result.ErrorMessage),
                        ErrorType.ValidationError => BadRequest(result.ErrorMessage),
                        ErrorType.DuplicateError => Conflict(result.ErrorMessage),
                        _ => StatusCode(500, result.ErrorMessage)
                    };
                }

                return Ok(result.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating list {ListId}", id);
                return StatusCode(500, "An error occurred while updating the list");
            }
        }

        /// <summary>
        /// Deletes a list
        /// </summary>
        /// <param name="id">List ID</param>
        /// <returns>No content</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteList(int id)
        {
            try
            {
                var result = await _listService.DeleteListAsync(id);

                if (result.IsFailure)
                {
                    return result.ErrorType switch
                    {
                        ErrorType.NotFound => NotFound(result.ErrorMessage),
                        _ => StatusCode(500, result.ErrorMessage)
                    };
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting list {ListId}", id);
                return StatusCode(500, "An error occurred while deleting the list");
            }
        }

        /// <summary>
        /// Adds a release to a list
        /// </summary>
        /// <param name="id">List ID</param>
        /// <param name="addDto">Release to add</param>
        /// <returns>No content</returns>
        [HttpPost("{id}/releases")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> AddReleaseToList(int id, [FromBody] AddReleaseToListDto addDto)
        {
            try
            {
                var result = await _listService.AddReleaseToListAsync(id, addDto.ReleaseId);

                if (result.IsFailure)
                {
                    return result.ErrorType switch
                    {
                        ErrorType.NotFound => NotFound(result.ErrorMessage),
                        ErrorType.DuplicateError => Conflict(result.ErrorMessage),
                        _ => StatusCode(500, result.ErrorMessage)
                    };
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding release to list {ListId}", id);
                return StatusCode(500, "An error occurred while adding the release to the list");
            }
        }

        /// <summary>
        /// Removes a release from a list
        /// </summary>
        /// <param name="id">List ID</param>
        /// <param name="releaseId">Release ID</param>
        /// <returns>No content</returns>
        [HttpDelete("{id}/releases/{releaseId}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> RemoveReleaseFromList(int id, int releaseId)
        {
            try
            {
                var result = await _listService.RemoveReleaseFromListAsync(id, releaseId);

                if (result.IsFailure)
                {
                    return result.ErrorType switch
                    {
                        ErrorType.NotFound => NotFound(result.ErrorMessage),
                        _ => StatusCode(500, result.ErrorMessage)
                    };
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing release from list {ListId}", id);
                return StatusCode(500, "An error occurred while removing the release from the list");
            }
        }

        /// <summary>
        /// Gets all lists that contain a specific release
        /// </summary>
        /// <param name="releaseId">Release ID</param>
        /// <returns>List of list summaries</returns>
        [HttpGet("by-release/{releaseId}")]
        [ProducesResponseType(typeof(List<ListSummaryDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<List<ListSummaryDto>>> GetListsForRelease(int releaseId)
        {
            try
            {
                var result = await _listService.GetListsForReleaseAsync(releaseId);

                if (result.IsFailure)
                {
                    return result.ErrorType switch
                    {
                        ErrorType.NotFound => NotFound(result.ErrorMessage),
                        _ => StatusCode(500, result.ErrorMessage)
                    };
                }

                return Ok(result.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting lists for release {ReleaseId}", releaseId);
                return StatusCode(500, "An error occurred while retrieving lists for the release");
            }
        }
    }
}
