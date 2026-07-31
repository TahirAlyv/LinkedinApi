using Linkedin.Business.Services.Interface;
using Linkedin.Core.Dtos;
using Linkedin.Core.Entities;
using Linkedin.Core.Enums;
using Linkedin.DataAccess.Repositories.Interfaces;

namespace Linkedin.Business.Services.Concrete
{
    public class NotificationService : INotficationsService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationPublisher _notificationPublisher;

        public NotificationService(IUnitOfWork unitOfWork, INotificationPublisher notificationPublisher)
        {
            _unitOfWork = unitOfWork;
            _notificationPublisher = notificationPublisher;
        }

        private string Cut(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            return text.Length > 80 ? text[..80] + "..." : text;
        }

        public async Task<Notification> CreateOrUpdateAsync(
            string senderId,
            string receiverId,
            NotificationType type,
            int? postId,
            string contentPreview,
            string senderUsername,
            string senderProfilePhoto,
            int? eventId = null,
            int? jobPostId = null)
        {
            var notification = await _unitOfWork.Notifications.GetSingleAsync(n =>
                n.SenderId == senderId &&
                n.ReceiverId == receiverId &&
                n.PostId == postId &&
                n.EventId == eventId &&
                n.JobPostId == jobPostId &&
                n.Type == type
            );

            if (notification == null)
            {
                notification = new Notification
                {
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    PostId = postId,
                    EventId = eventId,
                    JobPostId = jobPostId,
                    Type = type,
                    SenderUsername = senderUsername,
                    SenderProfilePhoto = senderProfilePhoto,
                    ContentPreview = Cut(contentPreview),
                    CreatedAt = DateTime.UtcNow,
                    LastTriggeredAt = DateTime.UtcNow,
                    IsRead = false
                };

                await _unitOfWork.Notifications.AddAsync(notification);
            }
            else
            {
                notification.LastTriggeredAt = DateTime.UtcNow;
                notification.IsRead = false;
                notification.ContentPreview = Cut(contentPreview);
                notification.SenderUsername = senderUsername;
                notification.SenderProfilePhoto = senderProfilePhoto;
            }

            await _unitOfWork.CompleteAsync();

            var sender = await _unitOfWork.Users.GetByIdAsync(senderId);

            await _notificationPublisher.PublishAsync(
                receiverId,
                new NotificationReturnDto
                {
                    Id = notification.Id,
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    Type = type,
                    PostId = postId,
                    EventId = eventId,
                    JobPostId = jobPostId,
                    ContentPreview = notification.ContentPreview,
                    SenderUsername = notification.SenderUsername,
                    SenderProfilePhoto = notification.SenderProfilePhoto,
                    SenderIsCompany = sender?.UserType == UserType.Employer,
                    CreatedAt = notification.CreatedAt,
                    LastTriggeredAt = notification.LastTriggeredAt,
                    IsRead = notification.IsRead,
                }
            );

            return notification;
        }

        public async Task<List<NotificationReturnDto>> GetNotificationsForUserAsync(string userId)
        {
            var notifications = await _unitOfWork.Notifications.GetNotificationsAsync(userId);

            return notifications
                .OrderByDescending(n => n.LastTriggeredAt ?? n.CreatedAt)
                .Select(n => new NotificationReturnDto
                {
                    Id = n.Id,
                    SenderId = n.SenderId,
                    ReceiverId = n.ReceiverId,
                    Type = n.Type,
                    PostId = n.PostId,
                    CommentId = n.CommentId,
                    EventId = n.EventId,
                    JobPostId = n.JobPostId,

                    SenderUsername = !string.IsNullOrWhiteSpace(n.SenderUsername)
                        ? n.SenderUsername
                        : n.Sender?.UserName,

                    SenderProfilePhoto = !string.IsNullOrWhiteSpace(n.SenderProfilePhoto)
                        ? n.SenderProfilePhoto
                        : n.Sender?.ProfileImage,

                    SenderIsCompany = n.Sender?.UserType == UserType.Employer,

                    ContentPreview = n.ContentPreview,
                    CreatedAt = n.CreatedAt,
                    LastTriggeredAt = n.LastTriggeredAt,
                    IsRead = n.IsRead
                })
                .ToList();
        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            await _unitOfWork.Notifications.MarkAllAsReadAsync(userId);
            await _unitOfWork.CompleteAsync();
        }

        public Task<NotificationDto> DeleteNotificcation(NotificationDto dto)
        {
            throw new NotImplementedException();
        }
    }
}
