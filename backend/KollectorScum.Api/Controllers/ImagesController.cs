using Microsoft.AspNetCore.Mvc;
using KollectorScum.Api.DTOs;
using System.Text.RegularExpressions;
using SystemFile = System.IO.File;

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
        private readonly HttpClient _httpClient;
        private static readonly Regex _fileNameSanitizer = new(@"[^\w\-_\.]", RegexOptions.Compiled);
        private static readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" };

        public ImagesController(IConfiguration configuration, ILogger<ImagesController> logger, HttpClient httpClient)
        {
            _imagesPath = configuration["ImagesPath"] ?? "/home/andy/music-images";
            _logger = logger;
            _httpClient = httpClient;
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
                if (!SystemFile.Exists(fullPath))
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
                var fileBytes = SystemFile.ReadAllBytes(fullPath);
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
        /// Downloads an image from a URL and saves it to the covers directory
        /// </summary>
        /// <param name="request">Image download request containing URL and metadata</param>
        /// <returns>Download result with file information</returns>
        [HttpPost("download")]
        [ProducesResponseType(typeof(ImageDownloadResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DownloadImage([FromBody] ImageDownloadRequestDto request)
        {
            try
            {
                // Validate request
                if (request == null)
                {
                    return BadRequest(new ImageDownloadResponseDto 
                    { 
                        Success = false, 
                        ErrorMessage = "Request body is required" 
                    });
                }

                if (string.IsNullOrWhiteSpace(request.ImageUrl))
                {
                    return BadRequest(new ImageDownloadResponseDto 
                    { 
                        Success = false, 
                        ErrorMessage = "ImageUrl is required" 
                    });
                }

                if (string.IsNullOrWhiteSpace(request.Artist) || string.IsNullOrWhiteSpace(request.Album))
                {
                    return BadRequest(new ImageDownloadResponseDto 
                    { 
                        Success = false, 
                        ErrorMessage = "Artist and Album are required" 
                    });
                }

                _logger.LogInformation("Starting image download: {Url} for {Artist} - {Album}", 
                    request.ImageUrl, request.Artist, request.Album);

                // Download the image
                using var response = await _httpClient.GetAsync(request.ImageUrl);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to download image from {Url}: {StatusCode}", 
                        request.ImageUrl, response.StatusCode);
                    return BadRequest(new ImageDownloadResponseDto 
                    { 
                        Success = false, 
                        ErrorMessage = $"Failed to download image: HTTP {response.StatusCode}" 
                    });
                }

                // Get image content
                var imageBytes = await response.Content.ReadAsByteArrayAsync();
                
                if (imageBytes.Length == 0)
                {
                    return BadRequest(new ImageDownloadResponseDto 
                    { 
                        Success = false, 
                        ErrorMessage = "Downloaded image is empty" 
                    });
                }

                // Determine file extension from content type or URL
                var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
                var extension = GetFileExtensionFromContentType(contentType) ?? GetFileExtensionFromUrl(request.ImageUrl) ?? ".jpg";

                // Validate file extension
                if (!_allowedExtensions.Contains(extension.ToLowerInvariant()))
                {
                    return BadRequest(new ImageDownloadResponseDto 
                    { 
                        Success = false, 
                        ErrorMessage = $"Unsupported image format: {extension}" 
                    });
                }

                // Generate filename
                var fileName = GenerateFileName(request.Artist, request.Album, request.Year, extension);

                // Ensure covers directory exists
                var coversPath = Path.Combine(_imagesPath, "covers");
                Directory.CreateDirectory(coversPath);

                // Full file path
                var filePath = Path.Combine(coversPath, fileName);

                // Handle filename conflicts
                var finalFileName = fileName;
                var counter = 1;
                while (SystemFile.Exists(Path.Combine(coversPath, finalFileName)))
                {
                    var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                    finalFileName = $"{nameWithoutExt}_{counter}{extension}";
                    counter++;
                }

                var finalFilePath = Path.Combine(coversPath, finalFileName);

                // Save the file
                await SystemFile.WriteAllBytesAsync(finalFilePath, imageBytes);

                _logger.LogInformation("Successfully saved image: {FilePath} ({Size} bytes)", 
                    finalFilePath, imageBytes.Length);

                // Generate access URL
                var accessUrl = $"/api/images/covers/{finalFileName}";

                return Ok(new ImageDownloadResponseDto
                {
                    Success = true,
                    ImagePath = $"covers/{finalFileName}",
                    FileName = finalFileName,
                    FileSize = imageBytes.Length,
                    AccessUrl = accessUrl
                });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error downloading image from {Url}", request?.ImageUrl);
                return StatusCode(500, new ImageDownloadResponseDto 
                { 
                    Success = false, 
                    ErrorMessage = "Network error while downloading image" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading image from {Url}", request?.ImageUrl);
                return StatusCode(500, new ImageDownloadResponseDto 
                { 
                    Success = false, 
                    ErrorMessage = "Internal server error while downloading image" 
                });
            }
        }

        private string GenerateFileName(string artist, string album, string? year, string extension)
        {
            // Sanitize inputs
            var sanitizedArtist = SanitizeFileName(artist);
            var sanitizedAlbum = SanitizeFileName(album);
            var sanitizedYear = !string.IsNullOrWhiteSpace(year) ? SanitizeFileName(year) : null;

            // Build filename
            var fileName = sanitizedYear != null 
                ? $"{sanitizedArtist}-{sanitizedAlbum}-{sanitizedYear}{extension}"
                : $"{sanitizedArtist}-{sanitizedAlbum}{extension}";

            // Ensure reasonable length
            if (fileName.Length > 200)
            {
                var maxNameLength = 200 - extension.Length - 10; // Leave room for extension and counter
                var truncatedName = fileName.Substring(0, maxNameLength);
                fileName = $"{truncatedName}{extension}";
            }

            return fileName;
        }

        private string SanitizeFileName(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "unknown";

            // Replace invalid characters and clean up
            var sanitized = _fileNameSanitizer.Replace(input.Trim(), "_");
            
            // Remove consecutive underscores
            sanitized = Regex.Replace(sanitized, "_+", "_");
            
            // Remove leading/trailing underscores
            sanitized = sanitized.Trim('_');

            return string.IsNullOrWhiteSpace(sanitized) ? "unknown" : sanitized;
        }

        private string? GetFileExtensionFromContentType(string contentType)
        {
            return contentType.ToLowerInvariant() switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/gif" => ".gif",
                "image/webp" => ".webp",
                "image/bmp" => ".bmp",
                _ => null
            };
        }

        private string? GetFileExtensionFromUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                var path = uri.LocalPath;
                var extension = Path.GetExtension(path);
                
                return _allowedExtensions.Contains(extension.ToLowerInvariant()) ? extension : null;
            }
            catch
            {
                return null;
            }
        }
    }
}
