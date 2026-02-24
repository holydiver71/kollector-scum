using Microsoft.AspNetCore.Mvc;
using KollectorScum.Api.Interfaces;

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
        private readonly string? _r2PublicBaseUrl;
        private readonly string _bucketName;
        private readonly ILogger<ImagesController> _logger;
        private readonly IStorageService _storageService;
        private readonly IUserContext _userContext;

        public ImagesController(IConfiguration configuration, ILogger<ImagesController> logger, IStorageService storageService, IUserContext userContext)
        {
            _imagesPath = configuration["ImagesPath"] ?? "/home/andy/music-images";
            // Support both configuration key formats: the env provider maps '__' to ':'
            // so try the colon form first and fall back to the literal double-underscore form.
            _r2PublicBaseUrl = configuration["R2:PublicBaseUrl"] ?? configuration["R2__PublicBaseUrl"];
            _bucketName = configuration["R2:BucketName"] ?? configuration["R2__BucketName"] ?? "cover-art";
            _logger = logger;
            _storageService = storageService;
            _userContext = userContext;
        }

        /// <summary>
        /// Serves an image file from the configured images directory
        /// </summary>
        /// <param name="imagePath">Relative path to the image (e.g., "covers/album1.jpg")</param>
        /// <returns>The image file or 404 if not found</returns>
        [HttpGet("{*imagePath}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetImage(string imagePath)
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

                // Check whether this is a bucket-prefixed path in the form {bucket}/{userId}/{filename}.
                // This is the format returned by NowPlayingController and other endpoints when R2 is used.
                var normalizedPath = imagePath.TrimStart('/');
                var bucketPrefixed =
                    normalizedPath.StartsWith($"{_bucketName}/", StringComparison.OrdinalIgnoreCase) ||
                    normalizedPath.StartsWith("cover-art/", StringComparison.OrdinalIgnoreCase);

                if (bucketPrefixed)
                {
                    if (!string.IsNullOrWhiteSpace(_r2PublicBaseUrl))
                    {
                        // Redirect: strip the bucket prefix so the CDN/worker path starts with {userId}/{filename}
                        var relative = normalizedPath;
                        if (relative.StartsWith($"{_bucketName}/", StringComparison.OrdinalIgnoreCase))
                            relative = relative.Substring(_bucketName.Length).TrimStart('/');
                        else if (relative.StartsWith("cover-art/", StringComparison.OrdinalIgnoreCase))
                            relative = relative.Substring("cover-art".Length).TrimStart('/');

                        var r2Url = _r2PublicBaseUrl.TrimEnd('/') + "/" + relative;
                        _logger.LogDebug("Redirecting image request to R2: {R2Url}", r2Url);
                        return Redirect(r2Url);
                    }

                    // No public URL configured – proxy the bytes directly from storage.
                    // Path arrives as {bucket}/{userId}/{filename}; parse accordingly.
                    var segments = normalizedPath.Split('/', 3);
                    if (segments.Length == 3)
                    {
                        var proxyBucket = segments[0];
                        var proxyUserId = segments[1];
                        var proxyFile = segments[2];

                        var stream = await _storageService.GetFileStreamAsync(proxyBucket, proxyUserId, proxyFile);
                        if (stream != null)
                        {
                            var ext = Path.GetExtension(proxyFile).ToLowerInvariant();
                            var ct = ext switch
                            {
                                ".jpg" or ".jpeg" => "image/jpeg",
                                ".png" => "image/png",
                                ".gif" => "image/gif",
                                ".webp" => "image/webp",
                                _ => "image/jpeg",
                            };
                            _logger.LogDebug("Proxying image from storage: {Path}", normalizedPath);
                            return File(stream, ct);
                        }
                        _logger.LogWarning("Storage proxy: file not found for path {Path}", normalizedPath);
                        return NotFound($"Image not found: {imagePath}");
                    }
                }

                // Build full file path - handle different image types (local fallback)
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
                R2PublicBaseUrl = _r2PublicBaseUrl,
                UsesR2 = !string.IsNullOrWhiteSpace(_r2PublicBaseUrl),
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

                var response = await httpClient.GetAsync(request.Url);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to download image from {Url}: {StatusCode}", request.Url, response.StatusCode);
                    return StatusCode(500, $"Failed to download image: {response.StatusCode}");
                }

                var imageBytes = await response.Content.ReadAsByteArrayAsync();
                var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";

                // Use provided filename if available (sanitized), otherwise generate GUID
                string uniqueFileName;
                if (!string.IsNullOrWhiteSpace(request.Filename)) 
                {
                    // Basic sanitization
                    var cleanName = Path.GetFileName(request.Filename);
                    uniqueFileName = cleanName; 
                }
                else 
                {
                    var fileExt = Path.GetExtension(originalFilename);
                    if (string.IsNullOrWhiteSpace(fileExt)) fileExt = ".jpg";
                    uniqueFileName = $"{Guid.NewGuid()}{fileExt}";
                }

                // Determine user id for multi-tenant storage; fall back to Guid.Empty if not authenticated
                var userId = _userContext.GetActingUserId() ?? Guid.Empty;

                _logger.LogInformation("Attempting to upload image. Bucket: {Bucket}, User: {User}, File: {File}", 
                    _bucketName, userId, uniqueFileName);

                // Upload to configured storage (R2 or local filesystem implementation)
                using var ms = new MemoryStream(imageBytes);
                var publicUrl = await _storageService.UploadFileAsync(_bucketName, userId.ToString(), uniqueFileName, ms, contentType);

                _logger.LogInformation("Downloaded and stored image: {Url} -> {StoredFile} (User: {UserId}, Size: {Size})", request.Url, uniqueFileName, userId, imageBytes.Length);

                return Ok(new {
                    Message = "Image downloaded and stored successfully",
                    Filename = uniqueFileName,
                    OriginalFilename = originalFilename,
                    Size = imageBytes.Length,
                    PublicUrl = publicUrl
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
