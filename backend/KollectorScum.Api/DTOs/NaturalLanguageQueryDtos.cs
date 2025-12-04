using System.ComponentModel.DataAnnotations;

namespace KollectorScum.Api.DTOs
{
    /// <summary>
    /// DTO for natural language query request
    /// </summary>
    public class NaturalLanguageQueryDto
    {
        /// <summary>
        /// The natural language question to ask about the collection
        /// </summary>
        [Required]
        [StringLength(1000, MinimumLength = 3)]
        public string Question { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for query response
    /// </summary>
    public class QueryResponseDto
    {
        /// <summary>
        /// The original question
        /// </summary>
        public string Question { get; set; } = string.Empty;

        /// <summary>
        /// The generated SQL query (for debugging/transparency)
        /// </summary>
        public string? Query { get; set; }

        /// <summary>
        /// The query results as a list of dynamic objects
        /// </summary>
        public List<Dictionary<string, object?>>? Results { get; set; }

        /// <summary>
        /// The number of results returned
        /// </summary>
        public int ResultCount { get; set; }

        /// <summary>
        /// Natural language answer summarizing the results
        /// </summary>
        public string Answer { get; set; } = string.Empty;

        /// <summary>
        /// Whether the query was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if the query failed
        /// </summary>
        public string? Error { get; set; }
    }
}
