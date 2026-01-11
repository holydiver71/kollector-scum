using KollectorScum.Api.Interfaces;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// Local file system implementation of IStorageService
    /// Stores files in wwwroot/{bucketName}/{userId}/{filename} structure for multi-tenant isolation
    /// </summary>
    public class LocalFileSystemStorageService : IStorageService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<LocalFileSystemStorageService> _logger;
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        private const long MaxFileSize = 5 * 1024 * 1024; // 5MB

        public LocalFileSystemStorageService(
            IWebHostEnvironment environment,
            ILogger<LocalFileSystemStorageService> logger)
        {
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Uploads a file to the local file system in a user-specific directory
        /// </summary>
        public async Task<string> UploadFileAsync(string bucketName, string userId, string fileName, Stream fileStream, string contentType)
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(bucketName))
                    throw new ArgumentException("Bucket name cannot be empty", nameof(bucketName));
                if (string.IsNullOrWhiteSpace(userId))
                    throw new ArgumentException("User ID cannot be empty", nameof(userId));
                if (string.IsNullOrWhiteSpace(fileName))
                    throw new ArgumentException("File name cannot be empty", nameof(fileName));
                if (fileStream == null)
                    throw new ArgumentNullException(nameof(fileStream));

                if (!fileStream.CanRead)
                    throw new ArgumentException("File stream must be readable", nameof(fileStream));

                // Empty stream is invalid
                if (fileStream.Length == 0)
                    throw new ArgumentException("File stream is empty", nameof(fileStream));

                // Sanitize filename (security: prevent directory traversal)
                var sanitizedFileName = Path.GetFileName(fileName);
                if (string.IsNullOrWhiteSpace(sanitizedFileName))
                    throw new ArgumentException("Invalid file name after sanitization", nameof(fileName));

                // Determine extension. If content type indicates an image, normalize to .jpg
                var extension = Path.GetExtension(sanitizedFileName).ToLowerInvariant();
                if (!string.IsNullOrWhiteSpace(contentType) && contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                {
                    extension = ".jpg"; // normalize stored images to .jpg for consistency
                }

                // If extension still missing or not allowed, reject
                if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
                {
                    throw new ArgumentException(
                        $"File extension '{extension}' is not allowed. Allowed extensions: {string.Join(", ", AllowedExtensions)}", nameof(fileName));
                }

                // Check file size
                if (fileStream.Length > MaxFileSize)
                {
                    throw new ArgumentException(
                        $"File size {fileStream.Length} bytes exceeds maximum allowed size of {MaxFileSize} bytes", nameof(fileStream));
                }

                // Create unique filename to avoid collisions
                var uniqueFileName = $"{Guid.NewGuid()}{extension}";

                // Build directory path: wwwroot/{bucketName}/{userId}
                var userDirectory = Path.Combine(_environment.WebRootPath, bucketName, userId);
                
                // Ensure directory exists
                Directory.CreateDirectory(userDirectory);

                // Build full file path
                var filePath = Path.Combine(userDirectory, uniqueFileName);

                // Write file to disk
                using (var fileStreamOutput = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    await fileStream.CopyToAsync(fileStreamOutput);
                }

                _logger.LogInformation(
                    "File uploaded successfully: {FileName} -> {FilePath} (User: {UserId})", 
                    sanitizedFileName, 
                    uniqueFileName, 
                    userId);

                // Return the public URL path
                return GetPublicUrl(bucketName, userId, uniqueFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload file {FileName} for user {UserId}", fileName, userId);
                throw;
            }
        }

        /// <summary>
        /// Deletes a file from the local file system
        /// </summary>
        public async Task DeleteFileAsync(string bucketName, string userId, string fileName)
        {
            await Task.Run(() =>
            {
                try
                {
                    // Sanitize filename
                    var sanitizedFileName = Path.GetFileName(fileName);
                    if (string.IsNullOrWhiteSpace(sanitizedFileName))
                    {
                        _logger.LogWarning("Invalid file name for deletion: {FileName}", fileName);
                        return;
                    }

                    // Build full file path
                    var filePath = Path.Combine(_environment.WebRootPath, bucketName, userId, sanitizedFileName);

                    // Check if file exists
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        _logger.LogInformation(
                            "File deleted successfully: {FilePath} (User: {UserId})", 
                            filePath, 
                            userId);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "File not found for deletion: {FilePath} (User: {UserId})", 
                            filePath, 
                            userId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete file {FileName} for user {UserId}", fileName, userId);
                    throw;
                }
            });
        }

        /// <summary>
        /// Gets the public URL for a file
        /// </summary>
        public string GetPublicUrl(string bucketName, string userId, string fileName)
        {
            // Sanitize filename
            var sanitizedFileName = Path.GetFileName(fileName);
            
            // Return URL in format: /{bucketName}/{userId}/{fileName}
            return $"/{bucketName}/{userId}/{sanitizedFileName}";
        }
    }
}
