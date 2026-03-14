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
                    _logger.LogWarning("Failed to download cover art from {Url}: {StatusCode}", imageUrl, response.StatusCode);
                    return null;
                }

                var imageBytes = await response.Content.ReadAsByteArrayAsync();
                var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";

                using var ms = new MemoryStream(imageBytes);
                var publicUrl = await _storageService.UploadFileAsync(_bucketName, userId.ToString(), filename, ms, contentType);

                _logger.LogDebug("Uploaded cover art to R2: {Filename} -> {Url}", filename, publicUrl);
                return filename;
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
