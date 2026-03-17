using System.IO;
using KollectorScum.Api.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace KollectorScum.Tests.Services
{
    /// <summary>
    /// Unit tests for <see cref="ImageResizerService"/>.
    /// </summary>
    public class ImageResizerServiceTests
    {
        private readonly ImageResizerService _service = new();

        // ─── Helpers ─────────────────────────────────────────────────────────────────

        /// <summary>Creates an in-memory JPEG stream of the requested dimensions.</summary>
        private static Stream CreateJpegStream(int width, int height)
        {
            using var image = new Image<Rgba32>(width, height);
            var ms = new MemoryStream();
            image.SaveAsJpeg(ms);
            ms.Position = 0;
            return ms;
        }

        // ─── ResizeAsync ─────────────────────────────────────────────────────────────

        [Fact]
        public async Task ResizeAsync_LargeImage_ReducesToMaxDimension()
        {
            using var input = CreateJpegStream(3200, 2400);

            using var result = await _service.ResizeAsync(input, 1600);

            using var output = await Image.LoadAsync(result);
            Assert.True(output.Width <= 1600);
            Assert.True(output.Height <= 1600);
        }

        [Fact]
        public async Task ResizeAsync_SmallImage_DoesNotUpscale()
        {
            using var input = CreateJpegStream(800, 600);

            using var result = await _service.ResizeAsync(input, 1600);

            using var output = await Image.LoadAsync(result);
            Assert.Equal(800, output.Width);
            Assert.Equal(600, output.Height);
        }

        [Fact]
        public async Task ResizeAsync_ReturnsSeekableStream()
        {
            using var input = CreateJpegStream(100, 100);

            using var result = await _service.ResizeAsync(input, 1600);

            Assert.True(result.CanRead);
            Assert.Equal(0, result.Position);
        }

        [Fact]
        public async Task ResizeAsync_NullInput_ThrowsArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _service.ResizeAsync(null!));
        }

        [Fact]
        public async Task ResizeAsync_InvalidDimension_ThrowsArgumentOutOfRangeException()
        {
            using var input = CreateJpegStream(100, 100);
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _service.ResizeAsync(input, 0));
        }

        // ─── GenerateThumbnailAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task GenerateThumbnailAsync_ProducesSquareImage()
        {
            using var input = CreateJpegStream(800, 600);

            using var result = await _service.GenerateThumbnailAsync(input, 300);

            using var output = await Image.LoadAsync(result);
            Assert.Equal(300, output.Width);
            Assert.Equal(300, output.Height);
        }

        [Fact]
        public async Task GenerateThumbnailAsync_ReturnsSeekableStream()
        {
            using var input = CreateJpegStream(400, 400);

            using var result = await _service.GenerateThumbnailAsync(input, 300);

            Assert.True(result.CanRead);
            Assert.Equal(0, result.Position);
        }

        [Fact]
        public async Task GenerateThumbnailAsync_NullInput_ThrowsArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _service.GenerateThumbnailAsync(null!));
        }

        [Fact]
        public async Task GenerateThumbnailAsync_InvalidSize_ThrowsArgumentOutOfRangeException()
        {
            using var input = CreateJpegStream(400, 400);
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _service.GenerateThumbnailAsync(input, -1));
        }
    }
}
