namespace KollectorScum.Api.DTOs
{
    /// <summary>
    /// Represents a single cover-art candidate returned by the image search endpoint.
    /// The <see cref="Confidence"/> value indicates how closely the release metadata
    /// matches the original search query (0.0 = lowest, 1.0 = exact match).
    /// </summary>
    public class CoverArtSearchResultDto
    {
        /// <summary>MusicBrainz Release ID (MBID) of the matched release.</summary>
        public string MbId { get; set; } = string.Empty;

        /// <summary>Artist name as stored in MusicBrainz.</summary>
        public string Artist { get; set; } = string.Empty;

        /// <summary>Release (album) title as stored in MusicBrainz.</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>Release year (null if not available in MusicBrainz data).</summary>
        public int? Year { get; set; }

        /// <summary>Release format, e.g. "CD", "Vinyl", "Digital Media".</summary>
        public string? Format { get; set; }

        /// <summary>ISO 3166-1 alpha-2 country code of the release (null if unavailable).</summary>
        public string? Country { get; set; }

        /// <summary>Label name for the release (null if unavailable).</summary>
        public string? Label { get; set; }

        /// <summary>Catalogue number for the release (null if unavailable or from MusicBrainz only).</summary>
        public string? CatalogueNumber { get; set; }

        /// <summary>
        /// Full-resolution image URL from the Cover Art Archive (500px or 1200px variant).
        /// Null when no cover art is available in the archive.
        /// </summary>
        public string? ImageUrl { get; set; }

        /// <summary>
        /// Thumbnail URL from the Cover Art Archive (250px variant).
        /// Null when no cover art is available.
        /// </summary>
        public string? ThumbnailUrl { get; set; }

        /// <summary>
        /// Normalised confidence score between 0.0 and 1.0.
        /// Derived from the MusicBrainz search score (0–100) divided by 100.
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// Human-readable confidence label for the UI ("Exact match", "Good match", "Possible match").
        /// </summary>
        public string ConfidenceLabel =>
            Confidence >= 0.95 ? "Exact match" :
            Confidence >= 0.75 ? "Good match" :
            "Possible match";
    }

    /// <summary>
    /// Response DTO returned by <c>POST /api/images/upload</c>.
    /// Contains the stored filenames of both the full-resolution cover and the auto-generated thumbnail.
    /// </summary>
    public class ImageUploadResponseDto
    {
        /// <summary>Storage key / filename of the uploaded (and resized) cover image.</summary>
        public string Filename { get; set; } = string.Empty;

        /// <summary>
        /// Storage key / filename of the auto-generated thumbnail (300px).
        /// Null when <c>generateThumbnail=false</c> was requested.
        /// </summary>
        public string? ThumbnailFilename { get; set; }

        /// <summary>Public URL of the stored cover image (when using cloud storage).</summary>
        public string? PublicUrl { get; set; }

        /// <summary>Public URL of the stored thumbnail (when using cloud storage).</summary>
        public string? ThumbnailPublicUrl { get; set; }

        /// <summary>Size of the uploaded image in bytes (after resizing).</summary>
        public long Size { get; set; }
    }
}
