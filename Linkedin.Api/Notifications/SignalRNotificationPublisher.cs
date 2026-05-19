using Linkedin.Api.Hubs;
using Linkedin.Business.Services.Interface;
using Linkedin.Core.Dtos;
using Microsoft.AspNetCore.SignalR;

namespace Linkedin.Api.Notifications
{
    public class SignalRNotificationPublisher : INotificationPublisher
    {

        private readonly IHubContext<NotificationHub> _hubContext;

        public SignalRNotificationPublisher(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task PublishAsync(string userId, NotificationReturnDto dto)
        {
            await _hubContext.Clients.User(userId)
                .SendAsync("ReceiveNotification", dto);
        }
    }
}
