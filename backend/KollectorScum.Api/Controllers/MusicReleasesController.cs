using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using KollectorScum.Api.Models.ValueObjects;
using KollectorScum.Api.DTOs;
using System.Linq.Expressions;
using System.Text.Json;

namespace KollectorScum.Api.Controllers
{
    /// <summary>
    /// API controller for managing music releases
    /// Uses separate services for queries and commands following CQRS principles
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class MusicReleasesController : ControllerBase
    {
        private readonly IMusicReleaseQueryService _queryService;
        private readonly IMusicReleaseCommandService _commandService;
        private readonly ILogger<MusicReleasesController> _logger;

        public MusicReleasesController(
            IMusicReleaseQueryService queryService,
            IMusicReleaseCommandService commandService,
            ILogger<MusicReleasesController> logger)
        {
            _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
            _commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets a paginated list of music releases
        /// </summary>
        /// <param name="search">Search term to filter by title or artist name</param>
        /// <param name="artistId">Filter by artist ID</param>
        /// <param name="genreId">Filter by genre ID</param>
        /// <param name="labelId">Filter by label ID</param>
        /// <param name="countryId">Filter by country ID</param>
        /// <param name="formatId">Filter by format ID</param>
        /// <param name="live">Filter by live recordings</param>
        /// <param name="yearFrom">Filter by minimum release year (inclusive)</param>
        /// <param name="yearTo">Filter by maximum release year (inclusive)</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 50)</param>
        /// <returns>Paginated list of music release summaries</returns>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<MusicReleaseSummaryDto>), 200)]
        public async Task<ActionResult<PagedResult<MusicReleaseSummaryDto>>> GetMusicReleases(
            [FromQuery] string? search,
            [FromQuery] int? artistId,
            [FromQuery] int? genreId,
            [FromQuery] int? labelId,
            [FromQuery] int? countryId,
            [FromQuery] int? formatId,
            [FromQuery] bool? live,
            [FromQuery] int? yearFrom,
            [FromQuery] int? yearTo,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var result = await _queryService.GetMusicReleasesAsync(
                    search, artistId, genreId, labelId, countryId, formatId, 
                    live, yearFrom, yearTo, page, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting music releases");
                return StatusCode(500, "An error occurred while retrieving music releases");
            }
        }

        /// <summary>
        /// Gets a specific music release by ID
        /// </summary>
        /// <param name="id">Music release ID</param>
        /// <returns>Music release details</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(MusicReleaseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<MusicReleaseDto>> GetMusicRelease(int id)
        {
            try
            {
                var dto = await _queryService.GetMusicReleaseAsync(id);
                
                if (dto == null)
                {
                    _logger.LogWarning("Music release not found: {Id}", id);
                    return NotFound($"Music release with ID {id} not found");
                }

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting music release by ID: {Id}", id);
                return StatusCode(500, "An error occurred while retrieving the music release");
            }
        }

        /// <summary>
        /// Gets search suggestions based on a partial search term
        /// </summary>
        /// <param name="query">Partial search term</param>
        /// <param name="limit">Maximum number of suggestions to return (default: 10)</param>
        /// <returns>List of search suggestions</returns>
        [HttpGet("suggestions")]
        [ProducesResponseType(typeof(List<SearchSuggestionDto>), 200)]
        public async Task<ActionResult<List<SearchSuggestionDto>>> GetSearchSuggestions(
            [FromQuery] string query,
            [FromQuery] int limit = 10)
        {
            try
            {
                var suggestions = await _queryService.GetSearchSuggestionsAsync(query, limit);
                return Ok(suggestions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting search suggestions for query: {Query}", query);
                return StatusCode(500, "An error occurred while retrieving search suggestions");
            }
        }

        /// <summary>
        /// Gets comprehensive collection statistics
        /// </summary>
        /// <returns>Collection statistics including counts, distributions, and value metrics</returns>
        [HttpGet("statistics")]
        [ProducesResponseType(typeof(CollectionStatisticsDto), 200)]
        public async Task<ActionResult<CollectionStatisticsDto>> GetCollectionStatistics()
        {
            try
            {
                var statistics = await _queryService.GetCollectionStatisticsAsync();
                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting collection statistics");
                return StatusCode(500, "An error occurred while retrieving collection statistics");
            }
        }

        /// <summary>
        /// Creates a new music release
        /// Supports auto-creation of new lookup entities (artists, labels, genres, etc.)
        /// </summary>
        /// <param name="createDto">Music release data</param>
        /// <returns>Created music release with details about auto-created entities</returns>
        [HttpPost]
        [ProducesResponseType(typeof(CreateMusicReleaseResponseDto), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<CreateMusicReleaseResponseDto>> CreateMusicRelease([FromBody] CreateMusicReleaseDto createDto)
        {
            try
            {
                var response = await _commandService.CreateMusicReleaseAsync(createDto);
                return CreatedAtAction(nameof(GetMusicRelease), new { id = response.Release.Id }, response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error creating music release: {Title}", createDto.Title);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating music release: {Title}", createDto.Title);
                return StatusCode(500, "An error occurred while creating the music release");
            }
        }

        /// <summary>
        /// Updates an existing music release
        /// </summary>
        /// <param name="id">Music release ID</param>
        /// <param name="updateDto">Updated music release data</param>
        /// <returns>Updated music release</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(MusicReleaseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<MusicReleaseDto>> UpdateMusicRelease(int id, [FromBody] UpdateMusicReleaseDto updateDto)
        {
            try
            {
                var updatedRelease = await _commandService.UpdateMusicReleaseAsync(id, updateDto);
                
                if (updatedRelease == null)
                {
                    return NotFound($"Music release with ID {id} not found");
                }
                
                return Ok(updatedRelease);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error updating music release: {Id}", id);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating music release: {Id}", id);
                return StatusCode(500, "An error occurred while updating the music release");
            }
        }

        /// <summary>
        /// Deletes a music release
        /// </summary>
        /// <param name="id">Music release ID</param>
        /// <returns>No content</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteMusicRelease(int id)
        {
            try
            {
                var deleted = await _commandService.DeleteMusicReleaseAsync(id);
                
                if (!deleted)
                {
                    return NotFound($"Music release with ID {id} not found");
                }
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting music release: {Id}", id);
                return StatusCode(500, "An error occurred while deleting the music release");
            }
        }
    }
}
