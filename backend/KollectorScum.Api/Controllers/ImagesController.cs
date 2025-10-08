using Microsoft.AspNetCore.Mvc;

namespace KollectorScum.Api.Controllers
{
    /// <summary>
    /// Controller for serving music-related images (album covers, artist photos, etc.)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ImagesController : ControllerBase
    {
        private readonly string _imagesPath;
        private readonly ILogger<ImagesController> _logger;

        public ImagesController(IConfiguration configuration, ILogger<ImagesController> logger)
        {
            _imagesPath = configuration["ImagesPath"] ?? "/home/andy/music-images";
            _logger = logger;
        }

        /// <summary>
        /// Serves an image file from the configured images directory
        /// </summary>
        /// <param name="imagePath">Relative path to the image (e.g., "covers/album1.jpg")</param>
        /// <returns>The image file or 404 if not found</returns>
        [HttpGet("{*imagePath}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetImage(string imagePath)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(imagePath))
                {
                    _logger.LogWarning("Empty image path requested");
                    return BadRequest("Image path cannot be empty");
                }

                // Security: Prevent directory traversal attacks
                if (imagePath.Contains("..") || Path.IsPathRooted(imagePath))
                {
                    _logger.LogWarning("Potentially malicious image path requested: {ImagePath}", imagePath);
                    return BadRequest("Invalid image path");
                }

                // Build full file path - handle different image types
                string fullPath;
                
                // If path already includes a directory (covers/, artists/, etc.), use as-is
                if (imagePath.Contains("/"))
                {
                    fullPath = Path.Combine(_imagesPath, imagePath);
                }
                else
                {
                    // For bare filenames, assume they're album covers and look in covers/ directory
                    fullPath = Path.Combine(_imagesPath, "covers", imagePath);
                }
                
                // Check if file exists
                if (!System.IO.File.Exists(fullPath))
                {
                    _logger.LogDebug("Image not found: {FullPath}", fullPath);
                    return NotFound($"Image not found: {imagePath}");
                }

                // Determine content type based on file extension
                var extension = Path.GetExtension(fullPath).ToLowerInvariant();
                var contentType = extension switch
                {
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    ".gif" => "image/gif",
                    ".webp" => "image/webp",
                    ".bmp" => "image/bmp",
                    ".tiff" or ".tif" => "image/tiff",
                    _ => "application/octet-stream"
                };

                // Read and serve the file
                var fileBytes = System.IO.File.ReadAllBytes(fullPath);
                _logger.LogDebug("Serving image: {ImagePath} ({Size} bytes)", imagePath, fileBytes.Length);
                
                return File(fileBytes, contentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error serving image: {ImagePath}", imagePath);
                return StatusCode(500, "Internal server error while serving image");
            }
        }

        /// <summary>
        /// Health check endpoint for the images service
        /// </summary>
        /// <returns>Status information about the images directory</returns>
        [HttpGet("health")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetHealth()
        {
            var exists = Directory.Exists(_imagesPath);
            var status = new
            {
                ImagesPath = _imagesPath,
                DirectoryExists = exists,
                Status = exists ? "OK" : "Images directory not found"
            };

            _logger.LogInformation("Images health check: {Status}", status.Status);
            return Ok(status);
        }
    }
}
