using KollectorScum.Api.Data;
using KollectorScum.Api.DTOs;
using KollectorScum.Api.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KollectorScum.Api.Controllers
{
    /// <summary>
    /// Controller for natural language database queries
    /// </summary>
    public class QueryController : BaseApiController
    {
        private readonly IQueryLLMService _queryLLMService;
        private readonly ISqlValidationService _sqlValidationService;
        private readonly KollectorScumDbContext _dbContext;

        public QueryController(
            IQueryLLMService queryLLMService,
            ISqlValidationService sqlValidationService,
            KollectorScumDbContext dbContext,
            ILogger<QueryController> logger) : base(logger)
        {
            _queryLLMService = queryLLMService;
            _sqlValidationService = sqlValidationService;
            _dbContext = dbContext;
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

                // Execute the query
                var results = await ExecuteQueryAsync(sql);

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

        private async Task<List<Dictionary<string, object?>>> ExecuteQueryAsync(string sql)
        {
            var results = new List<Dictionary<string, object?>>();

            using var connection = _dbContext.Database.GetDbConnection();
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandTimeout = 30; // 30 second timeout

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

            _logger.LogInformation("Query returned {Count} results", results.Count);
            return results;
        }
    }
}
