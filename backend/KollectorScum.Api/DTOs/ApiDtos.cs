using System.ComponentModel.DataAnnotations;

namespace KollectorScum.Api.DTOs
{
    /// <summary>
    /// Base class for paginated results
    /// </summary>
    /// <typeparam name="T">The type of items in the result</typeparam>
    public class PagedResult<T>
    {
        /// <summary>
        /// The items in the current page
        /// </summary>
        public IEnumerable<T> Items { get; set; } = new List<T>();

        /// <summary>
        /// Current page number (1-based)
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// Page size
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Total number of items across all pages
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Total number of pages
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// Whether there is a previous page
        /// </summary>
        public bool HasPrevious => Page > 1;

        /// <summary>
        /// Whether there is a next page
        /// </summary>
        public bool HasNext => Page < TotalPages;
    }

    /// <summary>
    /// DTO for search suggestions/autocomplete
    /// </summary>
    public class SearchSuggestionDto
    {
        /// <summary>
        /// Type of suggestion (release, artist, label, etc.)
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// ID of the suggested item
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Display name of the suggestion
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Optional subtitle (e.g., year for releases)
        /// </summary>
        public string? Subtitle { get; set; }
    }

    /// <summary>
    /// DTO for collection statistics
    /// </summary>
    public class CollectionStatisticsDto
    {
        /// <summary>
        /// Total number of releases in collection
        /// </summary>
        public int TotalReleases { get; set; }

        /// <summary>
        /// Total number of unique artists
        /// </summary>
        public int TotalArtists { get; set; }

        /// <summary>
        /// Total number of unique genres
        /// </summary>
        public int TotalGenres { get; set; }

        /// <summary>
        /// Total number of unique labels
        /// </summary>
        public int TotalLabels { get; set; }

        /// <summary>
        /// Releases by year statistics
        /// </summary>
        public List<YearStatisticDto> ReleasesByYear { get; set; } = new();

        /// <summary>
        /// Releases by genre statistics
        /// </summary>
        public List<GenreStatisticDto> ReleasesByGenre { get; set; } = new();

        /// <summary>
        /// Releases by format statistics
        /// </summary>
        public List<FormatStatisticDto> ReleasesByFormat { get; set; } = new();

        /// <summary>
        /// Releases by country statistics
        /// </summary>
        public List<CountryStatisticDto> ReleasesByCountry { get; set; } = new();

        /// <summary>
        /// Total collection value (sum of purchase prices)
        /// </summary>
        public decimal? TotalValue { get; set; }

        /// <summary>
        /// Average release price
        /// </summary>
        public decimal? AveragePrice { get; set; }

        /// <summary>
        /// Most expensive release
        /// </summary>
        public MusicReleaseSummaryDto? MostExpensiveRelease { get; set; }

        /// <summary>
        /// Recently added releases (last 10)
        /// </summary>
        public List<MusicReleaseSummaryDto> RecentlyAdded { get; set; } = new();
    }

    /// <summary>
    /// DTO for year-based statistics
    /// </summary>
    public class YearStatisticDto
    {
        /// <summary>
        /// Year
        /// </summary>
        public int Year { get; set; }

        /// <summary>
        /// Number of releases from this year
        /// </summary>
        public int Count { get; set; }
    }

    /// <summary>
    /// DTO for genre-based statistics
    /// </summary>
    public class GenreStatisticDto
    {
        /// <summary>
        /// Genre ID
        /// </summary>
        public int GenreId { get; set; }

        /// <summary>
        /// Genre name
        /// </summary>
        public string GenreName { get; set; } = string.Empty;

        /// <summary>
        /// Number of releases with this genre
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Percentage of total collection
        /// </summary>
        public decimal Percentage { get; set; }
    }

    /// <summary>
    /// DTO for format-based statistics
    /// </summary>
    public class FormatStatisticDto
    {
        /// <summary>
        /// Format ID
        /// </summary>
        public int FormatId { get; set; }

