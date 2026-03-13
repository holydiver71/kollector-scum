using KollectorScum.Api.Services;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace KollectorScum.Tests.Services
{
    /// <summary>
    /// Unit tests for MemoryCacheService
    /// </summary>
    public class MemoryCacheServiceTests : IDisposable
    {
        private readonly IMemoryCache _memoryCache;
        private readonly MemoryCacheService _service;

        public MemoryCacheServiceTests()
        {
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            _service = new MemoryCacheService(_memoryCache);
        }

        public void Dispose()
        {
            _memoryCache.Dispose();
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidCache_CreatesInstance()
        {
            // Act
            var service = new MemoryCacheService(_memoryCache);

            // Assert
            Assert.NotNull(service);
        }

        [Fact]
        public void Constructor_WithNullCache_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => new MemoryCacheService(null!));
        }

        #endregion

        #region Get Tests

        [Fact]
        public void Get_WhenKeyNotInCache_ReturnsDefault()
        {
            // Act
            var result = _service.Get<string>("nonexistent-key");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Get_WhenKeyInCache_ReturnsCachedValue()
        {
            // Arrange
            _service.Set("test-key", "test-value", TimeSpan.FromMinutes(5));

            // Act
            var result = _service.Get<string>("test-key");

            // Assert
            Assert.Equal("test-value", result);
        }

        [Fact]
        public void Get_WithNullKey_ReturnsDefault()
        {
            // Act
            var result = _service.Get<string>(null!);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Get_WithEmptyKey_ReturnsDefault()
        {
            // Act
            var result = _service.Get<string>(string.Empty);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Get_AfterExpiry_ReturnsDefault()
        {
            // Arrange
            _service.Set("expiring-key", "value", TimeSpan.FromMilliseconds(1));
            Thread.Sleep(50); // Wait for expiry

            // Act
            var result = _service.Get<string>("expiring-key");

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region Set Tests

        [Fact]
        public void Set_WithValidData_StoresValue()
        {
            // Act
            _service.Set("key", 42, TimeSpan.FromMinutes(5));

            // Assert
            var result = _service.Get<int>("key");
            Assert.Equal(42, result);
        }

        [Fact]
        public void Set_WithNullKey_DoesNotThrow()
        {
            // Act & Assert (should not throw)
            _service.Set(null!, "value", TimeSpan.FromMinutes(5));
        }

        [Fact]
        public void Set_WithEmptyKey_DoesNotThrow()
        {
            // Act & Assert (should not throw)
            _service.Set(string.Empty, "value", TimeSpan.FromMinutes(5));
        }

        [Fact]
        public void Set_WithInvalidationGroup_StoresValueWithGroup()
        {
            // Act
            _service.Set("key1", "value1", TimeSpan.FromMinutes(5), "group1");
            _service.Set("key2", "value2", TimeSpan.FromMinutes(5), "group1");

            // Assert
            Assert.Equal("value1", _service.Get<string>("key1"));
            Assert.Equal("value2", _service.Get<string>("key2"));
        }

        [Fact]
        public void Set_OverwritesExistingValue()
        {
            // Arrange
            _service.Set("key", "original", TimeSpan.FromMinutes(5));

            // Act
            _service.Set("key", "updated", TimeSpan.FromMinutes(5));

            // Assert
            Assert.Equal("updated", _service.Get<string>("key"));
        }

        #endregion

        #region Remove Tests

        [Fact]
        public void Remove_ExistingKey_RemovesValue()
        {
            // Arrange
            _service.Set("key", "value", TimeSpan.FromMinutes(5));

            // Act
            _service.Remove("key");

            // Assert
            Assert.Null(_service.Get<string>("key"));
        }

        [Fact]
        public void Remove_NonExistentKey_DoesNotThrow()
        {
            // Act & Assert (should not throw)
            _service.Remove("nonexistent");
        }

        [Fact]
        public void Remove_WithNullKey_DoesNotThrow()
        {
            // Act & Assert (should not throw)
            _service.Remove(null!);
        }

        [Fact]
        public void Remove_WithEmptyKey_DoesNotThrow()
        {
            // Act & Assert (should not throw)
            _service.Remove(string.Empty);
        }

        #endregion

        #region InvalidateGroup Tests

        [Fact]
        public void InvalidateGroup_RemovesAllGroupEntries()
        {
            // Arrange
            _service.Set("key1", "value1", TimeSpan.FromMinutes(5), "mygroup");
            _service.Set("key2", "value2", TimeSpan.FromMinutes(5), "mygroup");
            _service.Set("other-key", "other-value", TimeSpan.FromMinutes(5), "othergroup");

            // Act
            _service.InvalidateGroup("mygroup");

            // Assert
            Assert.Null(_service.Get<string>("key1"));
            Assert.Null(_service.Get<string>("key2"));
            Assert.Equal("other-value", _service.Get<string>("other-key")); // Other group unaffected
        }

        [Fact]
        public void InvalidateGroup_NonExistentGroup_DoesNotThrow()
        {
            // Act & Assert (should not throw)
            _service.InvalidateGroup("nonexistent-group");
        }

        [Fact]
        public void InvalidateGroup_WithNullGroup_DoesNotThrow()
        {
            // Act & Assert (should not throw)
            _service.InvalidateGroup(null!);
        }

        [Fact]
        public void InvalidateGroup_WithEmptyGroup_DoesNotThrow()
        {
            // Act & Assert (should not throw)
            _service.InvalidateGroup(string.Empty);
        }

        [Fact]
        public void InvalidateGroup_AllowsNewEntriesAfterInvalidation()
        {
            // Arrange
            _service.Set("key", "original", TimeSpan.FromMinutes(5), "group");
            _service.InvalidateGroup("group");

            // Act - add new entry to the same group
            _service.Set("key", "new-value", TimeSpan.FromMinutes(5), "group");

            // Assert
            Assert.Equal("new-value", _service.Get<string>("key"));
        }

        [Fact]
        public void InvalidateGroup_CalledTwice_DoesNotThrow()
        {
            // Arrange
            _service.Set("key", "value", TimeSpan.FromMinutes(5), "group");

            // Act & Assert (should not throw)
            _service.InvalidateGroup("group");
            _service.InvalidateGroup("group"); // Second call on already-invalidated group
        }

        #endregion

        #region Type Safety Tests

        [Fact]
        public void Set_AndGet_WithComplexType_Works()
        {
            // Arrange
            var list = new List<int> { 1, 2, 3 };

            // Act
            _service.Set("list-key", list, TimeSpan.FromMinutes(5));
            var result = _service.Get<List<int>>("list-key");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Equal(new[] { 1, 2, 3 }, result);
        }

        [Fact]
        public void Get_WithWrongType_ReturnsDefault()
        {
            // Arrange
            _service.Set("int-key", 42, TimeSpan.FromMinutes(5));

            // Act - try to get as string
            var result = _service.Get<string>("int-key");

            // Assert
            Assert.Null(result);
        }

        #endregion
    }
}
