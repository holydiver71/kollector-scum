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
            var sql = @"   SELECT * FROM ""MusicReleases"" LIMIT 10   ";

            // Act
            var result = _service.Sanitize(sql);

            // Assert
            Assert.Equal(@"SELECT * FROM ""MusicReleases"" LIMIT 10", result);
        }

        [Fact]
        public void Sanitize_LimitsQueryLength()
        {
            // Arrange - build a query that is long but has a LIMIT so the LIMIT won't be appended
            var sql = @"SELECT * FROM ""MusicReleases"" LIMIT 10 -- " + new string('X', 3000);

            // Act
            var result = _service.Sanitize(sql);

            // Assert
            Assert.True(result.Length <= 2010, $"Result length {result.Length} exceeds expected max");
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

        // ---------- UNION injection (SEC-01 hardening) ----------

        [Fact]
        public void Validate_UnionSelect_ReturnsFailure()
        {
            // A UNION without ALL can call fromless functions such as pg_read_file()
            var sql = @"SELECT ""Title"" FROM ""MusicReleases"" UNION SELECT pg_read_file('/etc/passwd') LIMIT 1";

            var result = _service.Validate(sql);

            Assert.False(result.IsValid);
            Assert.Contains("prohibited", result.ErrorMessage ?? "");
        }

        [Fact]
        public void Validate_UnionAllSelect_ReturnsFailure()
        {
            var sql = @"SELECT ""Title"" FROM ""MusicReleases"" UNION ALL SELECT 'injected' LIMIT 1";

            var result = _service.Validate(sql);

            Assert.False(result.IsValid);
        }

        // ---------- PostgreSQL system function injection (SEC-01 hardening) ----------

        [Theory]
        [InlineData(@"SELECT pg_read_file('/etc/passwd') LIMIT 1")]
        [InlineData(@"SELECT pg_ls_dir('.') LIMIT 1")]
        [InlineData(@"SELECT * FROM pg_catalog.pg_tables LIMIT 1")]
        [InlineData(@"SELECT current_setting('data_directory') LIMIT 1")]
        public void Validate_PostgresSystemFunction_ReturnsFailure(string sql)
        {
            var result = _service.Validate(sql);

            Assert.False(result.IsValid);
        }

        // ---------- Schema enumeration (SEC-01 hardening) ----------

        [Fact]
        public void Validate_InformationSchemaAccess_ReturnsFailure()
        {
            var sql = @"SELECT table_name FROM information_schema.tables LIMIT 10";

            var result = _service.Validate(sql);

            Assert.False(result.IsValid);
        }
    }
}
