using System.Text;
using KollectorScum.Api.Interfaces;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// Service for downloading Discogs cover art and uploading it to R2 storage.
    /// </summary>
    public class DiscogsImageService : IDiscogsImageService
    {
        private readonly HttpClient _httpClient;
        private readonly IStorageService _storageService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DiscogsImageService> _logger;
        private readonly string _bucketName;

        private const int MaxFilenameLength = 200;

        /// <summary>
        /// Initializes a new instance of <see cref="DiscogsImageService"/>.
        /// </summary>
        public DiscogsImageService(
            HttpClient httpClient,
            IStorageService storageService,
            IConfiguration configuration,
            ILogger<DiscogsImageService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _bucketName = _configuration["R2:BucketName"] ?? _configuration["R2__BucketName"] ?? "cover-art-staging";
        }

        /// <inheritdoc />
        public async Task<string?> DownloadAndStoreCoverArtAsync(
            string imageUrl,
            string artist,
            string title,
            string? year,
            Guid userId)
        {
            try
            {
                var filename = SanitizeFilename($"{artist}-{title}-{year ?? "Unknown"}.jpg");

                _logger.LogDebug("Downloading cover art from: {Url}", imageUrl);
                var response = await _httpClient.GetAsync(imageUrl);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to download cover art from {Url}: {StatusCode} {ReasonPhrase}", imageUrl, response.StatusCode, response.ReasonPhrase);
                    return null;
                }

                var contentLength = response.Content.Headers.ContentLength;
                var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";

                _logger.LogDebug("Downloaded cover art metadata: Url={Url}, ContentType={ContentType}, ContentLength={ContentLength}",
                    imageUrl, contentType, contentLength ?? -1);

                var imageBytes = await response.Content.ReadAsByteArrayAsync();

                if (imageBytes == null || imageBytes.Length == 0)
                {
                    _logger.LogWarning("Downloaded cover art is empty for {Url}", imageUrl);
                    return null;
                }

                using var ms = new MemoryStream(imageBytes);

                try
                {
                    var publicUrl = await _storageService.UploadFileAsync(_bucketName, userId.ToString(), filename, ms, contentType);
                    _logger.LogDebug("Uploaded cover art to R2: {Filename} -> {Url}", filename, publicUrl);
                    return filename;
                }
                catch (Exception exUpload)
                {
                    _logger.LogError(exUpload, "Failed to upload cover art to storage for {Url} (filename={Filename})", imageUrl, filename);
                    // Let caller decide on fallback; return null to indicate upload failure
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading/uploading cover art from {Url}", imageUrl);
                return null;
            }
        }

        /// <inheritdoc />
        public string SanitizeFilename(string filename)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new StringBuilder();

            foreach (var c in filename)
            {
                sanitized.Append(invalidChars.Contains(c) ? '_' : c);
            }

            var result = sanitized.ToString();
            if (result.Length > MaxFilenameLength)
            {
                var extension = Path.GetExtension(result);
                result = result[..(MaxFilenameLength - extension.Length)] + extension;
            }

            return result;
        }
    }
}
