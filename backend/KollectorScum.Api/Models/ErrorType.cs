namespace KollectorScum.Api.Models
{
    /// <summary>
    /// Defines common error types for consistent error handling
    /// </summary>
    public enum ErrorType
    {
        /// <summary>
        /// The requested resource was not found
        /// </summary>
        NotFound,

        /// <summary>
        /// Validation failed for the input data
        /// </summary>
        ValidationError,

        /// <summary>
        /// A duplicate entity already exists
        /// </summary>
        DuplicateError,

        /// <summary>
        /// An external API call failed
        /// </summary>
        ExternalApiError,

        /// <summary>
        /// A database operation failed
        /// </summary>
        DatabaseError,

        /// <summary>
        /// User is not authorized to perform the action
        /// </summary>
        AuthorizationError,

        /// <summary>
        /// An unexpected internal error occurred
        /// </summary>
        InternalError
    }
}
