using KollectorScum.Api.Models;
using System.ComponentModel.DataAnnotations;

namespace KollectorScum.Tests.Models
{
    /// <summary>
    /// Unit tests for the MusicRelease entity
    /// </summary>
    public class MusicReleaseTests
    {
        [Fact]
        public void MusicRelease_ValidData_ShouldPassValidation()
        {
            // Arrange
            var musicRelease = new MusicRelease
            {
                Id = 1,
                Title = "Abbey Road",
                ReleaseYear = new DateTime(1969, 9, 26),
                Live = false,
                DateAdded = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            // Act
            var validationResults = ValidateModel(musicRelease);

            // Assert
            Assert.Empty(validationResults);
        }

        [Fact]
        public void MusicRelease_EmptyTitle_ShouldFailValidation()
        {
            // Arrange
            var musicRelease = new MusicRelease
            {
                Id = 1,
                Title = string.Empty,
                DateAdded = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            // Act
            var validationResults = ValidateModel(musicRelease);

            // Assert
            Assert.NotEmpty(validationResults);
            Assert.Contains(validationResults, v => v.MemberNames.Contains("Title"));
        }

        [Fact]
        public void MusicRelease_TitleTooLong_ShouldFailValidation()
        {
            // Arrange
            var musicRelease = new MusicRelease
            {
                Id = 1,
                Title = new string('A', 301), // 301 characters, exceeds 300 limit
                DateAdded = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            // Act
            var validationResults = ValidateModel(musicRelease);

            // Assert
            Assert.NotEmpty(validationResults);
            Assert.Contains(validationResults, v => v.MemberNames.Contains("Title"));
        }

        [Fact]
        public void MusicRelease_DefaultValues_ShouldBeSetCorrectly()
        {
            // Arrange & Act
            var musicRelease = new MusicRelease();

            // Assert
            Assert.False(musicRelease.Live);
            Assert.True(musicRelease.DateAdded != default(DateTime));
            Assert.True(musicRelease.LastModified != default(DateTime));
            Assert.Equal(string.Empty, musicRelease.Title);
        }

        /// <summary>
        /// Helper method to validate model using data annotations
        /// </summary>
        /// <param name="model">The model to validate</param>
        /// <returns>List of validation results</returns>
        private static IList<ValidationResult> ValidateModel(object model)
        {
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(model, null, null);
            Validator.TryValidateObject(model, validationContext, validationResults, true);
            return validationResults;
        }
    }
}
