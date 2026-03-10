using KollectorScum.Api.Data;
using KollectorScum.Api.Interfaces;
using KollectorScum.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace KollectorScum.Api.Repositories
{
    /// <summary>
    /// Repository implementation for MagicLinkToken operations using EF Core.
    /// Automatically sets <c>CreatedAt</c> on creation and provides a cleanup
    /// method to remove expired tokens from the database.
    /// </summary>
    public class MagicLinkTokenRepository : IMagicLinkTokenRepository
    {
        private readonly KollectorScumDbContext _context;

        public MagicLinkTokenRepository(KollectorScumDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public async Task<MagicLinkToken> CreateAsync(MagicLinkToken token)
        {
            token.CreatedAt = DateTime.UtcNow;
            _context.MagicLinkTokens.Add(token);
            await _context.SaveChangesAsync();
            return token;
        }

        /// <inheritdoc />
        public async Task<MagicLinkToken?> FindByTokenAsync(string token)
        {
            return await _context.MagicLinkTokens
                .FirstOrDefaultAsync(t => t.Token == token);
        }

        /// <inheritdoc />
        public async Task<MagicLinkToken> UpdateAsync(MagicLinkToken token)
        {
            _context.MagicLinkTokens.Update(token);
            await _context.SaveChangesAsync();
            return token;
        }

        /// <inheritdoc />
        public async Task DeleteExpiredTokensAsync()
        {
            var expired = await _context.MagicLinkTokens
                .Where(t => t.ExpiresAt < DateTime.UtcNow)
                .ToListAsync();

            if (expired.Count > 0)
            {
                _context.MagicLinkTokens.RemoveRange(expired);
                await _context.SaveChangesAsync();
            }
        }
    }
}
