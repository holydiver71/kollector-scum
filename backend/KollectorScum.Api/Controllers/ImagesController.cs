using Microsoft.AspNetCore.Mvc;
using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;

namespace KollectorScum.Api.Controllers
{
    /// <summary>
    /// Controller for serving, uploading, and searching music-related images
    /// (album covers, artist photos, etc.)
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
        private readonly IImageResizerService _imageResizer;
        private readonly ICoverArtSearchService _coverArtSearch;

        /// <summary>Maximum allowed upload size: 5 MB.</summary>
        private const long MaxUploadBytes = 5 * 1024 * 1024;

        private static readonly HashSet<string> AllowedImageMimeTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp", "image/bmp", "image/tiff",
        };

        /// <summary>
        /// Initialises a new instance of <see cref="ImagesController"/>.
        /// </summary>
        public ImagesController(
            IConfiguration configuration,
            ILogger<ImagesController> logger,
            IStorageService storageService,
            IUserContext userContext,
            IImageResizerService imageResizer,
            ICoverArtSearchService coverArtSearch)
        {
            _imagesPath = configuration["ImagesPath"] ?? "/home/andy/music-images";
            // Support both configuration key formats: the env provider maps '__' to ':'
            // so try the colon form first and fall back to the literal double-underscore form.
            _r2PublicBaseUrl = configuration["R2:PublicBaseUrl"] ?? configuration["R2__PublicBaseUrl"];
            _bucketName = configuration["R2:BucketName"] ?? configuration["R2__BucketName"] ?? "cover-art";
            _logger = logger;
            _storageService = storageService;
            _userContext = userContext;
            _imageResizer = imageResizer;
            _coverArtSearch = coverArtSearch;
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
                            Response.Headers["Cache-Control"] = "public, max-age=86400, stale-while-revalidate=3600";
                            return File(stream, ct);
                        }
                        _logger.LogDebug("Storage proxy: file not found for path {Path}, falling back to local images directory", normalizedPath);

                        // Fall through to local filesystem lookup using just the filename
                        imagePath = proxyFile;
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

                // Allow the browser to cache images for 24 hours and serve stale copies
                // for up to 1 hour while revalidating in the background.  This dramatically
                // reduces repeated image requests between page navigations and helps avoid
                // hitting the API's rate limiter when browsing a large collection grid.
                Response.Headers["Cache-Control"] = "public, max-age=86400, stale-while-revalidate=3600";

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
        /// Downloads an image from a URL, resizes it to fit within 1600px, saves it to storage,
        /// and optionally generates a 300px thumbnail in the same request.
        /// </summary>
        /// <param name="request">Download request with URL and optional filename hint.</param>
        /// <param name="generateThumbnail">When <c>true</c>, also stores a 300px thumbnail.</param>
        /// <returns>Success or error response containing stored filenames.</returns>
        [HttpPost("download")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DownloadImage(
            [FromBody] ImageDownloadRequest request,
            [FromQuery] bool generateThumbnail = false)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(request.Url))
                {
                    return BadRequest("URL cannot be empty");
                }

                // Security: validate that the URL is an absolute HTTP/HTTPS URL to prevent SSRF
                if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var parsedUrl) ||
                    (parsedUrl.Scheme != Uri.UriSchemeHttp && parsedUrl.Scheme != Uri.UriSchemeHttps))
                {
                    return BadRequest("Only absolute HTTP/HTTPS URLs are accepted.");
                }

                if (!string.IsNullOrWhiteSpace(request.Filename))
                {
                    // Security: Prevent directory traversal attacks
                    if (request.Filename.Contains("..") || Path.IsPathRooted(request.Filename))
                    {
                        _logger.LogWarning("Potentially malicious filename requested: {Filename}", request.Filename);
                        return BadRequest("Invalid filename");
                    }
                }

                // Download the image
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "KollectorScum/1.0");

                var response = await httpClient.GetAsync(parsedUrl);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to download image from {Url}: {StatusCode}", request.Url, response.StatusCode);
                    return StatusCode(500, $"Failed to download image: {response.StatusCode}");
                }

                var imageBytes = await response.Content.ReadAsByteArrayAsync();
                var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";

                // Generate unique filenames
                var baseName = Guid.NewGuid().ToString();
                var ext = !string.IsNullOrWhiteSpace(request.Filename)
                    ? (Path.GetExtension(request.Filename).ToLowerInvariant() is { } e && e.Length > 0 ? e : ".jpg")
                    : ".jpg";
                var filename = $"{baseName}{ext}";
                var thumbFilename = $"thumb-{baseName}{ext}";

                var userId = _userContext.GetActingUserId() ?? Guid.Empty;

                // Resize cover to ≤1600px then store
                using var rawStream = new MemoryStream(imageBytes);
                using var resizedStream = await _imageResizer.ResizeAsync(rawStream, 1600);
                var publicUrl = await _storageService.UploadFileAsync(
                    _bucketName, userId.ToString(), filename, resizedStream, "image/jpeg");

                _logger.LogInformation(
                    "Downloaded and stored image: {Url} -> {StoredFile} (User: {UserId}, Size: {Size})",
                    request.Url, filename, userId, resizedStream.Length);

                // Optionally generate and store thumbnail
                string? thumbPublicUrl = null;
                if (generateThumbnail)
                {
                    resizedStream.Position = 0;
                    using var thumbStream = await _imageResizer.GenerateThumbnailAsync(resizedStream, 300);
                    thumbPublicUrl = await _storageService.UploadFileAsync(
                        _bucketName, userId.ToString(), thumbFilename, thumbStream, "image/jpeg");

                    _logger.LogInformation(
                        "Generated thumbnail for downloaded image: {ThumbFilename} (User: {UserId})", thumbFilename, userId);
                }

                return Ok(new
                {
                    Message = "Image downloaded and stored successfully",
                    Filename = filename,
                    ThumbnailFilename = generateThumbnail ? thumbFilename : (string?)null,
                    Size = resizedStream.Length,
                    PublicUrl = publicUrl,
                    ThumbnailPublicUrl = thumbPublicUrl,
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

        /// <summary>
        /// Accepts a multipart form-file upload, resizes it to fit within 1600px, stores it,
        /// and optionally auto-generates a 300px thumbnail in the same request.
        /// </summary>
        /// <param name="generateThumbnail">When <c>true</c>, a square 300-px thumbnail is also stored.</param>
        /// <param name="file">The image file to upload (max 5 MB; JPEG, PNG, GIF, WebP, BMP or TIFF).</param>
        /// <returns>An <see cref="ImageUploadResponseDto"/> containing the stored filenames and public URLs.</returns>
        [HttpPost("upload")]
        [ProducesResponseType(typeof(ImageUploadResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [RequestSizeLimit(MaxUploadBytes + 1024)]
        public async Task<IActionResult> UploadImage(
            [FromQuery] bool generateThumbnail = true,
            IFormFile? file = null)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file was provided.");

            if (file.Length > MaxUploadBytes)
                return BadRequest($"File exceeds the maximum allowed size of {MaxUploadBytes / 1024 / 1024} MB.");

            var contentType = file.ContentType ?? string.Empty;
            if (!AllowedImageMimeTypes.Contains(contentType))
                return BadRequest("Only image files (JPEG, PNG, GIF, WebP, BMP, TIFF) are accepted.");

            try
            {
                var userId = _userContext.GetActingUserId() ?? Guid.Empty;
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(ext)) ext = ".jpg";
                var baseName = $"{Guid.NewGuid()}";
                var filename = $"{baseName}{ext}";
                var thumbFilename = $"thumb-{baseName}{ext}";

                // Resize cover to ≤1600px
                await using var inputStream = file.OpenReadStream();
                using var resizedStream = await _imageResizer.ResizeAsync(inputStream, 1600);

                // Store cover
                var publicUrl = await _storageService.UploadFileAsync(
                    _bucketName, userId.ToString(), filename, resizedStream, "image/jpeg");

                _logger.LogInformation(
                    "Uploaded image: {Filename} (User: {UserId}, Size: {Size} bytes)",
                    filename, userId, resizedStream.Length);

                // Optionally generate and store thumbnail
                string? thumbPublicUrl = null;
                if (generateThumbnail)
                {
                    resizedStream.Position = 0;
                    using var thumbStream = await _imageResizer.GenerateThumbnailAsync(resizedStream, 300);
                    thumbPublicUrl = await _storageService.UploadFileAsync(
                        _bucketName, userId.ToString(), thumbFilename, thumbStream, "image/jpeg");

                    _logger.LogInformation(
                        "Generated thumbnail: {ThumbFilename} (User: {UserId})", thumbFilename, userId);
                }

                return Ok(new ImageUploadResponseDto
                {
                    Filename = filename,
                    ThumbnailFilename = generateThumbnail ? thumbFilename : null,
                    PublicUrl = publicUrl,
                    ThumbnailPublicUrl = thumbPublicUrl,
                    Size = resizedStream.Length,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image file: {FileName}", file.FileName);
                return StatusCode(500, "Internal server error while uploading image.");
            }
        }

        /// <summary>
        /// Searches MusicBrainz and the Cover Art Archive for album cover art matching
        /// the given query string. Returns up to 4 results ordered by confidence.
        /// </summary>
        /// <param name="q">
        /// Free-text search query (e.g. "Iron Maiden Killers 1981 CD"). Max 200 characters.
        /// </param>
        /// <param name="limit">Maximum results to return (1–10, default 4).</param>
        /// <param name="cancellationToken">Propagates request cancellation.</param>
        /// <returns>Array of <see cref="CoverArtSearchResultDto"/> ordered by confidence descending.</returns>
        [HttpGet("search")]
        [ProducesResponseType(typeof(IReadOnlyList<CoverArtSearchResultDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> SearchCoverArt(
            [FromQuery] string? q,
            [FromQuery] int limit = 4,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest("Query parameter 'q' is required.");

            if (q.Length > 200)
                return BadRequest("Query must not exceed 200 characters.");

            if (limit < 1 || limit > 10)
                return BadRequest("Limit must be between 1 and 10.");

            var results = await _coverArtSearch.SearchAsync(q, limit, cancellationToken);

            if (results.Count == 0)
                return NoContent();

            return Ok(results);
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
