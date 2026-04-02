using KollectorScum.Api.Interfaces;
using KollectorScum.Api.DTOs;
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
        private readonly IDiscogsImportJobService _jobService;
        private readonly IUserContext _userContext;
        private readonly ILogger<ImportController> _logger;

        /// <summary>
        /// Constructor for ImportController
        /// </summary>
        public ImportController(
            IDiscogsImportJobService jobService,
            IUserContext userContext,
            ILogger<ImportController> logger)
        {
            _jobService = jobService ?? throw new ArgumentNullException(nameof(jobService));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Import collection from Discogs
        /// </summary>
        /// <param name="request">Import request with Discogs username</param>
        /// <returns>Accepted job metadata for the queued import</returns>
        /// <response code="202">Returns the queued job metadata</response>
        /// <response code="400">If the username is invalid</response>
        /// <response code="401">If the user is not authenticated</response>
        /// <response code="409">If another import is already queued or running</response>
        /// <response code="500">If there was an error during import</response>
        [HttpPost("discogs")]
        [ProducesResponseType(typeof(DiscogsImportJobStatusDto), StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<DiscogsImportJobStatusDto>> ImportFromDiscogs(
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

                var job = await _jobService.EnqueueImportAsync(request.Username, userId.Value, HttpContext.RequestAborted);
                return Accepted(job);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Discogs import rejected for user {Username}", request.Username);
                return Conflict(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing from Discogs for username: {Username}", request.Username);
                return StatusCode(500, new { error = "Failed to import from Discogs", message = ex.Message });
            }
        }

        /// <summary>
        /// Get status for the current user's Discogs import job.
        /// </summary>
        [HttpGet("discogs/status")]
        [ProducesResponseType(typeof(DiscogsImportJobStatusDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<DiscogsImportJobStatusDto?>> GetDiscogsImportStatus([FromQuery] Guid? jobId = null)
        {
            var userId = _userContext.GetUserId();
            if (!userId.HasValue) return Unauthorized(new { error = "User is not authenticated" });

            var status = jobId.HasValue
                ? await _jobService.GetJobStatusAsync(jobId.Value, userId.Value, HttpContext.RequestAborted)
                : await _jobService.GetLatestJobStatusAsync(userId.Value, HttpContext.RequestAborted);

            return Ok(status);
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
