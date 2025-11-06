namespace KollectorScum.Api.Interfaces
{
    /// <summary>
    /// Generic interface for reading and deserializing JSON files
    /// </summary>
    public interface IJsonFileReader
    {
        /// <summary>
        /// Reads and deserializes a JSON file
        /// </summary>
        /// <typeparam name="T">Type to deserialize to</typeparam>
        /// <param name="filePath">Full path to the JSON file</param>
        /// <returns>Deserialized object or null if file doesn't exist or deserialization fails</returns>
        Task<T?> ReadJsonFileAsync<T>(string filePath) where T : class;

        /// <summary>
        /// Checks if a file exists
        /// </summary>
        /// <param name="filePath">Full path to check</param>
        /// <returns>True if file exists</returns>
        bool FileExists(string filePath);

        /// <summary>
        /// Gets the count of items in a JSON array file
        /// </summary>
        /// <typeparam name="T">Type of items in the array</typeparam>
        /// <param name="filePath">Full path to the JSON file</param>
        /// <returns>Count of items or 0 if file doesn't exist</returns>
        Task<int> GetJsonArrayCountAsync<T>(string filePath) where T : class;
    }
}
