using System.Text.Json.Serialization;

namespace KollectorScum.Api.DTOs
{
    /// <summary>
    /// DTO for importing music release data from JSON
    /// </summary>
    public class MusicReleaseImportDto
    {
        [JsonPropertyName("Id")]
        public int Id { get; set; }

        [JsonPropertyName("Title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("ReleaseYear")]
        public string? ReleaseYear { get; set; }

        [JsonPropertyName("OrigReleaseYear")]
        public string? OrigReleaseYear { get; set; }

        [JsonPropertyName("Artists")]
        public List<int> Artists { get; set; } = new();

        [JsonPropertyName("Genres")]
        public List<int> Genres { get; set; } = new();

        [JsonPropertyName("Live")]
        public bool Live { get; set; }

        [JsonPropertyName("LabelId")]
        public int LabelId { get; set; }

        [JsonPropertyName("CountryId")]
        public int CountryId { get; set; }

        [JsonPropertyName("LabelNumber")]
        public string? LabelNumber { get; set; }

        [JsonPropertyName("LengthInSeconds")]
        public string? LengthInSeconds { get; set; }

        [JsonPropertyName("FormatId")]
        public int FormatId { get; set; }

        [JsonPropertyName("PurchaseInfo")]
        public object? PurchaseInfo { get; set; }

        [JsonPropertyName("PackagingId")]
        public int PackagingId { get; set; }

        [JsonPropertyName("Upc")]
        public string? Upc { get; set; }

        [JsonPropertyName("Images")]
        public ImagesDto? Images { get; set; }

        [JsonPropertyName("Links")]
        public List<LinkDto> Links { get; set; } = new();

        [JsonPropertyName("DateAdded")]
        public DateTime DateAdded { get; set; }

        [JsonPropertyName("LastModified")]
        public DateTime LastModified { get; set; }

        [JsonPropertyName("Media")]
        public List<MediaDto> Media { get; set; } = new();
    }

    /// <summary>
    /// DTO for image information
    /// </summary>
    public class ImagesDto
    {
        [JsonPropertyName("CoverFront")]
        public string? CoverFront { get; set; }

        [JsonPropertyName("CoverBack")]
        public string? CoverBack { get; set; }

        [JsonPropertyName("Thumbnail")]
        public string? Thumbnail { get; set; }
    }

    /// <summary>
    /// DTO for link information
    /// </summary>
    public class LinkDto
    {
        [JsonPropertyName("Description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("Url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("UrlType")]
        public string UrlType { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for media information
    /// </summary>
    public class MediaDto
    {
        [JsonPropertyName("Title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("FormatId")]
        public int FormatId { get; set; }

        [JsonPropertyName("Index")]
        public int Index { get; set; }

        [JsonPropertyName("Tracks")]
        public List<TrackDto> Tracks { get; set; } = new();
    }

    /// <summary>
    /// DTO for track information
    /// </summary>
    public class TrackDto
    {
        [JsonPropertyName("Title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("ReleaseYear")]
        public string? ReleaseYear { get; set; }

        [JsonPropertyName("Artists")]
        public List<string> Artists { get; set; } = new();

        [JsonPropertyName("Genres")]
        public List<string> Genres { get; set; } = new();

        [JsonPropertyName("Live")]
        public bool Live { get; set; }

        [JsonPropertyName("LengthSecs")]
        public int LengthSecs { get; set; }

        [JsonPropertyName("Index")]
        public int Index { get; set; }
    }
}
