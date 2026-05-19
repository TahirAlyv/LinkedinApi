using Linkedin.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.DataAccess.Repositories.Interfaces
{
    public interface IConnectionRequestRepository : IRepository<ConnectionRequest>
    {
        Task<ConnectionRequest?> GetPendingRequestBetweenUsersAsync(
            string firstUserId,
            string secondUserId);

        Task<ConnectionRequest?> GetRequestWithUsersAsync(int requestId);

        Task<List<ConnectionRequest>> GetReceivedPendingRequestsAsync(string currentUserId);

        Task<List<ConnectionRequest>> GetSentPendingRequestsAsync(string currentUserId);
    }
}

