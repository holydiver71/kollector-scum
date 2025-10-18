using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KollectorScum.Api.Controllers
{
    /// <summary>
    /// Controller for Discogs API integration
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class DiscogsController : ControllerBase
    {
        private readonly IDiscogsService _discogsService;
        private readonly ILogger<DiscogsController> _logger;

        /// <summary>
        /// Constructor for DiscogsController
        /// </summary>
        public DiscogsController(
            IDiscogsService discogsService,
            ILogger<DiscogsController> logger)
        {
            _discogsService = discogsService;
            _logger = logger;
        }

        /// <summary>
        /// Search for releases by catalog number
        /// </summary>
        /// <param name="catalogNumber">The catalog number to search for</param>
        /// <param name="format">Optional format filter (e.g., "CD", "Vinyl")</param>
        /// <param name="country">Optional country filter (e.g., "UK", "US")</param>
        /// <param name="year">Optional year filter</param>
        /// <returns>List of matching releases</returns>
        /// <response code="200">Returns the list of matching releases</response>
        /// <response code="400">If the catalog number is invalid</response>
        /// <response code="500">If there was an error connecting to Discogs</response>
        [HttpGet("search")]
        [ProducesResponseType(typeof(List<DiscogsSearchResultDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<DiscogsSearchResultDto>>> SearchByCatalogNumber(
            [FromQuery] string catalogNumber,
            [FromQuery] string? format = null,
            [FromQuery] string? country = null,
            [FromQuery] int? year = null)
        {
            if (string.IsNullOrWhiteSpace(catalogNumber))
            {
                return BadRequest("Catalog number is required");
            }

            try
            {
                _logger.LogInformation("Searching Discogs: CatalogNumber={CatalogNumber}, Format={Format}, Country={Country}, Year={Year}",
                    catalogNumber, format, country, year);

                var results = await _discogsService.SearchByCatalogNumberAsync(
                    catalogNumber, format, country, year);

                _logger.LogInformation("Found {Count} results for catalog number: {CatalogNumber}",
                    results.Count, catalogNumber);

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching Discogs for catalog number: {CatalogNumber}", catalogNumber);
                return StatusCode(500, new { error = "Failed to search Discogs", message = ex.Message });
            }
        }

        /// <summary>
        /// Get detailed information about a specific release
        /// </summary>
        /// <param name="releaseId">The Discogs release ID</param>
        /// <returns>Full release details</returns>
        /// <response code="200">Returns the release details</response>
        /// <response code="404">If the release was not found</response>
        /// <response code="500">If there was an error connecting to Discogs</response>
        [HttpGet("release/{releaseId}")]
        [ProducesResponseType(typeof(DiscogsReleaseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<DiscogsReleaseDto>> GetReleaseDetails(string releaseId)
        {
            if (string.IsNullOrWhiteSpace(releaseId))
            {
                return BadRequest("Release ID is required");
            }

            try
            {
                _logger.LogInformation("Fetching Discogs release details for ID: {ReleaseId}", releaseId);

                var release = await _discogsService.GetReleaseDetailsAsync(releaseId);

                if (release == null)
                {
                    _logger.LogWarning("Release not found: {ReleaseId}", releaseId);
                    return NotFound(new { error = "Release not found", releaseId });
                }

                _logger.LogInformation("Successfully fetched release details for ID: {ReleaseId}", releaseId);

                return Ok(release);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Discogs release: {ReleaseId}", releaseId);
                return StatusCode(500, new { error = "Failed to fetch release from Discogs", message = ex.Message });
            }
        }
    }
}
