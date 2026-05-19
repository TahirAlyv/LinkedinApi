using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Linkedin.Api.Helpers
{
    public class NameIdentifierProvider: IUserIdProvider
    {
        public string GetUserId(HubConnectionContext connection)
        {
            return connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        }
    }
}
