using KollectorScum.Api.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// Resizes images using the SixLabors.ImageSharp library.
    /// Outputs JPEG regardless of the source format.
    /// </summary>
    public class ImageResizerService : IImageResizerService
    {
        private const int JpegQuality = 85;

        /// <inheritdoc />
        public async Task<Stream> ResizeAsync(Stream inputStream, int maxDimension = 1600)
        {
            if (inputStream == null) throw new ArgumentNullException(nameof(inputStream));
            if (maxDimension <= 0) throw new ArgumentOutOfRangeException(nameof(maxDimension));

            using var image = await Image.LoadAsync(inputStream);

            if (image.Width > maxDimension || image.Height > maxDimension)
            {
                image.Mutate(ctx => ctx.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(maxDimension, maxDimension),
                    Sampler = KnownResamplers.Lanczos3,
                }));
            }

            var output = new MemoryStream();
            await image.SaveAsync(output, new JpegEncoder { Quality = JpegQuality });
            output.Position = 0;
            return output;
        }

        /// <inheritdoc />
        public async Task<Stream> GenerateThumbnailAsync(Stream inputStream, int size = 300)
        {
            if (inputStream == null) throw new ArgumentNullException(nameof(inputStream));
            if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));

            // Reset position so the same stream can be reused after a prior ResizeAsync call.
            if (inputStream.CanSeek)
                inputStream.Position = 0;

            using var image = await Image.LoadAsync(inputStream);

            image.Mutate(ctx => ctx.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Crop,
                Size = new Size(size, size),
                Sampler = KnownResamplers.Lanczos3,
                Position = AnchorPositionMode.Center,
            }));

            var output = new MemoryStream();
            await image.SaveAsync(output, new JpegEncoder { Quality = JpegQuality });
            output.Position = 0;
            return output;
        }
    }
}