        /// <summary>
        /// Format name
        /// </summary>
        public string FormatName { get; set; } = string.Empty;

        /// <summary>
        /// Number of releases in this format
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Percentage of total collection
        /// </summary>
        public decimal Percentage { get; set; }
    }

    /// <summary>
    /// DTO for country-based statistics
    /// </summary>
    public class CountryStatisticDto
    {
        /// <summary>
        /// Country ID
        /// </summary>
        public int CountryId { get; set; }

        /// <summary>
        /// Country name
        /// </summary>
        public string CountryName { get; set; } = string.Empty;

        /// <summary>
        /// Number of releases from this country
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Percentage of total collection
        /// </summary>
        public decimal Percentage { get; set; }
    }

    // Country DTOs
    /// <summary>
    /// DTO for country data
    /// </summary>
    public class CountryDto
    {
        /// <summary>
        /// Country ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Country name
        /// </summary>
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for creating a new country
    /// </summary>
    public class CreateCountryDto
    {
        /// <summary>
        /// Country name
        /// </summary>
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for updating a country
    /// </summary>
    public class UpdateCountryDto
    {
        /// <summary>
        /// Country name
        /// </summary>
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;
    }

    // Store DTOs
    /// <summary>
    /// DTO for store data
    /// </summary>
    public class StoreDto
    {
        /// <summary>
        /// Store ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Store name
        /// </summary>
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for creating a new store
    /// </summary>
    public class CreateStoreDto
    {
        /// <summary>
        /// Store name
        /// </summary>
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for updating a store
    /// </summary>
    public class UpdateStoreDto
    {
        /// <summary>
        /// Store name
        /// </summary>
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;
    }

