using KollectorScum.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KollectorScum.Api.Controllers
{
    /// <summary>
    /// Controller for import operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ImportController : ControllerBase
    {
        private readonly IDiscogsCollectionImportService _importService;
        private readonly IUserContext _userContext;
        private readonly ILogger<ImportController> _logger;

        /// <summary>
        /// Constructor for ImportController
        /// </summary>
        public ImportController(
            IDiscogsCollectionImportService importService,
            IUserContext userContext,
            ILogger<ImportController> logger)
        {
            _importService = importService ?? throw new ArgumentNullException(nameof(importService));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Import collection from Discogs
        /// </summary>
        /// <param name="request">Import request with Discogs username</param>
        /// <returns>Import result with statistics</returns>
        /// <response code="200">Returns the import result</response>
        /// <response code="400">If the username is invalid</response>
        /// <response code="401">If the user is not authenticated</response>
        /// <response code="500">If there was an error during import</response>
        [HttpPost("discogs")]
        [ProducesResponseType(typeof(DiscogsImportResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<DiscogsImportResult>> ImportFromDiscogs(
            [FromBody] DiscogsImportRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username))
            {
                return BadRequest(new { error = "Discogs username is required" });
            }

            try
            {
                var userId = _userContext.GetUserId();
                if (!userId.HasValue)
                {
                    return Unauthorized(new { error = "User is not authenticated" });
                }

                _logger.LogInformation("Starting Discogs import for user {UserId} from Discogs user {DiscogsUsername}", 
                    userId.Value, request.Username);

                var result = await _importService.ImportCollectionAsync(request.Username, userId.Value);

                if (!result.Success)
                {
                    _logger.LogWarning("Discogs import failed for user {UserId}: {Errors}", 
                        userId.Value, string.Join("; ", result.Errors));
                    return StatusCode(500, result);
                }

                _logger.LogInformation("Discogs import completed for user {UserId}: {Imported} imported, {Skipped} skipped, {Failed} failed",
                    userId.Value, result.ImportedReleases, result.SkippedReleases, result.FailedReleases);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing from Discogs for username: {Username}", request.Username);
                return StatusCode(500, new { error = "Failed to import from Discogs", message = ex.Message });
            }
        }
    }

    /// <summary>
    /// Request to import collection from Discogs
    /// </summary>
    public class DiscogsImportRequest
    {
        /// <summary>
        /// Discogs username
        /// </summary>
        public string Username { get; set; } = string.Empty;
    }
}
