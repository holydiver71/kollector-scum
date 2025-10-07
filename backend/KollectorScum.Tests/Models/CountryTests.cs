using KollectorScum.Api.Models;
using System.ComponentModel.DataAnnotations;

namespace KollectorScum.Tests.Models
{
    /// <summary>
    /// Unit tests for the Country entity
    /// </summary>
    public class CountryTests
    {
        [Fact]
        public void Country_ValidData_ShouldPassValidation()
        {
            // Arrange
            var country = new Country
            {
                Id = 1,
                Name = "United States"
            };

            // Act
            var validationResults = ValidateModel(country);

            // Assert
            Assert.Empty(validationResults);
        }

        [Fact]
        public void Country_EmptyName_ShouldFailValidation()
        {
            // Arrange
            var country = new Country
            {
                Id = 1,
                Name = string.Empty
            };

            // Act
            var validationResults = ValidateModel(country);

            // Assert
            Assert.NotEmpty(validationResults);
            Assert.Contains(validationResults, v => v.MemberNames.Contains("Name"));
        }

        [Fact]
        public void Country_NameTooLong_ShouldFailValidation()
        {
            // Arrange
            var country = new Country
            {
                Id = 1,
                Name = new string('A', 101) // 101 characters, exceeds 100 limit
            };

            // Act
            var validationResults = ValidateModel(country);

            // Assert
            Assert.NotEmpty(validationResults);
            Assert.Contains(validationResults, v => v.MemberNames.Contains("Name"));
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
