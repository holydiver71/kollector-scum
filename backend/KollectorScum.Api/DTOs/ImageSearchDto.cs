using System.ComponentModel.DataAnnotations;

namespace KollectorScum.Api.DTOs
{
    /// <summary>
    /// DTO representing an image search result
    /// </summary>
    public class ImageSearchResultDto
    {
        /// <summary>
        /// Direct URL to the image
        /// </summary>
        public string ImageUrl { get; set; } = string.Empty;

        /// <summary>
        /// URL to a smaller thumbnail version of the image
        /// </summary>
        public string? ThumbnailUrl { get; set; }

        /// <summary>
        /// Width of the image in pixels
        /// </summary>
        public int? Width { get; set; }

        /// <summary>
        /// Height of the image in pixels
        /// </summary>
        public int? Height { get; set; }

        /// <summary>
        /// Title or description of the image
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// URL of the webpage where this image was found
        /// </summary>
        public string? SourceUrl { get; set; }

        /// <summary>
        /// File size in bytes (if available)
        /// </summary>
        public long? FileSize { get; set; }

        /// <summary>
        /// File extension/format (jpg, png, etc.)
        /// </summary>
        public string? Format { get; set; }

        /// <summary>
        /// Calculated aspect ratio (width/height)
        /// </summary>
        public double? AspectRatio => Width.HasValue && Height.HasValue && Height.Value > 0 
            ? (double)Width.Value / Height.Value 
            : null;

        /// <summary>
        /// Indicates if this image appears to be square (good for album covers)
        /// </summary>
        public bool IsSquareAspect => AspectRatio.HasValue && Math.Abs(AspectRatio.Value - 1.0) < 0.1;
    }

    /// <summary>
    /// DTO for requesting image downloads
    /// </summary>
    public class ImageDownloadRequestDto
    {
        /// <summary>
        /// URL of the image to download
        /// </summary>
        [Required]
        public string ImageUrl { get; set; } = string.Empty;

        /// <summary>
        /// Artist name for filename generation
        /// </summary>
        [Required]
        public string Artist { get; set; } = string.Empty;

        /// <summary>
        /// Album title for filename generation
        /// </summary>
        [Required]
        public string Album { get; set; } = string.Empty;

        /// <summary>
        /// Optional year for filename generation
        /// </summary>
        public string? Year { get; set; }

        /// <summary>
        /// Source URL for attribution
        /// </summary>
        public string? SourceUrl { get; set; }

        /// <summary>
        /// Additional attribution information
        /// </summary>
        public string? Attribution { get; set; }
    }

    /// <summary>
    /// DTO for image download response
    /// </summary>
    public class ImageDownloadResponseDto
    {
        /// <summary>
        /// Indicates if the download was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Relative path to the downloaded image
        /// </summary>
        public string? ImagePath { get; set; }

        /// <summary>
        /// Full filename of the downloaded image
        /// </summary>
        public string? FileName { get; set; }

        /// <summary>
        /// File size of the downloaded image in bytes
        /// </summary>
        public long? FileSize { get; set; }

        /// <summary>
        /// Error message if download failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// URL that can be used to access the image through the API
        /// </summary>
        public string? AccessUrl { get; set; }
    }

    /// <summary>
    /// Settings for Google Custom Search API
    /// </summary>
    public class GoogleSearchSettings
    {
        /// <summary>
        /// Google Custom Search API key
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Google Custom Search Engine ID
        /// </summary>
        public string SearchEngineId { get; set; } = string.Empty;

        /// <summary>
        /// Base URL for Google Custom Search API
        /// </summary>
        public string BaseUrl { get; set; } = "https://www.googleapis.com/customsearch/v1";

        /// <summary>
        /// Maximum number of results to return
        /// </summary>
        public int MaxResults { get; set; } = 10;

        /// <summary>
        /// Timeout for API requests in seconds
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;
    }
}