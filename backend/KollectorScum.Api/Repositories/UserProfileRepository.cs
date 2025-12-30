using KollectorScum.Api.Data;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace KollectorScum.Api.Repositories
{
    /// <summary>
    /// Repository implementation for UserProfile operations
    /// </summary>
    public class UserProfileRepository : IUserProfileRepository
    {
        private readonly KollectorScumDbContext _context;

        public UserProfileRepository(KollectorScumDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public async Task<UserProfile?> GetByUserIdAsync(Guid userId)
        {
            return await _context.UserProfiles
                .Include(up => up.SelectedKollection)
                .FirstOrDefaultAsync(up => up.UserId == userId);
        }

        /// <inheritdoc />
        public async Task<UserProfile> CreateAsync(UserProfile profile)
        {
            _context.UserProfiles.Add(profile);
            await _context.SaveChangesAsync();
            return profile;
        }

        /// <inheritdoc />
        public async Task<UserProfile> UpdateAsync(UserProfile profile)
        {
            _context.UserProfiles.Update(profile);
            await _context.SaveChangesAsync();
            return profile;
        }

        /// <inheritdoc />
        public async Task<bool> KollectionExistsAsync(int kollectionId)
        {
            return await _context.Kollections.AnyAsync(k => k.Id == kollectionId);
        }

        /// <inheritdoc />
        public async Task<int> GetUserMusicReleaseCountAsync(Guid userId)
        {
            return await _context.MusicReleases.CountAsync(mr => mr.UserId == userId);
        }

        /// <inheritdoc />
        public async Task<int> DeleteAllUserMusicReleasesAsync(Guid userId)
        {
            var releases = await _context.MusicReleases
                .Where(mr => mr.UserId == userId)
                .ToListAsync();

            var count = releases.Count;
            _context.MusicReleases.RemoveRange(releases);
            await _context.SaveChangesAsync();

            return count;
        }
    }
}
