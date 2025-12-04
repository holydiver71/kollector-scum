using System.Text.RegularExpressions;
using KollectorScum.Api.Interfaces;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// Service for validating and sanitizing SQL queries to ensure they are safe (read-only)
    /// </summary>
    public partial class SqlValidationService : ISqlValidationService
    {
        // Dangerous SQL patterns that are not allowed
        private static readonly string[] DangerousPatterns = new[]
        {
            @"\bDROP\b",
            @"\bDELETE\b",
            @"\bTRUNCATE\b",
            @"\bUPDATE\b",
            @"\bINSERT\b",
            @"\bALTER\b",
            @"\bCREATE\b",
            @"\bGRANT\b",
            @"\bREVOKE\b",
            @"\bEXEC\b",
            @"\bEXECUTE\b",
            @"\bxp_\w+",
            @"\bsp_\w+",
            @";\s*--",  // SQL injection pattern
            @";\s*(DROP|DELETE|UPDATE|INSERT|ALTER|CREATE)", // Chained dangerous commands
            @"\bINTO\s+OUTFILE\b",
            @"\bLOAD_FILE\b",
            @"\bUNION\s+ALL\s+SELECT\b",  // Potential SQL injection
        };

        // Allowed table names for security
        private static readonly HashSet<string> AllowedTables = new(StringComparer.OrdinalIgnoreCase)
        {
            "MusicReleases",
            "Artists",
            "Labels",
            "Countries",
            "Formats",
            "Genres",
            "Packagings",
            "Stores",
            "NowPlayings"
        };

        /// <inheritdoc />
        public SqlValidationResult Validate(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                return SqlValidationResult.Failure("SQL query cannot be empty");
            }

            var errors = new List<string>();

            // Check if it starts with SELECT (case-insensitive)
            var trimmedSql = sql.Trim();
            if (!trimmedSql.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("Only SELECT queries are allowed");
            }

            // Check for dangerous patterns
            foreach (var pattern in DangerousPatterns)
            {
                if (Regex.IsMatch(sql, pattern, RegexOptions.IgnoreCase))
                {
                    errors.Add($"Query contains forbidden pattern: {pattern.Replace("\\b", "").Replace("\\s*", " ").Replace("\\w+", "*")}");
                }
            }

            // Check for multiple statements (semicolon followed by another statement)
            if (ContainsMultipleStatements(sql))
            {
                errors.Add("Multiple SQL statements are not allowed");
            }

            // Check for comment injection
            if (sql.Contains("--") && !IsCommentInStringLiteral(sql))
            {
                errors.Add("SQL comments are not allowed for security reasons");
            }

            // Validate table names in FROM and JOIN clauses
            var tableErrors = ValidateTableNames(sql);
            errors.AddRange(tableErrors);

            if (errors.Count > 0)
            {
                return SqlValidationResult.Failure(errors);
            }

            return SqlValidationResult.Success();
        }

        /// <inheritdoc />
        public string Sanitize(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                return string.Empty;
            }

            var sanitized = sql.Trim();

            // Remove any trailing semicolons
            sanitized = sanitized.TrimEnd(';');

            // Remove any SQL comments
            sanitized = RemoveSqlComments(sanitized);

            // Limit query length to prevent abuse
            const int maxLength = 2000;
            if (sanitized.Length > maxLength)
            {
                sanitized = sanitized[..maxLength];
            }

            return sanitized;
        }

        private static bool ContainsMultipleStatements(string sql)
        {
            // Look for semicolons not inside string literals that are followed by SQL keywords
            var parts = sql.Split(';');
            if (parts.Length <= 1)
            {
                return false;
            }

            // Check if there's actual SQL after the semicolon
            for (int i = 1; i < parts.Length; i++)
            {
                var part = parts[i].Trim();
                if (!string.IsNullOrEmpty(part) && 
                    (part.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) ||
                     part.StartsWith("DROP", StringComparison.OrdinalIgnoreCase) ||
                     part.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase) ||
                     part.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase) ||
                     part.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsCommentInStringLiteral(string sql)
        {
            // Simple check - this is not perfect but catches obvious cases
            var commentIndex = sql.IndexOf("--", StringComparison.Ordinal);
            if (commentIndex < 0) return false;

            // Count quotes before the comment
            var beforeComment = sql[..commentIndex];
            var singleQuotes = beforeComment.Count(c => c == '\'');
            
            // If odd number of quotes, we're inside a string literal
            return singleQuotes % 2 == 1;
        }

        private static List<string> ValidateTableNames(string sql)
        {
            var errors = new List<string>();
            
            // Extract table names from FROM and JOIN clauses
            // Pattern matches: FROM "TableName" or FROM TableName or JOIN "TableName" or JOIN TableName
            var tablePattern = @"\b(FROM|JOIN)\s+""?(\w+)""?";
            var matches = Regex.Matches(sql, tablePattern, RegexOptions.IgnoreCase);

            foreach (Match match in matches)
            {
                var tableName = match.Groups[2].Value;
                if (!AllowedTables.Contains(tableName))
                {
                    errors.Add($"Table '{tableName}' is not allowed. Allowed tables: {string.Join(", ", AllowedTables)}");
                }
            }

            return errors;
        }

        private static string RemoveSqlComments(string sql)
        {
            // Remove single-line comments
            var noSingleLineComments = Regex.Replace(sql, @"--.*$", "", RegexOptions.Multiline);
            
            // Remove multi-line comments
            var noComments = Regex.Replace(noSingleLineComments, @"/\*.*?\*/", "", RegexOptions.Singleline);
            
            return noComments.Trim();
        }
    }
}
