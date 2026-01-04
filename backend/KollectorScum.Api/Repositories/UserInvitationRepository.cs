using KollectorScum.Api.Data;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace KollectorScum.Api.Repositories
{
    /// <summary>
    /// Repository implementation for user invitation operations
    /// </summary>
    public class UserInvitationRepository : IUserInvitationRepository
    {
        private readonly KollectorScumDbContext _context;

        public UserInvitationRepository(KollectorScumDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public async Task<UserInvitation?> FindByIdAsync(int id)
        {
            return await _context.UserInvitations.FindAsync(id);
        }

        /// <inheritdoc />
        public async Task<UserInvitation?> FindByEmailAsync(string email)
        {
            return await _context.UserInvitations
                .FirstOrDefaultAsync(ui => ui.Email.ToLower() == email.ToLower());
        }

        /// <inheritdoc />
        public async Task<UserInvitation> CreateAsync(UserInvitation invitation)
        {
            invitation.CreatedAt = DateTime.UtcNow;
            _context.UserInvitations.Add(invitation);
            await _context.SaveChangesAsync();
            return invitation;
        }

        /// <inheritdoc />
        public async Task<bool> DeleteAsync(int id)
        {
            var invitation = await _context.UserInvitations.FindAsync(id);
            if (invitation == null)
            {
                return false;
            }

            _context.UserInvitations.Remove(invitation);
            await _context.SaveChangesAsync();
            return true;
        }

        /// <inheritdoc />
        public async Task<List<UserInvitation>> GetAllAsync()
        {
            return await _context.UserInvitations
                .OrderByDescending(ui => ui.CreatedAt)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<UserInvitation> UpdateAsync(UserInvitation invitation)
        {
            _context.UserInvitations.Update(invitation);
            await _context.SaveChangesAsync();
            return invitation;
        }

        /// <inheritdoc />
        public async Task<bool> IsEmailInvitedAsync(string email)
        {
            return await _context.UserInvitations
                .AnyAsync(ui => ui.Email.ToLower() == email.ToLower());
        }
    }
}
