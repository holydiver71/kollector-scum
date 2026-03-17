using KollectorScum.Api.Services;
using Microsoft.Extensions.Logging;
using Moq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace KollectorScum.Tests.Services
{
    /// <summary>
    /// Unit tests for ImageResizerService: aspect-ratio preservation, no-upscale,
    /// and dimension limits.
    /// </summary>
    public class ImageResizerServiceTests
    {
        private readonly ImageResizerService _service;

        public ImageResizerServiceTests()
        {
            var logger = new Mock<ILogger<ImageResizerService>>();
            _service = new ImageResizerService(logger.Object);
        }

        // ─── Constructor ──────────────────────────────────────────────────────

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ImageResizerService(null!));
        }

        // ─── ResizeAsync ──────────────────────────────────────────────────────

        [Fact]
        public async Task ResizeAsync_WithNullStream_ThrowsArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _service.ResizeAsync(null!, 1600, "image/jpeg"));
        }

        [Fact]
        public async Task ResizeAsync_WithNonPositiveMaxDimension_ThrowsArgumentOutOfRangeException()
        {
            using var stream = CreateTestJpeg(100, 100);
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                _service.ResizeAsync(stream, 0, "image/jpeg"));
        }

        [Fact]
        public async Task ResizeAsync_ImageSmallerThanMax_NotUpscaled()
        {
            // 200×200 image; maxDimension = 1600 → should stay 200×200
            using var source = CreateTestJpeg(200, 200);

            using var result = await _service.ResizeAsync(source, 1600, "image/jpeg");

            using var img = Image.Load(result);
            Assert.Equal(200, img.Width);
            Assert.Equal(200, img.Height);
        }

        [Fact]
        public async Task ResizeAsync_ImageLargerThanMax_DownscaledPreservingAspectRatio()
        {
            // 3200×1600 landscape; maxDimension = 1600 → 1600×800
            using var source = CreateTestJpeg(3200, 1600);

            using var result = await _service.ResizeAsync(source, 1600, "image/jpeg");

            using var img = Image.Load(result);
            Assert.Equal(1600, img.Width);
            Assert.Equal(800, img.Height);
        }

        [Fact]
        public async Task ResizeAsync_PortraitImageLargerThanMax_DownscaledOnHeight()
        {
            // 800×3200 portrait; maxDimension = 300 → 75×300
            using var source = CreateTestJpeg(800, 3200);

            using var result = await _service.ResizeAsync(source, 300, "image/jpeg");

            using var img = Image.Load(result);
            Assert.Equal(300, img.Height);
            Assert.True(img.Width <= 300, $"Width {img.Width} should not exceed maxDimension");
        }

        [Fact]
        public async Task ResizeAsync_ReturnsJpegStream_WithPositionAtZero()
        {
            using var source = CreateTestJpeg(100, 100);

            var result = await _service.ResizeAsync(source, 1600, "image/jpeg");

            Assert.Equal(0, result.Position);
            // JPEG magic bytes: 0xFF 0xD8
            var first = result.ReadByte();
            var second = result.ReadByte();
            Assert.Equal(0xFF, first);
            Assert.Equal(0xD8, second);
        }

        // ─── Helpers ──────────────────────────────────────────────────────────

        /// <summary>Creates a minimal in-memory JPEG of the specified size.</summary>
        private static MemoryStream CreateTestJpeg(int width, int height)
        {
            using var image = new Image<Rgba32>(width, height);
            var ms = new MemoryStream();
            image.SaveAsJpeg(ms);
            ms.Position = 0;
            return ms;
        }
    }
}
