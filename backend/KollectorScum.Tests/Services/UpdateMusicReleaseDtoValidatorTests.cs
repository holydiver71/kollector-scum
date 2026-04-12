using FluentValidation.TestHelper;
using KollectorScum.Api.DTOs;
using KollectorScum.Api.Validators;
using Xunit;

namespace KollectorScum.Tests.Services
{
    /// <summary>
    /// Unit tests for <see cref="UpdateMusicReleaseDtoValidator"/>, specifically
    /// the image-field rules that were updated to accept full HTTPS URLs as well
    /// as bare filenames (to match <c>CreateMusicReleaseDtoValidator</c>).
    /// </summary>
    public class UpdateMusicReleaseDtoValidatorTests
    {
        private readonly UpdateMusicReleaseDtoValidator _validator = new();

        /// <summary>
        /// Builds a minimal valid DTO so all required fields pass validation and
        /// only the field under test may trigger an error.
        /// </summary>
        private static UpdateMusicReleaseDto Valid(MusicReleaseImageDto? images = null) =>
            new() { Title = "Test Album", Images = images };

        // ── CoverFront ────────────────────────────────────────────────────────

        [Fact]
        public void CoverFront_BareFilename_IsValid()
        {
            var dto = Valid(new MusicReleaseImageDto { CoverFront = "stormwitch-tales-of-terror-1985.jpg" });
            var result = _validator.TestValidate(dto);
            result.ShouldNotHaveValidationErrorFor(x => x.Images!.CoverFront);
        }

        [Fact]
        public void CoverFront_HttpsUrl_IsValid()
        {
            var dto = Valid(new MusicReleaseImageDto
            {
                CoverFront = "https://kollector-images-staging.workers.dev/6419cd5f/stormwitch.jpg"
            });
            var result = _validator.TestValidate(dto);
            result.ShouldNotHaveValidationErrorFor(x => x.Images!.CoverFront);
        }

        [Fact]
        public void CoverFront_RelativePath_IsValid()
        {
            var dto = Valid(new MusicReleaseImageDto { CoverFront = "/cover-art/6419cd5f/file.jpg" });
            var result = _validator.TestValidate(dto);
            result.ShouldNotHaveValidationErrorFor(x => x.Images!.CoverFront);
        }

        [Fact]
        public void CoverFront_FtpUrl_IsInvalid()
        {
            var dto = Valid(new MusicReleaseImageDto { CoverFront = "ftp://evil.example.com/file.jpg" });
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.Images!.CoverFront);
        }

        [Fact]
        public void CoverFront_Null_IsValid()
        {
            var dto = Valid(new MusicReleaseImageDto { CoverFront = null });
            var result = _validator.TestValidate(dto);
            result.ShouldNotHaveValidationErrorFor(x => x.Images!.CoverFront);
        }

        // ── Thumbnail ─────────────────────────────────────────────────────────

        [Fact]
        public void Thumbnail_BareFilename_IsValid()
        {
            var dto = Valid(new MusicReleaseImageDto { Thumbnail = "thumb-stormwitch.jpg" });
            var result = _validator.TestValidate(dto);
            result.ShouldNotHaveValidationErrorFor(x => x.Images!.Thumbnail);
        }

        [Fact]
        public void Thumbnail_HttpsUrl_IsValid()
        {
            var dto = Valid(new MusicReleaseImageDto
            {
                Thumbnail = "https://kollector-images-staging.workers.dev/6419cd5f/thumb-stormwitch.jpg"
            });
            var result = _validator.TestValidate(dto);
            result.ShouldNotHaveValidationErrorFor(x => x.Images!.Thumbnail);
        }

        [Fact]
        public void Thumbnail_FtpUrl_IsInvalid()
        {
            var dto = Valid(new MusicReleaseImageDto { Thumbnail = "ftp://evil.example.com/thumb.jpg" });
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.Images!.Thumbnail);
        }

        // ── CoverBack ─────────────────────────────────────────────────────────

        [Fact]
        public void CoverBack_HttpsUrl_IsValid()
        {
            var dto = Valid(new MusicReleaseImageDto
            {
                CoverBack = "https://kollector-images-staging.workers.dev/6419cd5f/back.jpg"
            });
            var result = _validator.TestValidate(dto);
            result.ShouldNotHaveValidationErrorFor(x => x.Images!.CoverBack);
        }
    }
}
