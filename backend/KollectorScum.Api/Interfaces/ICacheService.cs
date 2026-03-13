namespace KollectorScum.Api.Interfaces
{
    /// <summary>
    /// Service for caching data to improve performance for frequently-read, rarely-changed data.
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// Gets a cached value by key.
        /// </summary>
        /// <typeparam name="T">The type of the cached value</typeparam>
        /// <param name="key">The cache key</param>
        /// <returns>The cached value, or default if not found</returns>
        T? Get<T>(string key);

        /// <summary>
        /// Sets a value in the cache with an expiry time.
        /// </summary>
        /// <typeparam name="T">The type of the value to cache</typeparam>
        /// <param name="key">The cache key</param>
        /// <param name="value">The value to cache</param>
        /// <param name="expiry">The cache duration</param>
        /// <param name="invalidationGroup">Optional group key for bulk invalidation</param>
        void Set<T>(string key, T value, TimeSpan expiry, string? invalidationGroup = null);

        /// <summary>
        /// Removes a specific cache entry.
        /// </summary>
        /// <param name="key">The cache key to remove</param>
        void Remove(string key);

        /// <summary>
        /// Invalidates all cache entries belonging to a group.
        /// </summary>
        /// <param name="group">The group key</param>
        void InvalidateGroup(string group);
    }
}
