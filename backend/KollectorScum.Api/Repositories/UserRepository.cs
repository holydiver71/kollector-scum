using KollectorScum.Api.Data;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace KollectorScum.Api.Repositories
{
    /// <summary>
    /// Repository implementation for ApplicationUser operations
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly KollectorScumDbContext _context;

        public UserRepository(KollectorScumDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public async Task<ApplicationUser?> FindByGoogleSubAsync(string googleSub)
        {
            return await _context.ApplicationUsers
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.GoogleSub == googleSub);
        }

        /// <inheritdoc />
        public async Task<ApplicationUser?> FindByEmailAsync(string email)
        {
            return await _context.ApplicationUsers
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        /// <inheritdoc />
        public async Task<ApplicationUser?> FindByIdAsync(Guid userId)
        {
            return await _context.ApplicationUsers
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        /// <inheritdoc />
        public async Task<ApplicationUser> CreateAsync(ApplicationUser user)
        {
            user.CreatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            _context.ApplicationUsers.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        /// <inheritdoc />
        public async Task<ApplicationUser> UpdateAsync(ApplicationUser user)
        {
            user.UpdatedAt = DateTime.UtcNow;
            _context.ApplicationUsers.Update(user);
            await _context.SaveChangesAsync();
            return user;
        }

        /// <inheritdoc />
        public async Task<List<ApplicationUser>> GetAllAsync()
        {
            return await _context.ApplicationUsers
                .OrderBy(u => u.Email)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<bool> DeleteAsync(Guid userId)
        {
            var user = await _context.ApplicationUsers.FindAsync(userId);
            if (user == null)
            {
                return false;
            }

            _context.ApplicationUsers.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
