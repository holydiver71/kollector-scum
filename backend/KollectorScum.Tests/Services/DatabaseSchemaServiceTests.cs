using KollectorScum.Api.Services;

namespace KollectorScum.Tests.Services
{
    public class DatabaseSchemaServiceTests
    {
        private readonly DatabaseSchemaService _service;

        public DatabaseSchemaServiceTests()
        {
            _service = new DatabaseSchemaService();
        }

        [Fact]
        public void GetSchemaDocumentation_ReturnsNonEmptyString()
        {
            // Act
            var result = _service.GetSchemaDocumentation();

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(result));
        }

        [Fact]
        public void GetSchemaDocumentation_ContainsMusicReleasesTable()
        {
            // Act
            var result = _service.GetSchemaDocumentation();

            // Assert
            Assert.Contains("MusicReleases", result);
        }

        [Theory]
        [InlineData("Artists")]
        [InlineData("Labels")]
        [InlineData("Countries")]
        [InlineData("Formats")]
        [InlineData("Genres")]
        [InlineData("Packagings")]
        [InlineData("Stores")]
        [InlineData("NowPlayings")]
        public void GetSchemaDocumentation_ContainsAllTables(string tableName)
        {
            // Act
            var result = _service.GetSchemaDocumentation();

            // Assert
            Assert.Contains(tableName, result);
        }

        [Theory]
        [InlineData("Title")]
        [InlineData("ReleaseYear")]
        [InlineData("LabelId")]
        [InlineData("CountryId")]
        [InlineData("FormatId")]
        [InlineData("PackagingId")]
        public void GetSchemaDocumentation_ContainsKeyColumns(string columnName)
        {
            // Act
            var result = _service.GetSchemaDocumentation();

            // Assert
            Assert.Contains(columnName, result);
        }

        [Fact]
        public void GetSchemaDocumentation_ContainsRelationships()
        {
            // Act
            var result = _service.GetSchemaDocumentation();

            // Assert
            Assert.Contains("RELATIONSHIPS", result);
            Assert.Contains("->", result); // Arrow notation for relationships
        }

        [Fact]
        public void GetSchemaDocumentation_ContainsPostgresNotes()
        {
            // Act
            var result = _service.GetSchemaDocumentation();

            // Assert
            Assert.Contains("PostgreSQL", result);
            Assert.Contains("double quotes", result.ToLower());
        }

        [Fact]
        public void GetSampleQueries_ReturnsNonEmptyString()
        {
            // Act
            var result = _service.GetSampleQueries();

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(result));
        }

        [Fact]
        public void GetSampleQueries_ContainsSelectExamples()
        {
            // Act
            var result = _service.GetSampleQueries();

            // Assert
            Assert.Contains("SELECT", result);
        }

        [Fact]
        public void GetSampleQueries_ContainsJoinExamples()
        {
            // Act
            var result = _service.GetSampleQueries();

            // Assert
            Assert.Contains("JOIN", result);
        }

        [Fact]
        public void GetSampleQueries_ContainsGroupByExamples()
        {
            // Act
            var result = _service.GetSampleQueries();

            // Assert
            Assert.Contains("GROUP BY", result);
        }

        [Fact]
        public void GetSampleQueries_ContainsCountExamples()
        {
            // Act
            var result = _service.GetSampleQueries();

            // Assert
            Assert.Contains("COUNT", result);
        }

        [Fact]
        public void GetSchemaDocumentation_IsCachedAndConsistent()
        {
            // Act
            var result1 = _service.GetSchemaDocumentation();
            var result2 = _service.GetSchemaDocumentation();

            // Assert - should return same reference (cached)
            Assert.Same(result1, result2);
        }

        [Fact]
        public void GetSampleQueries_IsCachedAndConsistent()
        {
            // Act
            var result1 = _service.GetSampleQueries();
            var result2 = _service.GetSampleQueries();

            // Assert - should return same reference (cached)
            Assert.Same(result1, result2);
        }
    }
}
