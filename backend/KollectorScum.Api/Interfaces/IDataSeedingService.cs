namespace KollectorScum.Api.Interfaces
{
    /// <summary>
    /// Interface for data seeding service
    /// </summary>
    public interface IDataSeedingService
    {
        /// <summary>
        /// Seeds all lookup table data from JSON files
        /// </summary>
        /// <returns>Task representing the async operation</returns>
        Task SeedLookupDataAsync();

        /// <summary>
        /// Seeds country data from JSON file
        /// </summary>
        /// <returns>Task representing the async operation</returns>
        Task SeedCountriesAsync();

        /// <summary>
        /// Seeds store data from JSON file
        /// </summary>
        /// <returns>Task representing the async operation</returns>
        Task SeedStoresAsync();

        /// <summary>
        /// Seeds format data from JSON file
        /// </summary>
        /// <returns>Task representing the async operation</returns>
        Task SeedFormatsAsync();

        /// <summary>
        /// Seeds genre data from JSON file
        /// </summary>
        /// <returns>Task representing the async operation</returns>
        Task SeedGenresAsync();

        /// <summary>
        /// Seeds label data from JSON file
        /// </summary>
        /// <returns>Task representing the async operation</returns>
        Task SeedLabelsAsync();

        /// <summary>
        /// Seeds artist data from JSON file
        /// </summary>
        /// <returns>Task representing the async operation</returns>
        Task SeedArtistsAsync();

        /// <summary>
        /// Seeds packaging data from JSON file
        /// </summary>
        /// <returns>Task representing the async operation</returns>
        Task SeedPackagingsAsync();

        /// <summary>
        /// Seeds music release data from JSON file
        /// </summary>
        /// <returns>Task representing the async operation</returns>
        Task SeedMusicReleasesAsync();
    }
}