    // Format DTOs
    /// <summary>
    /// DTO for format data
    /// </summary>
    public class FormatDto
    {
        /// <summary>
        /// Format ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Format name
        /// </summary>
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for creating a new format
    /// </summary>
    public class CreateFormatDto
    {
        /// <summary>
        /// Format name
        /// </summary>
        [Required]
        [StringLength(50, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for updating a format
    /// </summary>
    public class UpdateFormatDto
    {
        /// <summary>
        /// Format name
        /// </summary>
        [Required]
        [StringLength(50, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;
    }

    // Genre DTOs
    /// <summary>
    /// DTO for genre data
    /// </summary>
    public class GenreDto
    {
        /// <summary>
        /// Genre ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Genre name
        /// </summary>
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for creating a new genre
    /// </summary>
    public class CreateGenreDto
    {
        /// <summary>
        /// Genre name
        /// </summary>
        [Required]
        [StringLength(50, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for updating a genre
    /// </summary>
    public class UpdateGenreDto
    {
        /// <summary>
        /// Genre name
        /// </summary>
        [Required]
        [StringLength(50, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;
    }

    // Label DTOs
    /// <summary>
    /// DTO for label data
    /// </summary>
    public class LabelDto
    {
        /// <summary>
        /// Label ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Label name
        /// </summary>
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for creating a new label
    /// </summary>
    public class CreateLabelDto
    {
        /// <summary>
        /// Label name
        /// </summary>
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for updating a label
    /// </summary>
    public class UpdateLabelDto
    {
        /// <summary>
        /// Label name
        /// </summary>
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;
    }

    // Artist DTOs
    /// <summary>
    /// DTO for artist data
    /// </summary>
    public class ArtistDto
    {
        /// <summary>
        /// Artist ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Artist name
        /// </summary>
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for creating a new artist
    /// </summary>
    public class CreateArtistDto
    {
        /// <summary>
        /// Artist name
        /// </summary>
        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for updating an artist
    /// </summary>
    public class UpdateArtistDto
    {
        /// <summary>
        /// Artist name
        /// </summary>
        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;
    }

    // Packaging DTOs
    /// <summary>
    /// DTO for packaging data
    /// </summary>
    public class PackagingDto
    {
        /// <summary>
        /// Packaging ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Packaging name
        /// </summary>
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for creating a new packaging
    /// </summary>
    public class CreatePackagingDto
    {
        /// <summary>
        /// Packaging name
        /// </summary>
        [Required]
        [StringLength(50, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for updating a packaging
    /// </summary>
    public class UpdatePackagingDto
    {
        /// <summary>
        /// Packaging name
        /// </summary>
        [Required]
        [StringLength(50, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;
    }

    // ===== MUSIC RELEASE DTOs =====

    /// <summary>
    /// DTO for creating a music release
    /// Supports both ID-based and name-based lookup data for auto-creation
    /// </summary>
    public class CreateMusicReleaseDto
    {
        [Required]
        [StringLength(300)]
        public string Title { get; set; } = string.Empty;
        
        public DateTime? ReleaseYear { get; set; }
        public DateTime? OrigReleaseYear { get; set; }
        
        // Artist lookup - can provide IDs OR names (or both)
        public List<int>? ArtistIds { get; set; }
        public List<string>? ArtistNames { get; set; }
        
        // Genre lookup - can provide IDs OR names (or both)
        public List<int>? GenreIds { get; set; }
        public List<string>? GenreNames { get; set; }
        
        public bool Live { get; set; }
        
        // Label lookup - can provide ID OR name
        public int? LabelId { get; set; }
        public string? LabelName { get; set; }
        
        // Country lookup - can provide ID OR name
        public int? CountryId { get; set; }
        public string? CountryName { get; set; }
        
        [StringLength(100)]
        public string? LabelNumber { get; set; }
        
        [StringLength(50)]
        public string? Upc { get; set; }
        
        public int? LengthInSeconds { get; set; }
        
        // Format lookup - can provide ID OR name
        public int? FormatId { get; set; }
        public string? FormatName { get; set; }
        
        // Packaging lookup - can provide ID OR name
        public int? PackagingId { get; set; }
        public string? PackagingName { get; set; }
        
        public MusicReleasePurchaseInfoDto? PurchaseInfo { get; set; }
        public MusicReleaseImageDto? Images { get; set; }
        public List<MusicReleaseLinkDto>? Links { get; set; }
        public List<MusicReleaseMediaDto>? Media { get; set; }
    }

    /// <summary>
    /// DTO for updating a music release
    /// </summary>
    public class UpdateMusicReleaseDto
    {
        [Required]
        [StringLength(300)]
        public string Title { get; set; } = string.Empty;
        
        public DateTime? ReleaseYear { get; set; }
        public DateTime? OrigReleaseYear { get; set; }
        public List<int>? ArtistIds { get; set; }
        public List<int>? GenreIds { get; set; }
        public bool Live { get; set; }
        public int? LabelId { get; set; }
        public int? CountryId { get; set; }
        
        [StringLength(100)]
        public string? LabelNumber { get; set; }
        
        public int? LengthInSeconds { get; set; }
        public int? FormatId { get; set; }
        public int? PackagingId { get; set; }
        public MusicReleasePurchaseInfoDto? PurchaseInfo { get; set; }
        public MusicReleaseImageDto? Images { get; set; }
        public List<MusicReleaseLinkDto>? Links { get; set; }
        public List<MusicReleaseMediaDto>? Media { get; set; }
    }

    /// <summary>
    /// DTO for music release response with full details
    /// </summary>
    public class MusicReleaseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime? ReleaseYear { get; set; }
        public DateTime? OrigReleaseYear { get; set; }
        public List<ArtistDto>? Artists { get; set; }
        public List<GenreDto>? Genres { get; set; }
        public bool Live { get; set; }
        public LabelDto? Label { get; set; }
        public CountryDto? Country { get; set; }
        public string? LabelNumber { get; set; }
        public int? LengthInSeconds { get; set; }
        public FormatDto? Format { get; set; }
        public PackagingDto? Packaging { get; set; }
        public string? Upc { get; set; }
        public MusicReleasePurchaseInfoDto? PurchaseInfo { get; set; }
        public MusicReleaseImageDto? Images { get; set; }
        public List<MusicReleaseLinkDto>? Links { get; set; }
        public List<MusicReleaseMediaDto>? Media { get; set; }
        public DateTime DateAdded { get; set; }
        public DateTime LastModified { get; set; }
    }

    /// <summary>
    /// DTO for create music release response with auto-creation details
    /// </summary>
    public class CreateMusicReleaseResponseDto
    {
        /// <summary>
        /// The created music release
        /// </summary>
        public MusicReleaseDto Release { get; set; } = new();

        /// <summary>
        /// Details about what lookup entities were created during this operation
        /// </summary>
        public CreatedEntitiesDto? Created { get; set; }
    }

    /// <summary>
    /// DTO containing information about entities that were auto-created
    /// </summary>
    public class CreatedEntitiesDto
    {
        /// <summary>
        /// Artists that were created
        /// </summary>
        public List<ArtistDto>? Artists { get; set; }

        /// <summary>
        /// Labels that were created
        /// </summary>
        public List<LabelDto>? Labels { get; set; }

        /// <summary>
        /// Genres that were created
        /// </summary>
        public List<GenreDto>? Genres { get; set; }

        /// <summary>
        /// Countries that were created
        /// </summary>
        public List<CountryDto>? Countries { get; set; }

        /// <summary>
        /// Formats that were created
        /// </summary>
        public List<FormatDto>? Formats { get; set; }

        /// <summary>
        /// Packagings that were created
        /// </summary>
        public List<PackagingDto>? Packagings { get; set; }

        /// <summary>
        /// Stores that were created
        /// </summary>
        public List<StoreDto>? Stores { get; set; }
    }

    /// <summary>
    /// DTO for music release summary (for lists and search results)
    /// </summary>
    public class MusicReleaseSummaryDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime? ReleaseYear { get; set; }
        public DateTime? OrigReleaseYear { get; set; }
        public List<string>? ArtistNames { get; set; }
        public List<string>? GenreNames { get; set; }
        public string? LabelName { get; set; }
        public string? FormatName { get; set; }
        public string? CountryName { get; set; }
        public string? CoverImageUrl { get; set; }
        public DateTime DateAdded { get; set; }
    }

    // ===== VALUE OBJECT DTOs FOR MUSIC RELEASES =====

    /// <summary>
    /// DTO for purchase information
    /// </summary>
    public class MusicReleasePurchaseInfoDto
    {
        public int? StoreId { get; set; }
        public string? StoreName { get; set; }
        public decimal? Price { get; set; }
        public string? Currency { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// DTO for image information - matches the JSON structure in the database
    /// </summary>
    public class MusicReleaseImageDto
    {
        public string? CoverFront { get; set; }
        public string? CoverBack { get; set; }
        public string? Thumbnail { get; set; }
    }

    /// <summary>
    /// DTO for link information
    /// </summary>
    public class MusicReleaseLinkDto
    {
        public string? Url { get; set; }
        public string? Type { get; set; } // "spotify", "discogs", "musicbrainz", etc.
        public string? Description { get; set; }
    }

    /// <summary>
    /// DTO for media information (CD, Vinyl side, etc.)
    /// </summary>
    public class MusicReleaseMediaDto
    {
        public string? Name { get; set; }
        public List<MusicReleaseTrackDto>? Tracks { get; set; }
    }

    /// <summary>
    /// DTO for track information
    /// </summary>
    public class MusicReleaseTrackDto
    {
        public string Title { get; set; } = string.Empty;
        public DateTime? ReleaseYear { get; set; }
        public List<string> Artists { get; set; } = new();
        public List<string> Genres { get; set; } = new();
        public bool Live { get; set; }
        public int? LengthSecs { get; set; }
        public int Index { get; set; }
    }


}
