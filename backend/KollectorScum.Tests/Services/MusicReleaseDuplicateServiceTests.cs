using System.Text.Json;
using KollectorScum.Api.Data;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using KollectorScum.Api.Repositories;
using KollectorScum.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KollectorScum.Tests.Services
{
    public class MusicReleaseDuplicateServiceTests
    {
        private static readonly Guid UserId = Guid.Parse("12337b39-c346-449c-b269-33b2e820d74f");

        private static KollectorScumDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<KollectorScumDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new KollectorScumDbContext(options);
        }

        private static MusicReleaseDuplicateService CreateService(KollectorScumDbContext context, Guid? actingUserId = null, bool useNullActingUser = false)
        {
            var userContextMock = new Mock<IUserContext>();
            userContextMock.Setup(x => x.GetActingUserId()).Returns(useNullActingUser ? null : actingUserId ?? UserId);

            return new MusicReleaseDuplicateService(
                new Repository<MusicRelease>(context),
                Mock.Of<ILogger<MusicReleaseDuplicateService>>(),
                userContextMock.Object);
        }

        [Fact]
        public async Task CheckForDuplicatesAsync_WithMatchingCatalog_ReturnsCatalogDuplicate()
        {
            using var context = CreateContext();
            context.MusicReleases.AddRange(
                new MusicRelease { Id = 1, UserId = UserId, Title = "Album One", LabelNumber = "cat-001", Artists = "[1]" },
                new MusicRelease { Id = 2, UserId = UserId, Title = "Album Two", LabelNumber = "cat-002", Artists = "[2]" });
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var duplicates = await service.CheckForDuplicatesAsync("Any", "CAT-001", new List<int> { 99 });

            Assert.Single(duplicates);
            Assert.Equal(1, duplicates[0].Id);
        }

        [Fact]
        public async Task CheckForDuplicatesAsync_WithTitleArtistMatch_ReturnsDuplicate()
        {
            using var context = CreateContext();
            context.MusicReleases.AddRange(
                new MusicRelease { Id = 1, UserId = UserId, Title = "Shared Title", Artists = JsonSerializer.Serialize(new List<int> { 3, 4 }) },
                new MusicRelease { Id = 2, UserId = UserId, Title = "Shared Title", Artists = JsonSerializer.Serialize(new List<int> { 8 }) },
                new MusicRelease { Id = 3, UserId = UserId, Title = "Different Title", Artists = JsonSerializer.Serialize(new List<int> { 3 }) });
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var duplicates = await service.CheckForDuplicatesAsync("shared title", null, new List<int> { 4 });

            Assert.Single(duplicates);
            Assert.Equal(1, duplicates[0].Id);
        }

        [Fact]
        public async Task CheckForDuplicatesAsync_WithExcludeReleaseId_ExcludesCurrentRelease()
        {
            using var context = CreateContext();
            context.MusicReleases.AddRange(
                new MusicRelease { Id = 11, UserId = UserId, Title = "Title", LabelNumber = "ABC-1", Artists = "[1]" },
                new MusicRelease { Id = 12, UserId = UserId, Title = "Title", LabelNumber = "ABC-2", Artists = "[1]" });
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var duplicates = await service.CheckForDuplicatesAsync("title", "ABC-1", new List<int> { 1 }, excludeReleaseId: 11);

            Assert.Single(duplicates);
            Assert.Equal(12, duplicates[0].Id);
        }

        [Fact]
        public async Task CheckForDuplicatesAsync_OnlyChecksActingUserReleases()
        {
            using var context = CreateContext();
            var otherUserId = Guid.NewGuid();
            context.MusicReleases.AddRange(
                new MusicRelease { Id = 21, UserId = otherUserId, Title = "Shared", LabelNumber = "DUP", Artists = "[5]" },
                new MusicRelease { Id = 22, UserId = UserId, Title = "Unique", LabelNumber = "UNQ", Artists = "[7]" });
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var duplicates = await service.CheckForDuplicatesAsync("shared", "DUP", new List<int> { 5 });

            Assert.Empty(duplicates);
        }

        [Fact]
        public async Task IsDuplicateAsync_WhenNoActingUser_ReturnsFalse()
        {
            using var context = CreateContext();
            context.MusicReleases.Add(new MusicRelease { Id = 1, UserId = UserId, Title = "Album", LabelNumber = "CAT", Artists = "[1]" });
            await context.SaveChangesAsync();

            var service = CreateService(context, useNullActingUser: true);

            var isDuplicate = await service.IsDuplicateAsync("album", "CAT", new List<int> { 1 });

            Assert.False(isDuplicate);
        }
    }
}
