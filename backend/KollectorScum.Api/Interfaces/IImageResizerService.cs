namespace KollectorScum.Api.Interfaces
{
    /// <summary>
    /// Service for resizing images while preserving aspect ratio and without upscaling.
    /// </summary>
    public interface IImageResizerService
    {
        /// <summary>
        /// Resizes an image so that neither dimension exceeds <paramref name="maxDimension"/>.
        /// The aspect ratio is preserved and images are never upscaled.
        /// </summary>
        /// <param name="source">The input image stream.</param>
        /// <param name="maxDimension">Maximum width or height in pixels.</param>
        /// <param name="contentType">The MIME type of the source image (e.g. "image/jpeg").</param>
        /// <returns>A new stream containing the resized image encoded as JPEG.</returns>
        Task<Stream> ResizeAsync(Stream source, int maxDimension, string contentType);
    }
}
