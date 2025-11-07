namespace KollectorScum.Api.Interfaces
{
    /// <summary>
    /// Generic interface for seeding lookup table data from JSON files
    /// </summary>
    /// <typeparam name="TEntity">The entity type to seed</typeparam>
    /// <typeparam name="TDto">The DTO type used for deserialization</typeparam>
    public interface ILookupSeeder<TEntity, TDto> 
        where TEntity : class 
        where TDto : class
    {
        /// <summary>
        /// Seeds data for the specified entity type from a JSON file
        /// </summary>
        /// <returns>Number of entities seeded, or 0 if data already exists or file not found</returns>
        Task<int> SeedAsync();

        /// <summary>
        /// Gets the name of the lookup table being seeded (for logging)
        /// </summary>
        string TableName { get; }

        /// <summary>
        /// Gets the JSON filename (without path) to read from
        /// </summary>
        string FileName { get; }
    }
}
