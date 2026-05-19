using Linkedin.Business.Services.Interface;
using Linkedin.Core.Dtos;
using Linkedin.Core.Entities;
using Linkedin.Core.Enums;
using Linkedin.DataAccess.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            int postId,
            string contentPreview,
            string senderUsername,
            string senderProfilePhoto)
        {
            var notification = await _unitOfWork.Notifications.GetSingleAsync(n =>
                n.SenderId == senderId &&
                n.ReceiverId == receiverId &&
                n.PostId == postId &&
                n.Type == type
            );

            if (notification == null)
            {
                notification = new Notification
                {
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    PostId = postId,
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
            }

            await _unitOfWork.CompleteAsync();

            // 🔔 🔥 CANLI SIGNALR PUSH (ƏSAS HİSSƏ)
            await _notificationPublisher.PublishAsync(
                receiverId,
                new NotificationReturnDto
                {
                    Id = notification.Id,
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    Type = type,
                    PostId = postId,
                    ContentPreview = notification.ContentPreview,
                    SenderUsername = notification.SenderUsername,
                    SenderProfilePhoto = notification.SenderProfilePhoto,
                    CreatedAt = notification.CreatedAt,
                    LastTriggeredAt = notification.LastTriggeredAt,
                    IsRead = notification.IsRead
                }
            );

            return notification;
        }




        public async Task<List<NotificationReturnDto>> GetNotificationsForUserAsync(string userId)
        {
            var notifications = await _unitOfWork.Notifications.GetNotificationsAsync(userId);

            return notifications
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new NotificationReturnDto
                {
                    Id = n.Id,
                    SenderId = n.SenderId,
                    ReceiverId = n.ReceiverId,
                    Type = n.Type,
                    PostId = n.PostId,
                    CommentId = n.CommentId,
                    SenderUsername = n.Sender?.UserName,
                    SenderProfilePhoto = n.Sender?.ProfileImage,
                    ContentPreview = n.ContentPreview,
                    CreatedAt = n.CreatedAt,
                    LastTriggeredAt = n.LastTriggeredAt,
                    IsRead = n.IsRead
                })
                .ToList();
        }

        public Task<NotificationDto> DeleteNotificcation(NotificationDto dto)
        {
            throw new NotImplementedException();
        }
       

 

    }
}

