using Linkedin.Business.Services.Interface;
using Linkedin.Core.Common;
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
    public class LikeService : ILikeService
    {

        private IUnitOfWork _unitOfWork;
        private readonly INotificationPublisher _notificationPublisher;
        private readonly INotficationsService _notificationsService;

        public LikeService(IUnitOfWork unitOfWork, INotificationPublisher notificationPublisher, INotficationsService notificationsService)
        {
            _unitOfWork = unitOfWork;
            _notificationPublisher = notificationPublisher;
            _notificationsService = notificationsService;
        }
        public async Task<ServiceResult> ToggleLikeAsync(int postId, string userId)
        {
            var post = await _unitOfWork.Posts
                .GetPostByIdAsync(postId, p => p.User);

            if (post == null)
                return new ServiceResult(false, "Post not found", null);

            var existingLike = (await _unitOfWork.Likes
                .FindAsync(l => l.PostId == postId && l.UserId == userId))
                .FirstOrDefault();

            // =========================
            // 👍 LIKE
            // =========================
            if (existingLike == null)
            {
                var like = new Like
                {
                    PostId = postId,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Likes.AddAsync(like);
                post.LikeCount = (post.LikeCount ?? 0) + 1;

                // 🔔 NOTIFICATION (öz postu deyilsə)
                if (post.User.Id != userId)
                {
                    var sender = await _unitOfWork.Users.GetByIdAsync(userId);
                    if (sender == null)
                        return new ServiceResult(false, "Sender not found", null);

                    await _notificationsService.CreateOrUpdateAsync(
                        senderId: userId,
                        receiverId: post.User.Id,
                        type: NotificationType.Like,
                        postId: post.Id,
                        contentPreview: "liked your post",
                        senderUsername: sender.UserName!,
                        senderProfilePhoto: sender.ProfileImage!
                    );
                }
            }
            // =========================
            // 👎 UNLIKE
            // =========================
            else
            {
                _unitOfWork.Likes.Remove(existingLike);
                post.LikeCount = Math.Max((post.LikeCount ?? 1) - 1, 0);
                // ❗ notification-a TOXMURUQ
            }

            _unitOfWork.Posts.Update(post);
            await _unitOfWork.CompleteAsync();

            return new ServiceResult(
                true,
                "Toggled",
                post.LikeCount ?? 0
            );
        }





        public async Task<ServiceResult> RemoveLikeAsync(int postId, string userId)
        {
            var post = await _unitOfWork.Posts.GetByIdAsync(postId);
            if (post == null)
                return new ServiceResult(false, "Post not found.", null);

            var like = (await _unitOfWork.Likes.FindAsync(l => l.PostId == postId && l.UserId == userId)).FirstOrDefault();
            if (like == null)
                return new ServiceResult(false, "Like not found.", null);

            post.LikeCount = Math.Max((post.LikeCount ?? 1) - 1, 0);

            _unitOfWork.Posts.Update(post);
            _unitOfWork.Likes.Remove(like);
            var result = await _unitOfWork.CompleteAsync();
            if (result <= 0)
                return new ServiceResult(false, "An error occurred while removing like.", null);
            return new ServiceResult(true, "Like removed successfully.", null);


        }

        public async Task<(bool Success, int LikeCount)> GetLikeCountByPostId(int postId)
        {
            var post = await _unitOfWork.Posts.GetByIdAsync(postId);

            if (post == null)
                return (false, 0);

            return (true, post.LikeCount ?? 0);
        }



    }

   
}
