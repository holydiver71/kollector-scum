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
    }
}
