
using Linkedin.DataAccess.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Linkedin.Api.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        public override Task OnConnectedAsync()
        {
            Console.WriteLine($"User connected: {Context.UserIdentifier}");
            return base.OnConnectedAsync();
        }
 

    }
}
