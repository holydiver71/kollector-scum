namespace KollectorScum.Api.Interfaces
{
    /// <summary>
    /// Service for converting natural language questions to SQL queries using LLM
    /// </summary>
    public interface IQueryLLMService
    {
        /// <summary>
        /// Generates SQL from a natural language question
        /// </summary>
        /// <param name="question">The natural language question</param>
        /// <returns>Generated SQL query</returns>
        Task<string> GenerateSqlFromNaturalLanguageAsync(string question);

        /// <summary>
        /// Formats query results as a natural language response
        /// </summary>
        /// <param name="question">The original question</param>
        /// <param name="results">The query results</param>
        /// <returns>Natural language response</returns>
        Task<string> FormatResultsAsNaturalLanguageAsync(string question, object results);
    }
}
