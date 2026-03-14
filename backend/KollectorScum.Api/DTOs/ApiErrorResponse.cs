using System.Text.Json.Serialization;

namespace KollectorScum.Api.DTOs
{
    /// <summary>
    /// Unified API error response DTO with standardized error format.
    /// Provides a consistent shape for all error responses returned by the API.
    /// </summary>
    public class ApiErrorResponse
    {
        /// <summary>
        /// Human-readable error message safe for API consumers.
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; init; }

        /// <summary>
        /// Machine-readable error code for programmatic handling (optional).
        /// </summary>
        [JsonPropertyName("errorCode")]
        public string? ErrorCode { get; init; }

        /// <summary>
        /// Internal error details — only populated in Development environments.
        /// </summary>
        [JsonPropertyName("details")]
        public string? Details { get; init; }

        /// <summary>
        /// Initializes a new instance of <see cref="ApiErrorResponse"/>.
        /// </summary>
        /// <param name="message">Human-readable error message.</param>
        /// <param name="errorCode">Optional machine-readable error code.</param>
        /// <param name="details">Optional internal details (only for Development).</param>
        public ApiErrorResponse(string message, string? errorCode = null, string? details = null)
        {
            Message = message;
            ErrorCode = errorCode;
            Details = details;
        }
    }
}
