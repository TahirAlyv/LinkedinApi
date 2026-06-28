using Linkedin.Core.Data;
using Linkedin.Core.Entities;
using Linkedin.DataAccess.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Linkedin.DataAccess.Repositories.Concrete
{
    public class RefreshTokenRepository : Repository<RefreshToken>, IRefreshToken
    {
        public RefreshTokenRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<RefreshToken?> GetByTokenAsync(string token)
        {
            return await _context.RefreshTokens
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Token == token);
        }

        public async Task<List<RefreshToken>> GetExpiredTokensAsync(DateTime utcNow)
        {
            return await _context.RefreshTokens
                .Where(x =>
                    x.ExpiresAt <= utcNow ||
                    x.SessionExpiresAt <= utcNow)
                .ToListAsync();
        }

        public async Task<List<RefreshToken>> GetActiveTokensByFamilyAsync(
            string userId,
            string tokenFamilyId)
        {
            return await _context.RefreshTokens
                .Where(x =>
                    x.UserId == userId &&
                    x.TokenFamilyId == tokenFamilyId &&
                    !x.IsRevoked)
                .ToListAsync();
        }
    }
}