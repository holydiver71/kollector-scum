using System.ComponentModel.DataAnnotations;

namespace KollectorScum.Api.DTOs
{
    /// <summary>
    /// DTO for Discogs search request parameters
    /// </summary>
    public class DiscogsSearchRequestDto
    {
        /// <summary>
        /// Catalog number to search for
        /// </summary>
        [Required]
        public string CatalogNumber { get; set; } = string.Empty;

        /// <summary>
        /// Optional format filter (e.g., "CD", "Vinyl", "Cassette")
        /// </summary>
        public string? Format { get; set; }

        /// <summary>
        /// Optional country filter (e.g., "UK", "US", "Japan")
        /// </summary>
        public string? Country { get; set; }

        /// <summary>
        /// Optional year filter
        /// </summary>
        public int? Year { get; set; }
    }

    /// <summary>
    /// DTO for Discogs search result item
    /// </summary>
    public class DiscogsSearchResultDto
    {
        /// <summary>
        /// Discogs release ID
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Release title
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Artist name(s)
        /// </summary>
        public string Artist { get; set; } = string.Empty;

        /// <summary>
        /// Release year
        /// </summary>
        public string? Year { get; set; }

        /// <summary>
        /// Format (e.g., "CD", "Vinyl", "Cassette")
        /// </summary>
        public string? Format { get; set; }

        /// <summary>
        /// Label name
        /// </summary>
        public string? Label { get; set; }

        /// <summary>
        /// Catalog number
        /// </summary>
        public string? CatalogNumber { get; set; }

        /// <summary>
        /// Country of release
        /// </summary>
        public string? Country { get; set; }

        /// <summary>
        /// Thumbnail image URL
        /// </summary>
        public string? ThumbUrl { get; set; }

        /// <summary>
        /// Cover image URL
        /// </summary>
        public string? CoverImageUrl { get; set; }

        /// <summary>
        /// Resource URL for full details
        /// </summary>
        public string? ResourceUrl { get; set; }
    }

    /// <summary>
    /// DTO for full Discogs release details
    /// </summary>
    public class DiscogsReleaseDto
    {
        /// <summary>
        /// Discogs release ID
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Release title
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// List of artists
        /// </summary>
        public List<DiscogsArtistDto> Artists { get; set; } = new();

        /// <summary>
        /// Release year
        /// </summary>
        public int? Year { get; set; }

        /// <summary>
        /// List of genres
        /// </summary>
        public List<string> Genres { get; set; } = new();

        /// <summary>
        /// List of styles (sub-genres)
        /// </summary>
        public List<string> Styles { get; set; } = new();

        /// <summary>
        /// List of labels
        /// </summary>
        public List<DiscogsLabelDto> Labels { get; set; } = new();

        /// <summary>
        /// Country of release
        /// </summary>
        public string? Country { get; set; }

        /// <summary>
        /// Release date
        /// </summary>
        public string? ReleasedDate { get; set; }

        /// <summary>
        /// List of formats
        /// </summary>
        public List<DiscogsFormatDto> Formats { get; set; } = new();

        /// <summary>
        /// List of images
        /// </summary>
        public List<DiscogsImageDto> Images { get; set; } = new();

        /// <summary>
        /// Track listing
        /// </summary>
        public List<DiscogsTrackDto> Tracklist { get; set; } = new();

        /// <summary>
        /// List of identifiers (barcodes, catalog numbers, etc.)
        /// </summary>
        public List<DiscogsIdentifierDto> Identifiers { get; set; } = new();

        /// <summary>
        /// Resource URL
        /// </summary>
        public string? ResourceUrl { get; set; }

        /// <summary>
        /// Discogs URI
        /// </summary>
        public string? Uri { get; set; }

        /// <summary>
        /// Release notes
        /// </summary>
        public string? Notes { get; set; }
    }

    /// <summary>
    /// DTO for Discogs artist information
    /// </summary>
    public class DiscogsArtistDto
    {
        /// <summary>
        /// Artist name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Artist ID
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        /// Resource URL
        /// </summary>
        public string? ResourceUrl { get; set; }
    }

    /// <summary>
    /// DTO for Discogs label information
    /// </summary>
    public class DiscogsLabelDto
    {
        /// <summary>
        /// Label name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Catalog number
        /// </summary>
        public string? CatalogNumber { get; set; }

        /// <summary>
        /// Label ID
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        /// Resource URL
        /// </summary>
        public string? ResourceUrl { get; set; }
    }

    /// <summary>
    /// DTO for Discogs format information
    /// </summary>
    public class DiscogsFormatDto
    {
        /// <summary>
        /// Format name (e.g., "CD", "Vinyl")
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Quantity
        /// </summary>
        public string? Qty { get; set; }

