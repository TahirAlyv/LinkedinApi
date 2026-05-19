using Linkedin.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.DataAccess.Repositories.Interfaces
{
    public interface IConnectionRepository : IRepository<Connection>
    {
        Task<bool> AreConnectedAsync(string currentUserId, string targetUserId);
        Task<bool> ExistsAsync(string userId, string connectedUserId);

        Task<List<Connection>> GetUserConnectionsAsync(string currentUserId);
        Task RemoveConnectionAsync(string currentUserId, string targetUserId);
    }
}
