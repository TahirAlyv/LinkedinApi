using Linkedin.Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Business.Services.Interface
{
    public interface IConnectionService
    {
        Task<ServiceResult> SendConnectionRequestAsync(string currentUserId, string receiverUsername);

        Task<ServiceResult> ConnectDirectlyAsync(string currentUserId, string targetUsername);
        Task<ServiceResult> AcceptRequestAsync(string currentUserId, int requestId);
        Task<ServiceResult> RejectRequestAsync(string currentUserId, int requestId);
        Task<ServiceResult> CancelRequestAsync(string currentUserId, int requestId);

        Task<ServiceResult> GetReceivedRequestsAsync(string currentUserId);
        Task<ServiceResult> GetSentRequestsAsync(string currentUserId);
        Task<ServiceResult> GetMyConnectionsAsync(string currentUserId);

        Task<ServiceResult> GetConnectionStatusAsync(string currentUserId, string targetUsername);

        Task<ServiceResult> RemoveConnectionAsync(string currentUserId, string targetUsername);


    }
}
