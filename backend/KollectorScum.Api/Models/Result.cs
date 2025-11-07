namespace KollectorScum.Api.Models
{
    /// <summary>
    /// Represents the result of an operation that can succeed or fail
    /// Eliminates the need for throwing exceptions for business logic errors
    /// </summary>
    /// <typeparam name="T">The type of the result value on success</typeparam>
    public class Result<T>
    {
        /// <summary>
        /// Indicates whether the operation succeeded
        /// </summary>
        public bool IsSuccess { get; private set; }

        /// <summary>
        /// Indicates whether the operation failed
        /// </summary>
        public bool IsFailure => !IsSuccess;

        /// <summary>
        /// The value returned on success (null on failure)
        /// </summary>
        public T? Value { get; private set; }

        /// <summary>
        /// The error message on failure (null on success)
        /// </summary>
        public string? ErrorMessage { get; private set; }

        /// <summary>
        /// The type of error that occurred (only set on failure)
        /// </summary>
        public ErrorType? ErrorType { get; private set; }

        /// <summary>
        /// Private constructor to enforce factory method usage
        /// </summary>
        private Result(bool isSuccess, T? value, string? errorMessage, ErrorType? errorType)
        {
            IsSuccess = isSuccess;
            Value = value;
            ErrorMessage = errorMessage;
            ErrorType = errorType;
        }

        /// <summary>
        /// Creates a successful result with a value
        /// </summary>
        /// <param name="value">The success value</param>
        /// <returns>A successful Result</returns>
        public static Result<T> Success(T value)
        {
            return new Result<T>(true, value, null, null);
        }

        /// <summary>
        /// Creates a failed result with an error message and type
        /// </summary>
        /// <param name="errorMessage">Description of the error</param>
        /// <param name="errorType">Type of error that occurred</param>
        /// <returns>A failed Result</returns>
        public static Result<T> Failure(string errorMessage, ErrorType errorType)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
                throw new ArgumentException("Error message cannot be null or empty", nameof(errorMessage));

            return new Result<T>(false, default, errorMessage, errorType);
        }

        /// <summary>
        /// Creates a NotFound failure result
        /// </summary>
        /// <param name="entityName">Name of the entity that was not found</param>
        /// <param name="id">ID of the entity that was not found</param>
        /// <returns>A failed Result with NotFound error type</returns>
        public static Result<T> NotFound(string entityName, object id)
        {
            return Failure($"{entityName} with ID {id} was not found", Models.ErrorType.NotFound);
        }

        /// <summary>
        /// Creates a ValidationError failure result
        /// </summary>
        /// <param name="message">Validation error message</param>
        /// <returns>A failed Result with ValidationError error type</returns>
        public static Result<T> ValidationError(string message)
        {
            return Failure(message, Models.ErrorType.ValidationError);
        }

        /// <summary>
        /// Creates a DuplicateError failure result
        /// </summary>
        /// <param name="message">Duplicate error message</param>
        /// <returns>A failed Result with DuplicateError error type</returns>
        public static Result<T> DuplicateError(string message)
        {
            return Failure(message, Models.ErrorType.DuplicateError);
        }
    }
}
