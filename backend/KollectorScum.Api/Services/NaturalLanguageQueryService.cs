using System.Text.Json;
using KollectorScum.Api.Interfaces;
using OpenAI;
using OpenAI.Chat;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// Settings for LLM configuration
    /// </summary>
    public class LLMSettings
    {
        public string Provider { get; set; } = "OpenAI";
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "gpt-4o";
    }

    /// <summary>
    /// Service for converting natural language questions to SQL queries using OpenAI
    /// </summary>
    public class NaturalLanguageQueryService : IQueryLLMService
    {
        private readonly OpenAIClient _client;
        private readonly string _model;
        private readonly IDatabaseSchemaService _schemaService;
        private readonly ILogger<NaturalLanguageQueryService> _logger;

        public NaturalLanguageQueryService(
            IDatabaseSchemaService schemaService,
            IConfiguration configuration,
            ILogger<NaturalLanguageQueryService> logger)
        {
            _schemaService = schemaService;
            _logger = logger;

            var settings = configuration.GetSection("LLM").Get<LLMSettings>() ?? new LLMSettings();
            
            // Try environment variable first, then config
            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? settings.ApiKey;
            
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("OpenAI API key not configured. Set OPENAI_API_KEY environment variable or LLM:ApiKey in appsettings.");
            }

            _client = new OpenAIClient(apiKey);
            _model = settings.Model;
        }

        /// <inheritdoc />
        public async Task<string> GenerateSqlFromNaturalLanguageAsync(string question)
        {
            _logger.LogInformation("Generating SQL for question: {Question}", question);

            var systemPrompt = BuildSqlGenerationPrompt();
            var userPrompt = $"Generate a PostgreSQL SELECT query for this question: {question}";

            try
            {
                var chatClient = _client.GetChatClient(_model);
                var messages = new List<ChatMessage>
                {
                    new SystemChatMessage(systemPrompt),
                    new UserChatMessage(userPrompt)
                };

                var response = await chatClient.CompleteChatAsync(messages);
                var sql = response.Value.Content[0].Text;

                // Clean up the response - remove markdown code blocks if present
                sql = CleanSqlResponse(sql);

                _logger.LogInformation("Generated SQL: {Sql}", sql);
                return sql;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating SQL from natural language");
                throw new InvalidOperationException("Failed to generate SQL query. Please try rephrasing your question.", ex);
            }
        }

        /// <inheritdoc />
        public async Task<string> FormatResultsAsNaturalLanguageAsync(string question, object results)
        {
            _logger.LogInformation("Formatting results for question: {Question}", question);

            var systemPrompt = @"You are a helpful assistant that summarizes database query results in natural language.
Keep your response concise and friendly. Focus on the key information the user was asking about.
If there are no results, say so clearly. If there are many results, summarize the key findings.
Don't mention SQL or databases - just answer the question naturally.";

            var resultsJson = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
            var userPrompt = $@"Original question: {question}

Query results (JSON):
{resultsJson}

Please provide a natural language summary of these results.";

            try
            {
                var chatClient = _client.GetChatClient(_model);
                var messages = new List<ChatMessage>
                {
                    new SystemChatMessage(systemPrompt),
                    new UserChatMessage(userPrompt)
                };

                var response = await chatClient.CompleteChatAsync(messages);
                var answer = response.Value.Content[0].Text;

                _logger.LogInformation("Generated answer: {Answer}", answer);
                return answer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error formatting results as natural language");
                // Return a generic response if LLM fails
                return "Query completed successfully. See the results below.";
            }
        }

        private string BuildSqlGenerationPrompt()
        {
            var schema = _schemaService.GetSchemaDocumentation();
            var samples = _schemaService.GetSampleQueries();

            return $@"You are a SQL expert that converts natural language questions into PostgreSQL queries.
You MUST follow these rules strictly:

1. Only generate SELECT queries - never INSERT, UPDATE, DELETE, DROP, or any other modifying statements
2. Use double quotes for all table and column names with capital letters (PostgreSQL syntax)
3. Always limit results to 100 rows maximum using LIMIT 100
4. Return ONLY the SQL query, no explanations or markdown formatting
5. If the question cannot be answered with the available schema, return a query that selects a message explaining why

{schema}

{samples}

IMPORTANT RULES:
- Output only valid PostgreSQL SELECT statements
- Use ILIKE for case-insensitive text matching
- Use EXTRACT(YEAR FROM ""ReleaseYear"") for year comparisons
- Always add LIMIT 100 at the end
- Do not include semicolons at the end
- Do not wrap in markdown code blocks";
        }

        private static string CleanSqlResponse(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                return sql;
            }

            var cleaned = sql.Trim();

            // Remove markdown code blocks
            if (cleaned.StartsWith("```sql", StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned[6..];
            }
            else if (cleaned.StartsWith("```"))
            {
                cleaned = cleaned[3..];
            }

            if (cleaned.EndsWith("```"))
            {
                cleaned = cleaned[..^3];
            }

            // Remove any leading/trailing whitespace again
            cleaned = cleaned.Trim();

            // Ensure LIMIT is present
            if (!cleaned.Contains("LIMIT", StringComparison.OrdinalIgnoreCase))
            {
                cleaned += " LIMIT 100";
            }

            return cleaned;
        }
    }
}
