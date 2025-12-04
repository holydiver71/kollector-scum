namespace KollectorScum.Api.Interfaces
{
    /// <summary>
    /// Service for validating and sanitizing SQL queries
    /// </summary>
    public interface ISqlValidationService
    {
        /// <summary>
        /// Validates that a SQL query is safe to execute (read-only, no dangerous patterns)
        /// </summary>
        /// <param name="sql">The SQL query to validate</param>
        /// <returns>Validation result with success status and any error messages</returns>
        SqlValidationResult Validate(string sql);

        /// <summary>
        /// Sanitizes a SQL query by removing potentially dangerous content
        /// </summary>
        /// <param name="sql">The SQL query to sanitize</param>
        /// <returns>Sanitized SQL query</returns>
        string Sanitize(string sql);
    }

    /// <summary>
    /// Result of SQL validation
    /// </summary>
    public class SqlValidationResult
    {
        /// <summary>
        /// Whether the SQL is valid and safe to execute
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Error message if validation failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// List of specific validation errors
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// Creates a successful validation result
        /// </summary>
        public static SqlValidationResult Success() => new() { IsValid = true };

        /// <summary>
        /// Creates a failed validation result
        /// </summary>
        public static SqlValidationResult Failure(string error)
        {
            return new()
            {
                IsValid = false,
                ErrorMessage = error,
                Errors = new List<string> { error }
            };
        }

        /// <summary>
        /// Creates a failed validation result with multiple errors
        /// </summary>
        public static SqlValidationResult Failure(List<string> errors)
        {
            return new()
            {
                IsValid = false,
                ErrorMessage = string.Join("; ", errors),
                Errors = errors
            };
        }
    }
}
