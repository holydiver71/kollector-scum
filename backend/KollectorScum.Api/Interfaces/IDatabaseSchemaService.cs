namespace KollectorScum.Api.Interfaces
{
    /// <summary>
    /// Service for generating database schema documentation for LLM context
    /// </summary>
    public interface IDatabaseSchemaService
    {
        /// <summary>
        /// Gets the complete database schema documentation as a string
        /// suitable for including in LLM prompts
        /// </summary>
        /// <returns>Database schema documentation</returns>
        string GetSchemaDocumentation();

        /// <summary>
        /// Gets sample query examples for the LLM
        /// </summary>
        /// <returns>Sample SQL queries with descriptions</returns>
        string GetSampleQueries();
    }
}
