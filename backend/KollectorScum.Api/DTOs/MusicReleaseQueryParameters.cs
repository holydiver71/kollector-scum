namespace KollectorScum.Api.DTOs
{
    /// <summary>
    /// Encapsulates all query parameters for music release searches
    /// Replaces 10+ individual method parameters with a single object
    /// </summary>
    public class MusicReleaseQueryParameters
    {
        /// <summary>
        /// Search term for title/artist/label search
        /// </summary>
        public string? Search { get; set; }

        /// <summary>
        /// Filter by artist ID
        /// </summary>
        public int? ArtistId { get; set; }

        /// <summary>
        /// Filter by genre ID
        /// </summary>
        public int? GenreId { get; set; }

        /// <summary>
        /// Filter by kollection ID (applies genre filter based on kollection)
        /// </summary>
        public int? KollectionId { get; set; }

        /// <summary>
        /// Filter by label ID
        /// </summary>
        public int? LabelId { get; set; }

        /// <summary>
        /// Filter by country ID
        /// </summary>
        public int? CountryId { get; set; }

        /// <summary>
        /// Filter by format ID (CD, Vinyl, etc.)
        /// </summary>
        public int? FormatId { get; set; }

        /// <summary>
        /// Filter by live recording status
        /// </summary>
        public bool? Live { get; set; }

        /// <summary>
        /// Filter by minimum release year
        /// </summary>
        public int? YearFrom { get; set; }

        /// <summary>
        /// Filter by maximum release year
        /// </summary>
        public int? YearTo { get; set; }

        /// <summary>
        /// Sort field (Title, Artist, DateAdded)
        /// </summary>
        public string? SortBy { get; set; }

        /// <summary>
        /// Sort direction (asc or desc)
        /// </summary>
        public string? SortOrder { get; set; }

        /// <summary>
        /// Pagination parameters (page number and size)
        /// </summary>
        public PaginationParameters Pagination { get; set; } = new();
    }
}
