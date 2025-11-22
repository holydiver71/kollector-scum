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

        /// <summary>
        /// Downloads an image from a URL and saves it to the images directory
        /// </summary>
        /// <param name="request">Download request with URL and filename</param>
        /// <returns>Success or error response</returns>
        [HttpPost("download")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DownloadImage([FromBody] ImageDownloadRequest request)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(request.Url))
                {
                    return BadRequest("URL cannot be empty");
                }

                if (string.IsNullOrWhiteSpace(request.Filename))
                {
                    return BadRequest("Filename cannot be empty");
                }

                // Security: Prevent directory traversal attacks
                if (request.Filename.Contains("..") || Path.IsPathRooted(request.Filename))
                {
                    _logger.LogWarning("Potentially malicious filename requested: {Filename}", request.Filename);
                    return BadRequest("Invalid filename");
                }

                // Determine target folder based on folder parameter or filename prefix
                string targetFolder = "covers"; // default
                if (!string.IsNullOrWhiteSpace(request.Folder))
                {
                    // Validate folder parameter
                    if (request.Folder.Contains("..") || Path.IsPathRooted(request.Folder))
                    {
                        _logger.LogWarning("Potentially malicious folder requested: {Folder}", request.Folder);
                        return BadRequest("Invalid folder");
                    }
                    targetFolder = request.Folder;
                }
                else if (request.Filename.StartsWith("thumb-", StringComparison.OrdinalIgnoreCase))
                {
                    // Auto-detect thumbnails based on filename prefix
                    targetFolder = "thumbnails";
                }

                // Ensure target directory exists
                var targetPath = Path.Combine(_imagesPath, targetFolder);
                if (!Directory.Exists(targetPath))
                {
                    Directory.CreateDirectory(targetPath);
                    _logger.LogInformation("Created {Folder} directory: {Path}", targetFolder, targetPath);
                }

                // Build full file path and make it unique if needed
                var originalFilename = request.Filename;
                var fullPath = Path.Combine(targetPath, originalFilename);
                var finalFilename = originalFilename;

                // If file exists, add number suffix
                if (System.IO.File.Exists(fullPath))
                {
                    var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFilename);
                    var extension = Path.GetExtension(originalFilename);
                    var counter = 1;

                    do
                    {
                        finalFilename = $"{fileNameWithoutExtension} ({counter}){extension}";
                        fullPath = Path.Combine(targetPath, finalFilename);
                        counter++;
                    } while (System.IO.File.Exists(fullPath));

                    _logger.LogInformation("File already exists, using unique filename: {OriginalFilename} -> {FinalFilename}", 
                        originalFilename, finalFilename);
                }

                // Download the image
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "KollectorScum/1.0");
                
                var imageBytes = await httpClient.GetByteArrayAsync(request.Url);
                
                // Save to file
                await System.IO.File.WriteAllBytesAsync(fullPath, imageBytes);
                
                _logger.LogInformation("Downloaded image: {Url} -> {Filename} ({Size} bytes)", 
                    request.Url, finalFilename, imageBytes.Length);

                return Ok(new { 
                    Message = "Image downloaded successfully", 
                    Filename = finalFilename,
                    OriginalFilename = originalFilename,
                    Size = imageBytes.Length 
                });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to download image from URL: {Url}", request.Url);
                return StatusCode(500, $"Failed to download image: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading image: {Url}", request.Url);
                return StatusCode(500, "Internal server error while downloading image");
            }
        }
    }

    /// <summary>
    /// Request model for image download
    /// </summary>
    public class ImageDownloadRequest
    {
        public string Url { get; set; } = string.Empty;
        public string Filename { get; set; } = string.Empty;
        public string? Folder { get; set; } // Optional: "covers", "thumbnails", etc.
    }
}
