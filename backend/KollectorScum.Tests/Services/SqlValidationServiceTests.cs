using KollectorScum.Api.Services;

namespace KollectorScum.Tests.Services
{
    public class SqlValidationServiceTests
    {
        private readonly SqlValidationService _service;

        public SqlValidationServiceTests()
        {
            _service = new SqlValidationService();
        }

        [Fact]
        public void Validate_ValidSelectQuery_ReturnsSuccess()
        {
            // Arrange
            var sql = @"SELECT ""Title"", ""ReleaseYear"" FROM ""MusicReleases"" LIMIT 100";

            // Act
            var result = _service.Validate(sql);

            // Assert
            Assert.True(result.IsValid);
            Assert.Null(result.ErrorMessage);
        }

        [Fact]
        public void Validate_SelectWithJoin_ReturnsSuccess()
        {
            // Arrange
            var sql = @"SELECT mr.""Title"", l.""Name"" 
                        FROM ""MusicReleases"" mr 
                        JOIN ""Labels"" l ON mr.""LabelId"" = l.""Id""
                        LIMIT 100";

            // Act
            var result = _service.Validate(sql);

            // Assert
            Assert.True(result.IsValid);
        }

        [Theory]
        [InlineData("DELETE FROM \"MusicReleases\"")]
        [InlineData("DROP TABLE \"MusicReleases\"")]
        [InlineData("UPDATE \"MusicReleases\" SET \"Title\" = 'Test'")]
        [InlineData("INSERT INTO \"MusicReleases\" (\"Title\") VALUES ('Test')")]
        [InlineData("TRUNCATE TABLE \"MusicReleases\"")]
        [InlineData("ALTER TABLE \"MusicReleases\" ADD COLUMN test TEXT")]
        [InlineData("CREATE TABLE test (id INT)")]
        public void Validate_DangerousStatement_ReturnsFailure(string sql)
        {
            // Act
            var result = _service.Validate(sql);

            // Assert
            Assert.False(result.IsValid);
            Assert.NotNull(result.ErrorMessage);
        }

        [Fact]
        public void Validate_EmptyQuery_ReturnsFailure()
        {
            // Arrange
            var sql = "";

            // Act
            var result = _service.Validate(sql);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("empty", result.ErrorMessage?.ToLower() ?? "");
        }

        [Fact]
        public void Validate_WhitespaceQuery_ReturnsFailure()
        {
            // Arrange
            var sql = "   \t\n  ";

            // Act
            var result = _service.Validate(sql);

            // Assert
            Assert.False(result.IsValid);
        }

        [Fact]
        public void Validate_MultipleStatements_ReturnsFailure()
        {
            // Arrange
            var sql = @"SELECT * FROM ""MusicReleases""; DROP TABLE ""MusicReleases""";

            // Act
            var result = _service.Validate(sql);

            // Assert
            Assert.False(result.IsValid);
        }

        [Fact]
        public void Validate_UnknownTable_ReturnsFailure()
        {
            // Arrange
            var sql = @"SELECT * FROM ""Users""";

            // Act
            var result = _service.Validate(sql);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("not available", result.ErrorMessage ?? "");
        }

        [Theory]
        [InlineData("MusicReleases")]
        [InlineData("Artists")]
        [InlineData("Labels")]
        [InlineData("Countries")]
        [InlineData("Formats")]
        [InlineData("Genres")]
        [InlineData("Packagings")]
        [InlineData("Stores")]
        [InlineData("NowPlayings")]
        public void Validate_AllowedTables_ReturnsSuccess(string tableName)
        {
            // Arrange
            var sql = $@"SELECT * FROM ""{tableName}"" LIMIT 100";

            // Act
            var result = _service.Validate(sql);

            // Assert
            Assert.True(result.IsValid, $"Table {tableName} should be allowed");
        }

        [Fact]
        public void Validate_SqlWithComment_ReturnsFailure()
        {
            // Arrange
            var sql = @"SELECT * FROM ""MusicReleases"" -- comment";

            // Act
            var result = _service.Validate(sql);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("comment", result.ErrorMessage?.ToLower() ?? "");
        }

        [Fact]
        public void Sanitize_RemovesTrailingSemicolon()
        {
            // Arrange
            var sql = @"SELECT * FROM ""MusicReleases"";";

            // Act
            var result = _service.Sanitize(sql);

            // Assert
            Assert.DoesNotContain(";", result);
        }

        [Fact]
        public void Sanitize_TrimsWhitespace()
        {
            // Arrange
            var sql = @"   SELECT * FROM ""MusicReleases""   ";

            // Act
            var result = _service.Sanitize(sql);

            // Assert
            Assert.Equal(@"SELECT * FROM ""MusicReleases""", result);
        }

        [Fact]
        public void Sanitize_LimitsQueryLength()
        {
            // Arrange
            var sql = new string('X', 3000);

            // Act
            var result = _service.Sanitize(sql);

            // Assert
            Assert.True(result.Length <= 2000);
        }

        [Fact]
        public void Sanitize_RemovesSqlComments()
        {
            // Arrange
            var sql = @"SELECT * FROM ""MusicReleases"" -- this is a comment";

            // Act
            var result = _service.Sanitize(sql);

            // Assert
            Assert.DoesNotContain("--", result);
            Assert.DoesNotContain("comment", result);
        }

        [Fact]
        public void Sanitize_RemovesMultiLineComments()
        {
            // Arrange
            var sql = @"SELECT * /* comment */ FROM ""MusicReleases""";

            // Act
            var result = _service.Sanitize(sql);

            // Assert
            Assert.DoesNotContain("/*", result);
            Assert.DoesNotContain("*/", result);
        }

        [Fact]
        public void Sanitize_EmptyInput_ReturnsEmpty()
        {
            // Act
            var result = _service.Sanitize("");

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void Sanitize_NullInput_ReturnsEmpty()
        {
            // Act
            var result = _service.Sanitize(null!);

            // Assert
            Assert.Equal(string.Empty, result);
        }
    }
}