        /// <summary>
        /// Format descriptions
        /// </summary>
        public List<string> Descriptions { get; set; } = new();
    }

    /// <summary>
    /// DTO for Discogs image information
    /// </summary>
    public class DiscogsImageDto
    {
        /// <summary>
        /// Image type (primary, secondary, etc.)
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Image URI
        /// </summary>
        public string Uri { get; set; } = string.Empty;

        /// <summary>
        /// Thumbnail URI (150px)
        /// </summary>
        public string? Uri150 { get; set; }

        /// <summary>
        /// Resource URL
        /// </summary>
        public string? ResourceUrl { get; set; }

        /// <summary>
        /// Image width
        /// </summary>
        public int? Width { get; set; }

        /// <summary>
        /// Image height
        /// </summary>
        public int? Height { get; set; }
    }

    /// <summary>
    /// DTO for Discogs track information
    /// </summary>
    public class DiscogsTrackDto
    {
        /// <summary>
        /// Track position (e.g., "1", "A1", "CD1-1")
        /// </summary>
        public string Position { get; set; } = string.Empty;

        /// <summary>
        /// Track title
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Track duration
        /// </summary>
        public string? Duration { get; set; }

        /// <summary>
        /// Track artists (if different from album artists)
        /// </summary>
        public List<DiscogsArtistDto>? Artists { get; set; }
    }

    /// <summary>
    /// DTO for Discogs identifier (barcode, matrix, etc.)
    /// </summary>
    public class DiscogsIdentifierDto
    {
        /// <summary>
        /// Identifier type (e.g., "Barcode", "Matrix / Runout")
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Identifier value
        /// </summary>
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for Discogs user collection response
    /// </summary>
    public class DiscogsCollectionResponseDto
    {
        /// <summary>
        /// Pagination information
        /// </summary>
        public DiscogsPaginationDto? Pagination { get; set; }

        /// <summary>
        /// List of releases in collection
        /// </summary>
        public List<DiscogsCollectionReleaseDto> Releases { get; set; } = new();
    }

    /// <summary>
    /// DTO for pagination information
    /// </summary>
    public class DiscogsPaginationDto
    {
        /// <summary>
        /// Current page number
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// Number of items per page
        /// </summary>
        public int PerPage { get; set; }

        /// <summary>
        /// Total number of pages
        /// </summary>
        public int Pages { get; set; }

        /// <summary>
        /// Total number of items
        /// </summary>
        public int Items { get; set; }
    }

    /// <summary>
    /// DTO for a release in user's collection
    /// </summary>
    public class DiscogsCollectionReleaseDto
    {
        /// <summary>
        /// Instance ID (unique to this collection item)
        /// </summary>
        public string? InstanceId { get; set; }

        /// <summary>
        /// Rating (0-5)
        /// </summary>
        public int? Rating { get; set; }

        /// <summary>
        /// Basic information about the release
        /// </summary>
        public DiscogsBasicInfoDto? BasicInformation { get; set; }

        /// <summary>
        /// User notes about this release
        /// </summary>
        public List<DiscogsNoteDto>? Notes { get; set; }

        /// <summary>
        /// Date added to collection
        /// </summary>
        public string? DateAdded { get; set; }
    }

    /// <summary>
    /// DTO for basic release information in collection
    /// </summary>
    public class DiscogsBasicInfoDto
    {
        /// <summary>
        /// Release ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Release title
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Release year
        /// </summary>
        public int? Year { get; set; }

        /// <summary>
        /// List of artists
        /// </summary>
        public List<DiscogsArtistDto> Artists { get; set; } = new();

        /// <summary>
        /// List of labels
        /// </summary>
        public List<DiscogsLabelDto> Labels { get; set; } = new();

        /// <summary>
        /// List of formats
        /// </summary>
        public List<DiscogsFormatDto> Formats { get; set; } = new();

        /// <summary>
        /// List of genres
        /// </summary>
        public List<string> Genres { get; set; } = new();

        /// <summary>
        /// List of styles (sub-genres)
        /// </summary>
        public List<string> Styles { get; set; } = new();

        /// <summary>
        /// Country of release
        /// </summary>
        public string? Country { get; set; }

        /// <summary>
        /// Cover image URL
        /// </summary>
        public string? CoverImage { get; set; }

        /// <summary>
        /// Thumbnail URL
        /// </summary>
        public string? Thumb { get; set; }

        /// <summary>
        /// Resource URL for full details
        /// </summary>
        public string? ResourceUrl { get; set; }
    }

    /// <summary>
    /// DTO for notes on collection items
    /// </summary>
    public class DiscogsNoteDto
    {
        /// <summary>
        /// Note field ID
        /// </summary>
        public int FieldId { get; set; }

        /// <summary>
        /// Note value/text
        /// </summary>
        public string? Value { get; set; }
    }
}
