using KollectorScum.Api.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// Resizes images using SixLabors.ImageSharp while preserving aspect ratio and
    /// without upscaling. The output is always encoded as JPEG.
    /// </summary>
    public class ImageResizerService : IImageResizerService
    {
        private readonly ILogger<ImageResizerService> _logger;

        /// <summary>Initialises a new instance of <see cref="ImageResizerService"/>.</summary>
        /// <param name="logger">Logger instance.</param>
        public ImageResizerService(ILogger<ImageResizerService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<Stream> ResizeAsync(Stream source, int maxDimension, string contentType)
        {
            ArgumentNullException.ThrowIfNull(source);
            if (maxDimension <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxDimension), "maxDimension must be positive.");

            using var image = await Image.LoadAsync(source);

            var originalWidth = image.Width;
            var originalHeight = image.Height;

            // Only downscale – never upscale
            if (originalWidth > maxDimension || originalHeight > maxDimension)
            {
                // Compute scale factor to fit within maxDimension × maxDimension
                var scale = Math.Min(
                    (double)maxDimension / originalWidth,
                    (double)maxDimension / originalHeight);

                var newWidth = (int)Math.Round(originalWidth * scale);
                var newHeight = (int)Math.Round(originalHeight * scale);

                image.Mutate(ctx => ctx.Resize(newWidth, newHeight));

                _logger.LogDebug(
                    "Resized image from {W}×{H} to {NW}×{NH} (maxDimension={Max})",
                    originalWidth, originalHeight, newWidth, newHeight, maxDimension);
            }
            else
            {
                _logger.LogDebug(
                    "Image {W}×{H} is within maxDimension={Max}; no resize needed.",
                    originalWidth, originalHeight, maxDimension);
            }

            var output = new MemoryStream();
            await image.SaveAsJpegAsync(output, new JpegEncoder { Quality = 90 });
            output.Position = 0;
            return output;
        }
    }
}
