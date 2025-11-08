using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KollectorScum.Api.Controllers
{
    /// <summary>
    /// Controller for image search functionality
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ImageSearchController : ControllerBase
    {
        private readonly IImageSearchService _imageSearchService;
        private readonly ILogger<ImageSearchController> _logger;

        public ImageSearchController(IImageSearchService imageSearchService, ILogger<ImageSearchController> logger)
        {
            _imageSearchService = imageSearchService;
            _logger = logger;
        }

        /// <summary>
        /// Search for album cover images using artist and album information
        /// </summary>
        /// <param name="artist">Artist name</param>
        /// <param name="album">Album title</param>
        /// <param name="year">Optional release year for more accurate results</param>
        /// <returns>Collection of image search results</returns>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ImageSearchResultDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> SearchImages(
            [FromQuery] string artist, 
            [FromQuery] string album, 
            [FromQuery] string? year = null)
        {
            try
            {
                // Validate required parameters
                if (string.IsNullOrWhiteSpace(artist))
                {
                    _logger.LogWarning("Image search request missing artist parameter");
                    return BadRequest("Artist parameter is required");
                }

                if (string.IsNullOrWhiteSpace(album))
                {
                    _logger.LogWarning("Image search request missing album parameter");
                    return BadRequest("Album parameter is required");
                }

                // Log the search request
                _logger.LogInformation("Image search request: Artist='{Artist}', Album='{Album}', Year='{Year}'", 
                    artist, album, year);

                // Check if service is available
                var isServiceAvailable = await _imageSearchService.IsServiceAvailableAsync();
                if (!isServiceAvailable)
                {
                    _logger.LogWarning("Image search service is not available");
                    return StatusCode(503, new { 
                        error = "Image search service is currently unavailable", 
                        details = "Please check Google Custom Search API configuration" 
                    });
                }

                // Perform the search
                var searchResults = await _imageSearchService.SearchImagesAsync(artist, album, year);
                var resultsList = searchResults.ToList();

                _logger.LogInformation("Image search completed: Found {Count} results for '{Artist} - {Album}'", 
                    resultsList.Count, artist, album);

                return Ok(resultsList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during image search for Artist='{Artist}', Album='{Album}', Year='{Year}'", 
                    artist, album, year);
                
                return StatusCode(500, new { 
                    error = "An error occurred while searching for images", 
                    details = "Please try again or contact support if the problem persists" 
                });
            }
        }

        /// <summary>
        /// Get the status and capabilities of the image search service
        /// </summary>
        /// <returns>Service status information</returns>
        [HttpGet("status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetServiceStatus()
        {
            try
            {
                var isAvailable = await _imageSearchService.IsServiceAvailableAsync();
                
                var status = new
                {
                    ServiceName = "Google Custom Search API",
                    IsAvailable = isAvailable,
                    Status = isAvailable ? "OK" : "Unavailable",
                    Capabilities = new[]
                    {
                        "Album cover image search",
                        "Multiple image format support",
                        "Aspect ratio filtering",
                        "Safe search enabled"
                    },
                    LastChecked = DateTime.UtcNow
                };

                _logger.LogInformation("Image search service status check: {Status}", status.Status);
                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking image search service status");
                return StatusCode(500, new { 
                    error = "Unable to check service status", 
                    details = ex.Message 
                });
            }
        }

        /// <summary>
        /// Health check endpoint for monitoring
        /// </summary>
        /// <returns>Health status</returns>
        [HttpGet("health")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> HealthCheck()
        {
            try
            {
                var isHealthy = await _imageSearchService.IsServiceAvailableAsync();
                
                if (isHealthy)
                {
                    return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
                }
                else
                {
                    return StatusCode(503, new { status = "unhealthy", timestamp = DateTime.UtcNow });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
                return StatusCode(503, new { 
                    status = "unhealthy", 
                    error = ex.Message, 
                    timestamp = DateTime.UtcNow 
                });
            }
        }
    }
}