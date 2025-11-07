namespace KollectorScum.Api.DTOs
{
    /// <summary>
    /// Encapsulates pagination parameters for query operations
    /// </summary>
    public class PaginationParameters
    {
        private const int MaxPageSize = 100;
        private int _pageSize = 20;

        /// <summary>
        /// Current page number (1-based)
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// Number of items per page (max 100)
        /// </summary>
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = (value > MaxPageSize) ? MaxPageSize : value;
        }
    }
}
