namespace KollectorScum.Api.Interfaces
{
    /// <summary>
    /// Service for resizing images and generating thumbnails.
    /// </summary>
    public interface IImageResizerService
    {
        /// <summary>
        /// Resizes an image stream so that neither dimension exceeds <paramref name="maxDimension"/>.
        /// The aspect ratio is preserved. If the image is already within the limit the stream is
        /// returned unchanged (re-encoded as JPEG).
        /// </summary>
        /// <param name="inputStream">Source image bytes.</param>
        /// <param name="maxDimension">Maximum allowed width or height in pixels (default 1600).</param>
        /// <returns>JPEG-encoded image stream with dimensions ≤ <paramref name="maxDimension"/>.</returns>
        Task<Stream> ResizeAsync(Stream inputStream, int maxDimension = 1600);

        /// <summary>
        /// Creates a square thumbnail by centre-cropping and scaling the input image
        /// to <paramref name="size"/> × <paramref name="size"/> pixels.
        /// </summary>
        /// <param name="inputStream">Source image bytes (will be read from start).</param>
        /// <param name="size">Target square dimension in pixels (default 300).</param>
        /// <returns>JPEG-encoded thumbnail stream.</returns>
        Task<Stream> GenerateThumbnailAsync(Stream inputStream, int size = 300);
    }
}
