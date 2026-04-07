using KollectorScum.Api.Data;
using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KollectorScum.Api.Controllers
{
    /// <summary>
    /// Controller for natural language database queries.
    /// Requires authentication; all queries are tenant-scoped to the current user.
    /// </summary>
    [Authorize]
    public class QueryController : BaseApiController
    {
        private readonly IQueryLLMService _queryLLMService;
        private readonly ISqlValidationService _sqlValidationService;
        private readonly KollectorScumDbContext _dbContext;
        private readonly IUserContext _userContext;

        public QueryController(
            IQueryLLMService queryLLMService,
            ISqlValidationService sqlValidationService,
            KollectorScumDbContext dbContext,
            IUserContext userContext,
            ILogger<QueryController> logger) : base(logger)
        {
            _queryLLMService = queryLLMService;
            _sqlValidationService = sqlValidationService;
            _dbContext = dbContext;
            _userContext = userContext;
        }

        /// <summary>
        /// Ask a natural language question about the music collection
        /// </summary>
        /// <param name="request">The question to ask</param>
        /// <returns>Query results and natural language answer</returns>
        [HttpPost("ask")]
        [ProducesResponseType(typeof(QueryResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(QueryResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<QueryResponseDto>> Ask([FromBody] NaturalLanguageQueryDto request)
        {
            LogOperation("NaturalLanguageQuery", new { Question = request.Question });

            var userId = _userContext.GetActingUserId();
            if (userId == null)
            {
                return Unauthorized(new QueryResponseDto
                {
                    Question = request.Question,
                    Success = false,
                    Error = "User identity could not be determined."
                });
            }

            try
            {
                // Generate SQL from natural language
                var sql = await _queryLLMService.GenerateSqlFromNaturalLanguageAsync(request.Question);

                // Sanitize and validate the SQL
                sql = _sqlValidationService.Sanitize(sql);
                var validationResult = _sqlValidationService.Validate(sql);

                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("Generated SQL failed validation: {Sql}, Errors: {Errors}", 
                        sql, validationResult.ErrorMessage);
                    
                    return BadRequest(new QueryResponseDto
                    {
                        Question = request.Question,
                        Success = false,
                        Error = "I couldn't generate a safe query for that question. Please try rephrasing your request."
                    });
                }

                // Execute the query, scoped to the current user's tenant
                var results = await ExecuteQueryAsync(sql, userId.Value);

                // Generate natural language answer
                var answer = await _queryLLMService.FormatResultsAsNaturalLanguageAsync(
                    request.Question, 
                    results);

                return Ok(new QueryResponseDto
                {
                    Question = request.Question,
                    Query = sql,
                    Results = results,
                    ResultCount = results.Count,
                    Answer = answer,
                    Success = true
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation during query processing");
                return BadRequest(new QueryResponseDto
                {
                    Question = request.Question,
                    Success = false,
                    Error = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing natural language query");
                return StatusCode(500, new QueryResponseDto
                {
                    Question = request.Question,
                    Success = false,
                    Error = "An error occurred while processing your request. Please try again."
                });
            }
        }

        private async Task<List<Dictionary<string, object?>>> ExecuteQueryAsync(string sql, Guid userId)
        {
            var results = new List<Dictionary<string, object?>>();

            // Wrap the validated SQL in tenant-scoped CTEs so all user-owned tables
            // are pre-filtered to the current user; prevents cross-tenant data leakage.
            var scopedSql = ApplyTenantScoping(sql);

            using var connection = _dbContext.Database.GetDbConnection();
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = scopedSql;
            command.CommandTimeout = 30; // 30 second timeout

            // Parameterised userId prevents injection and enforces tenant isolation
            var userIdParam = command.CreateParameter();
            userIdParam.ParameterName = "userId";
            userIdParam.Value = userId;
            command.Parameters.Add(userIdParam);

            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object?>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var name = reader.GetName(i);
                    var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    
                    // Convert DateTime to ISO string for JSON serialization
                    if (value is DateTime dt)
                    {
                        value = dt.ToString("O");
                    }
                    
                    row[name] = value;
                }
                results.Add(row);
            }

            _logger.LogInformation("Query returned {Count} results for user {UserId}", results.Count, userId);
            return results;
        }

        /// <summary>
        /// Wraps the validated SQL in tenant-scoped CTEs for all user-owned tables.
        /// Each allowed table is shadowed by a CTE that pre-filters rows to the current user,
        /// ensuring the subsequent SELECT cannot access another tenant's data even if the
        /// LLM-generated SQL omits a UserId predicate.
        /// </summary>
        public static string ApplyTenantScoping(string sql)
        {
            // CTEs shadow every user-owned allowed table with a user-filtered view.
            // NowPlayings has no UserId column; it is scoped through its MusicReleaseId FK.
            const string tenantCteBlock =
                """
                WITH "MusicReleases" AS (
                    SELECT * FROM public."MusicReleases" WHERE "UserId" = @userId
                ),
                "Artists" AS (
                    SELECT * FROM public."Artists" WHERE "UserId" = @userId
                ),
                "Labels" AS (
                    SELECT * FROM public."Labels" WHERE "UserId" = @userId
                ),
                "Countries" AS (
                    SELECT * FROM public."Countries" WHERE "UserId" = @userId
                ),
                "Formats" AS (
                    SELECT * FROM public."Formats" WHERE "UserId" = @userId
                ),
                "Genres" AS (
                    SELECT * FROM public."Genres" WHERE "UserId" = @userId
                ),
                "Packagings" AS (
                    SELECT * FROM public."Packagings" WHERE "UserId" = @userId
                ),
                "Stores" AS (
                    SELECT * FROM public."Stores" WHERE "UserId" = @userId
                ),
                "NowPlayings" AS (
                    SELECT np.* FROM public."NowPlayings" np
                    INNER JOIN public."MusicReleases" mr ON np."MusicReleaseId" = mr."Id"
                    WHERE mr."UserId" = @userId
                )
                """;

            var trimmed = sql.TrimStart();

            if (trimmed.StartsWith("WITH", StringComparison.OrdinalIgnoreCase))
            {
                // SQL already has CTEs — inject our scoping CTEs first, then append
                // the original CTE list (skipping the "WITH" keyword).
                var afterWith = trimmed[4..]; // strip "WITH"
                return tenantCteBlock + "," + afterWith;
            }

            return tenantCteBlock + " " + sql;
        }
    }
}
