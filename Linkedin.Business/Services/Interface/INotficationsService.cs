using Linkedin.Core.Dtos;
using Linkedin.Core.Entities;
using Linkedin.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Business.Services.Interface
{
    public interface INotficationsService
    {
        Task<Notification> CreateOrUpdateAsync(
        string senderId,
        string receiverId,
        NotificationType type,
        int postId,
        string contentPreview,
        string senderUsername,
        string senderProfilePhoto
    );
        Task<List<NotificationReturnDto>> GetNotificationsForUserAsync(string userId);

        Task<NotificationDto> DeleteNotificcation(NotificationDto dto);
    }
}
