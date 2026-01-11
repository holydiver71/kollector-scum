namespace KollectorScum.Api.Interfaces
{
    /// <summary>
    /// Interface for storage service operations (file upload, deletion, and URL retrieval)
    /// </summary>
    public interface IStorageService
    {
        /// <summary>
        /// Uploads a file to storage in a user-specific directory
        /// </summary>
        /// <param name="bucketName">The bucket or directory name (e.g., "cover-art")</param>
        /// <param name="userId">The user ID to organize files by user</param>
        /// <param name="fileName">The original filename</param>
        /// <param name="fileStream">The file content stream</param>
        /// <param name="contentType">The MIME type of the file</param>
        /// <returns>The public URL or path to access the uploaded file</returns>
        Task<string> UploadFileAsync(string bucketName, string userId, string fileName, Stream fileStream, string contentType);

        /// <summary>
        /// Deletes a file from storage
        /// </summary>
        /// <param name="bucketName">The bucket or directory name</param>
        /// <param name="userId">The user ID who owns the file</param>
        /// <param name="fileName">The filename to delete</param>
        Task DeleteFileAsync(string bucketName, string userId, string fileName);

        /// <summary>
        /// Gets the public URL for accessing a file
        /// </summary>
        /// <param name="bucketName">The bucket or directory name</param>
        /// <param name="userId">The user ID who owns the file</param>
        /// <param name="fileName">The filename</param>
        /// <returns>The public URL or path to access the file</returns>
        string GetPublicUrl(string bucketName, string userId, string fileName);
    }
}
