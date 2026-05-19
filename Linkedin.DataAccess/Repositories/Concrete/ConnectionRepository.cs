using Linkedin.Core.Data;
using Linkedin.Core.Entities;
using Linkedin.DataAccess.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.DataAccess.Repositories.Concrete
{
    public class ConnectionRepository : Repository<Connection>, IConnectionRepository
    {
        public ConnectionRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<bool> AreConnectedAsync(string currentUserId, string targetUserId)
        {
            return await _context.Connections
                .AnyAsync(c =>
                    c.UserId == currentUserId &&
                    c.ConnectedUserId == targetUserId);
        }

        public async Task<bool> ExistsAsync(string userId, string connectedUserId)
        {
            return await _context.Connections
                .AnyAsync(c => c.UserId == userId && c.ConnectedUserId == connectedUserId);
        }

        public async Task<List<Connection>> GetUserConnectionsAsync(string currentUserId)
        {
            return await _context.Connections
                .Include(c => c.ConnectedUser)
                .Where(c => c.UserId == currentUserId)
                .OrderByDescending(c => c.ConnectedAt)
                .ToListAsync();
        }


        public async Task RemoveConnectionAsync(string currentUserId, string targetUserId)
        {
            var connections = await _context.Connections
                .Where(c =>
                    (c.UserId == currentUserId && c.ConnectedUserId == targetUserId) ||
                    (c.UserId == targetUserId && c.ConnectedUserId == currentUserId)
                )
                .ToListAsync();

            if (connections.Any())
            {
                _context.Connections.RemoveRange(connections);
            }
        }
    }
}
