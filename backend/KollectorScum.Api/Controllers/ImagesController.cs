using Microsoft.AspNetCore.Mvc;
using KollectorScum.Api.Interfaces;
using System.Text.Json;
using System.Text.Json.Serialization;

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
        private readonly IImageResizerService _imageResizerService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        private static readonly HashSet<string> AllowedExtensions =
            new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

        private const long MaxFileSizeBytes = 5_242_880; // 5 MB
        private const int CoverMaxDimension = 1600;
        private const int ThumbnailMaxDimension = 300;

        /// <summary>Initialises a new instance of <see cref="ImagesController"/>.</summary>
        public ImagesController(
            IConfiguration configuration,
            ILogger<ImagesController> logger,
            IStorageService storageService,
            IUserContext userContext,
            IImageResizerService imageResizerService,
            IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _imagesPath = configuration["ImagesPath"] ?? "/home/andy/music-images";
            // Support both configuration key formats: the env provider maps '__' to ':'
            // so try the colon form first and fall back to the literal double-underscore form.
            _r2PublicBaseUrl = configuration["R2:PublicBaseUrl"] ?? configuration["R2__PublicBaseUrl"];
            _bucketName = configuration["R2:BucketName"] ?? configuration["R2__BucketName"] ?? "cover-art";
            _logger = logger;
            _storageService = storageService;
            _userContext = userContext;
            _imageResizerService = imageResizerService;
            _httpClientFactory = httpClientFactory;
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
        /// and optionally generates a 300px thumbnail.
        /// </summary>
        /// <param name="request">Download request with URL and filename</param>
        /// <param name="generateThumbnail">When true, also create a thumbnail stored with a <c>thumb-</c> prefix.</param>
        /// <returns>Success or error response including public URLs</returns>
        [HttpPost("download")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DownloadImage([FromBody] ImageDownloadRequest request, [FromQuery] bool generateThumbnail = false)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(request.Url))
                    return BadRequest("URL cannot be empty");

                if (string.IsNullOrWhiteSpace(request.Filename))
                    return BadRequest("Filename cannot be empty");

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
                    if (request.Folder.Contains("..") || Path.IsPathRooted(request.Folder))
                    {
                        _logger.LogWarning("Potentially malicious folder requested: {Folder}", request.Folder);
                        return BadRequest("Invalid folder");
                    }
                    targetFolder = request.Folder;
                }
                else if (request.Filename.StartsWith("thumb-", StringComparison.OrdinalIgnoreCase))
                {
                    targetFolder = "thumbnails";
                }

                // Ensure target directory exists
                var targetPath = Path.Combine(_imagesPath, targetFolder);
                if (!Directory.Exists(targetPath))
                {
                    Directory.CreateDirectory(targetPath);
                    _logger.LogInformation("Created {Folder} directory: {Path}", targetFolder, targetPath);
                }

                // Build unique filename
                var originalFilename = request.Filename;
                var fullPath = Path.Combine(targetPath, originalFilename);
                var finalFilename = originalFilename;

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

                // Download the image using the named 'image-downloader' HttpClient
                var httpClient = _httpClientFactory.CreateClient("image-downloader");

                var response = await httpClient.GetAsync(request.Url);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to download image from {Url}: {StatusCode}", request.Url, response.StatusCode);
                    return StatusCode(500, $"Failed to download image: {response.StatusCode}");
                }

                var imageBytes = await response.Content.ReadAsByteArrayAsync();
                var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";

                // Sanitize filename
                var cleanName = Path.GetFileName(
                    string.IsNullOrWhiteSpace(request.Filename) ? $"{Guid.NewGuid()}.jpg" : request.Filename);
                var uniqueFileName = cleanName;

                // Determine user id for multi-tenant storage; fall back to Guid.Empty if not authenticated
                var userId = _userContext.GetActingUserId() ?? Guid.Empty;

                _logger.LogInformation("Resizing cover image to max {Max}px. Bucket: {Bucket}, User: {User}, File: {File}",
                    CoverMaxDimension, _bucketName, userId, uniqueFileName);

                // Resize cover to max 1600px before storing
                string publicUrl;
                using (var sourceStream = new MemoryStream(imageBytes))
                {
                    using var resizedStream = await _imageResizerService.ResizeAsync(sourceStream, CoverMaxDimension, contentType);
                    publicUrl = await _storageService.UploadFileAsync(_bucketName, userId.ToString(), uniqueFileName, resizedStream, "image/jpeg");
                }

                _logger.LogInformation("Downloaded and stored image: {Url} -> {StoredFile} (User: {UserId}, Size: {Size})",
                    request.Url, uniqueFileName, userId, imageBytes.Length);

                // Optionally generate a thumbnail at 300px
                string? thumbnailFilename = null;
                string? thumbnailPublicUrl = null;
                if (generateThumbnail)
                {
                    thumbnailFilename = $"thumb-{uniqueFileName}";
                    _logger.LogInformation("Generating thumbnail {Thumb} at max {Max}px.", thumbnailFilename, ThumbnailMaxDimension);

                    using var thumbSource = new MemoryStream(imageBytes);
                    using var thumbStream = await _imageResizerService.ResizeAsync(thumbSource, ThumbnailMaxDimension, contentType);
                    thumbnailPublicUrl = await _storageService.UploadFileAsync(_bucketName, userId.ToString(), thumbnailFilename, thumbStream, "image/jpeg");
                }

                return Ok(new ImageStoreResult
                {
                    Filename = uniqueFileName,
                    PublicUrl = publicUrl,
                    ThumbnailFilename = thumbnailFilename,
                    ThumbnailPublicUrl = thumbnailPublicUrl
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
        /// Accepts a direct file upload, resizes it to fit within 1600 px, stores it,
        /// and optionally generates a 300 px thumbnail.
        /// </summary>
        /// <param name="file">The image file to upload (max 5 MB; .jpg .jpeg .png .webp .gif).</param>
        /// <param name="generateThumbnail">When true, also create a thumbnail stored with a <c>thumb-</c> prefix.</param>
        /// <returns>JSON with <c>Filename</c>, <c>PublicUrl</c>, and optionally <c>ThumbnailFilename</c> / <c>ThumbnailPublicUrl</c>.</returns>
        [HttpPost("upload")]
        [RequestSizeLimit(MaxFileSizeBytes)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UploadImage(IFormFile file, [FromQuery] bool generateThumbnail = false)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest("No file provided.");

                if (file.Length > MaxFileSizeBytes)
                    return BadRequest($"File size exceeds the 5 MB limit ({file.Length} bytes).");

                var ext = Path.GetExtension(file.FileName);
                if (!AllowedExtensions.Contains(ext))
                    return BadRequest($"File extension '{ext}' is not allowed. Allowed: {string.Join(", ", AllowedExtensions)}");

                var uniqueFileName = $"{Guid.NewGuid()}.jpg";
                var userId = _userContext.GetActingUserId() ?? Guid.Empty;
                var contentType = file.ContentType ?? "image/jpeg";

                _logger.LogInformation("Uploading file. Bucket: {Bucket}, User: {User}, File: {File}", _bucketName, userId, uniqueFileName);

                // Read + resize cover to max 1600px
                string publicUrl;
                byte[] originalBytes;
                using (var ms = new MemoryStream())
                {
                    await file.CopyToAsync(ms);
                    originalBytes = ms.ToArray();
                }

                using (var sourceStream = new MemoryStream(originalBytes))
                {
                    using var resized = await _imageResizerService.ResizeAsync(sourceStream, CoverMaxDimension, contentType);
                    publicUrl = await _storageService.UploadFileAsync(_bucketName, userId.ToString(), uniqueFileName, resized, "image/jpeg");
                }

                // Optionally generate thumbnail
                string? thumbnailFilename = null;
                string? thumbnailPublicUrl = null;
                if (generateThumbnail)
                {
                    thumbnailFilename = $"thumb-{uniqueFileName}";
                    _logger.LogInformation("Generating upload thumbnail {Thumb}.", thumbnailFilename);
                    using var thumbSource = new MemoryStream(originalBytes);
                    using var thumbStream = await _imageResizerService.ResizeAsync(thumbSource, ThumbnailMaxDimension, contentType);
                    thumbnailPublicUrl = await _storageService.UploadFileAsync(_bucketName, userId.ToString(), thumbnailFilename, thumbStream, "image/jpeg");
                }

                return Ok(new ImageStoreResult
                {
                    Filename = uniqueFileName,
                    PublicUrl = publicUrl,
                    ThumbnailFilename = thumbnailFilename,
                    ThumbnailPublicUrl = thumbnailPublicUrl
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image.");
                return StatusCode(500, "Internal server error while uploading image.");
            }
        }

        /// <summary>
        /// Proxies an image search query to Google Custom Search JSON API and returns
        /// a list of image results. SSRF mitigation: only the sanitised <paramref name="q"/>
        /// string is forwarded; no user-supplied URLs are followed.
        /// </summary>
        /// <param name="q">Search query (max 200 characters).</param>
        /// <returns>Array of <see cref="ImageSearchResult"/> or 204 when Google returns no results.</returns>
        [HttpGet("search")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        public async Task<IActionResult> SearchImages([FromQuery] string? q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest("Query parameter 'q' is required.");

            if (q.Length > 200)
                return BadRequest("Query must be 200 characters or fewer.");

            var apiKey = _configuration["Google:ApiKey"] ?? _configuration["Google__ApiKey"];
            var engineId = _configuration["Google:SearchEngineId"] ?? _configuration["Google__SearchEngineId"];

            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(engineId))
            {
                _logger.LogWarning("Google Image Search is not configured (missing ApiKey or SearchEngineId).");
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    "Image search is not configured. Set Google:ApiKey and Google:SearchEngineId in app configuration.");
            }

            try
            {
                // Build a safe query string – only q, key, cx, and fixed searchType params are included.
                var encodedQ = Uri.EscapeDataString(q);
                var requestUrl =
                    $"https://www.googleapis.com/customsearch/v1?key={Uri.EscapeDataString(apiKey)}&cx={Uri.EscapeDataString(engineId)}&searchType=image&num=10&q={encodedQ}";

                var client = _httpClientFactory.CreateClient("google-image-search");
                var response = await client.GetAsync(requestUrl);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Google Custom Search returned {Status} for query '{Q}'", response.StatusCode, q);
                    return StatusCode(StatusCodes.Status502BadGateway,
                        $"Image search upstream error: {response.StatusCode}");
                }

                var body = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(body);

                if (!doc.RootElement.TryGetProperty("items", out var items))
                {
                    _logger.LogDebug("Google image search returned no items for query '{Q}'", q);
                    return NoContent();
                }

                var results = new List<ImageSearchResult>();
                foreach (var item in items.EnumerateArray())
                {
                    var imageUrl = item.TryGetProperty("link", out var link) ? link.GetString() : null;
                    var title = item.TryGetProperty("title", out var t) ? t.GetString() : null;
                    string? thumbUrl = null;
                    int width = 0, height = 0;

                    if (item.TryGetProperty("image", out var imageInfo))
                    {
                        if (imageInfo.TryGetProperty("thumbnailLink", out var tl)) thumbUrl = tl.GetString();
                        if (imageInfo.TryGetProperty("width", out var w)) w.TryGetInt32(out width);
                        if (imageInfo.TryGetProperty("height", out var h)) h.TryGetInt32(out height);
                    }

                    if (!string.IsNullOrWhiteSpace(imageUrl))
                    {
                        results.Add(new ImageSearchResult
                        {
                            Title = title ?? string.Empty,
                            ImageUrl = imageUrl,
                            ThumbnailUrl = thumbUrl ?? imageUrl,
                            Width = width,
                            Height = height,
                        });
                    }
                }

                return results.Count == 0 ? NoContent() : Ok(results);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error calling Google Custom Search for query '{Q}'", q);
                return StatusCode(StatusCodes.Status502BadGateway, "Failed to reach image search provider.");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse Google Custom Search response for query '{Q}'", q);
                return StatusCode(StatusCodes.Status502BadGateway, "Failed to parse image search response.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching images for query '{Q}'", q);
                return StatusCode(500, "Internal server error while searching images.");
            }
        }
    }

    // ─── Request / Response models ─────────────────────────────────────────────

    /// <summary>
    /// Request model for image download
    /// </summary>
    public class ImageDownloadRequest
    {
        /// <summary>URL of the image to download.</summary>
        public string Url { get; set; } = string.Empty;
        /// <summary>Desired filename for the stored image.</summary>
        public string Filename { get; set; } = string.Empty;
        /// <summary>Optional target folder override (e.g. "covers", "thumbnails").</summary>
        public string? Folder { get; set; }
    }

    /// <summary>
    /// Response returned by the upload and download endpoints.
    /// </summary>
    public class ImageStoreResult
    {
        /// <summary>Stored filename of the main image.</summary>
        public string Filename { get; set; } = string.Empty;
        /// <summary>Public URL of the main image.</summary>
        public string PublicUrl { get; set; } = string.Empty;
        /// <summary>Stored filename of the auto-generated thumbnail, if requested.</summary>
        public string? ThumbnailFilename { get; set; }
        /// <summary>Public URL of the auto-generated thumbnail, if requested.</summary>
        public string? ThumbnailPublicUrl { get; set; }
    }

    /// <summary>
    /// A single result from the Google image search proxy.
    /// </summary>
    public class ImageSearchResult
    {
        /// <summary>Page / image title from Google.</summary>
        public string Title { get; set; } = string.Empty;
        /// <summary>Direct URL to the full-size image.</summary>
        public string ImageUrl { get; set; } = string.Empty;
        /// <summary>URL to the Google-hosted thumbnail.</summary>
        public string ThumbnailUrl { get; set; } = string.Empty;
        /// <summary>Reported image width in pixels.</summary>
        public int Width { get; set; }
        /// <summary>Reported image height in pixels.</summary>
        public int Height { get; set; }
    }
}
