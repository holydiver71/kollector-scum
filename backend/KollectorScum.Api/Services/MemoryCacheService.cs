using KollectorScum.Api.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using System.Collections.Concurrent;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// In-memory cache service implementation using IMemoryCache.
    /// Supports grouping of cache entries for bulk invalidation.
    /// </summary>
    public class MemoryCacheService : ICacheService
    {
        private readonly IMemoryCache _cache;
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _groupTokens;
        private readonly TimeSpan _defaultExpiry = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Initializes a new instance of the MemoryCacheService.
        /// </summary>
        /// <param name="cache">The underlying memory cache</param>
        public MemoryCacheService(IMemoryCache cache)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _groupTokens = new ConcurrentDictionary<string, CancellationTokenSource>(StringComparer.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public T? Get<T>(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return default;

            return _cache.TryGetValue(key, out T? value) ? value : default;
        }

        /// <inheritdoc />
        public void Set<T>(string key, T value, TimeSpan expiry, string? invalidationGroup = null)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            var options = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(expiry);

            // Register with group token so we can invalidate all group entries at once
            if (!string.IsNullOrWhiteSpace(invalidationGroup))
            {
                var cts = _groupTokens.GetOrAdd(invalidationGroup, _ => new CancellationTokenSource());
                options.AddExpirationToken(new CancellationChangeToken(cts.Token));
            }

            _cache.Set(key, value, options);
        }

        /// <inheritdoc />
        public void Remove(string key)
        {
            if (!string.IsNullOrWhiteSpace(key))
                _cache.Remove(key);
        }

        /// <inheritdoc />
        public void InvalidateGroup(string group)
        {
            if (string.IsNullOrWhiteSpace(group))
                return;

            if (_groupTokens.TryRemove(group, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
            }
        }
    }
}
