using Linkedin.Core.Dtos;
using Linkedin.Core.Entities;
using Linkedin.Core.Enums;

namespace Linkedin.Business.Services.Interface
{
    public interface INotficationsService
    {
        Task<Notification> CreateOrUpdateAsync(
            string senderId,
            string receiverId,
            NotificationType type,
            int? postId,
            string contentPreview,
            string senderUsername,
            string senderProfilePhoto,
            int? eventId = null,
            int? jobPostId = null
        );

        Task<List<NotificationReturnDto>> GetNotificationsForUserAsync(string userId);

        Task MarkAllAsReadAsync(string userId);

        Task<NotificationDto> DeleteNotificcation(NotificationDto dto);
    }
}
