namespace KollectorScum.Api.Interfaces
{
    /// <summary>
    /// Interface for orchestrating lookup table seeding operations
    /// </summary>
    public interface IDataSeedingOrchestrator
    {
        /// <summary>
        /// Seeds all lookup tables in the correct order
        /// </summary>
        /// <returns>Total number of records seeded across all tables</returns>
        Task<int> SeedAllLookupDataAsync();

        /// <summary>
        /// Clears the database of all music releases
        /// </summary>
        Task ClearDatabaseAsync();

        /// <summary>
        /// Seeds the database with random releases from Discogs
        /// </summary>
        /// <param name="count">Number of releases to seed</param>
        /// <returns>Number of releases successfully seeded</returns>
        Task<int> SeedFromDiscogsAsync(int count);
    }
}
