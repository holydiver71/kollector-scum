namespace KollectorScum.Api.Interfaces
{
    /// <summary>
    /// Service for downloading cover art from external sources and uploading to storage.
    /// </summary>
    public interface IDiscogsImageService
    {
        /// <summary>
        /// Downloads cover art from the specified URL, uploads it to R2 storage,
        /// and returns the stored filename.
        /// </summary>
        /// <param name="imageUrl">The URL of the image to download.</param>
        /// <param name="artist">Artist name used to build the filename.</param>
        /// <param name="title">Release title used to build the filename.</param>
        /// <param name="year">Release year used to build the filename.</param>
        /// <param name="userId">The user who owns this release.</param>
        /// <returns>The stored filename, or null if the download/upload failed.</returns>
        Task<string?> DownloadAndStoreCoverArtAsync(string imageUrl, string artist, string title, string? year, Guid userId);

        /// <summary>
        /// Sanitizes a filename by replacing invalid characters and enforcing a maximum length.
        /// </summary>
        /// <param name="filename">The raw filename to sanitize.</param>
        /// <returns>A sanitized filename safe for storage.</returns>
        string SanitizeFilename(string filename);
    }
}
