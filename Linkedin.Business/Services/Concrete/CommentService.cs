using Linkedin.Business.Services.Interface;
using Linkedin.Core.Common;
using Linkedin.Core.Dtos;
using Linkedin.Core.Entities;
using Linkedin.Core.Enums;
using Linkedin.DataAccess.Repositories.Interfaces;
 
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Business.Services.Concrete
{
    public class CommentService : ICommentService
    {

        private readonly IUnitOfWork _unitOfWork;
        private readonly INotficationsService _notficationsService;
        private readonly INotificationPublisher _notificationPublisher;
 


        public CommentService(IUnitOfWork unitOfWork, INotficationsService notficationsService, INotificationPublisher notificationPublisher)
        {
            _unitOfWork = unitOfWork;
            _notficationsService = notficationsService;
            _notificationPublisher = notificationPublisher;
        }

        private async Task<Comment> GetCommentEntityAsync(int commentId)
        {
            var comment = await _unitOfWork.Comments.GetWithIncludesAsync(commentId);
            if (comment == null)
                throw new Exception("Comment not found");

            return comment;
        }

        public async Task<CommentNotificationDto> AddComment(CreateCommentDto dto, string userId)
        {
            var post = await _unitOfWork.Posts.GetByIdAsync(dto.PostId);
            if (post == null) return null!;

            var comment = new Comment
            {
                PostId = dto.PostId,
                Text = dto.Text,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Comments.AddAsync(comment);
            await _unitOfWork.CompleteAsync();

            var fullComment = await _unitOfWork.Comments.GetWithIncludesAsync(comment.Id);

            if (fullComment.Post.UserID != userId)
            {
                await _notficationsService.CreateOrUpdateAsync(
                    senderId: userId,
                    receiverId: fullComment.Post.UserID,
                    type: NotificationType.Comment,
                    postId: fullComment.PostId,
                    contentPreview: fullComment.Text,
                    senderUsername: fullComment.User.UserName!,
                    senderProfilePhoto: fullComment.User.ProfileImage!
                );
            }

            return new CommentNotificationDto
            {
                CommentId = fullComment.Id,
                PostId = fullComment.PostId,
                Content = fullComment.Text,
                CreatedAt = fullComment.CreatedAt,
                Username = fullComment.User.UserName!,
                UserPhoto = fullComment.User.ProfileImage!,
                PostOwnerId = fullComment.Post.UserID,
                UserId = userId

            };
        }


        public async Task<List<CommentDto>> GetCommentsByPostIdAsync(int postId, int page, int pageSize)
        {
            var comments = await _unitOfWork.Comments.GetByPostIdAsync(postId, page, pageSize);

            return comments;
        }

        public async Task<int?> GetPostIdByCommentIdAsync(int commentId)
        {
            return await _unitOfWork.Comments.GetPostIdByCommentIdAsync(commentId);
        }

        public async Task<int> GetCommentCountByPostIdAsync(int postId)
        {
            return await _unitOfWork.Comments.GetCommentCountByPostIdAsync(postId);
        }

        public async Task<ServiceResult> DeleteByCommentIdAsync(int commentId,string userId)
        {
            var comment= await GetCommentEntityAsync(commentId);
            var commentOwenerId= comment.UserId;
            var postOwnerId= comment.Post.UserID;

            if (commentOwenerId != userId && userId != postOwnerId)
                return new ServiceResult (success:false, message:"Comment not found or you are not authorized to delete this comment");

            _unitOfWork.Comments.Remove(comment);
            await _unitOfWork.CompleteAsync();

             var dto =new CommentDto
            {
                Id = comment.Id,
                Text = comment.Text,
                CreatedAt = comment.CreatedAt,
                Username = comment.User.UserName!,
                UserPhoto = comment.User.ProfileImage!
            };


             return new ServiceResult (success:true, message:"Comment deleted successfully", data:dto);
 

        }

        public async Task<CommentDto> GetByCommentId(int commentId)
        {
            var comment = await GetCommentEntityAsync(commentId);

            return new CommentDto
            {
                Id = comment.Id,
                Text = comment.Text,
                CreatedAt = comment.CreatedAt,
                Username = comment.User.UserName!,
                UserPhoto = comment.User.ProfileImage!
            };
        }


        public async Task<CommentNotificationDto> GetCommentNotificationDto(int commentId)
        {
            var comment = await GetCommentEntityAsync(commentId);

            return new CommentNotificationDto
            {
                CommentId = comment.Id,
                PostId = comment.PostId,
                Content = comment.Text,
                CreatedAt = comment.CreatedAt,
                Username = comment.User.UserName!,
                UserPhoto = comment.User.ProfileImage!,
                PostOwnerId = comment.Post.UserID
            };
        }
        public async Task<ServiceResult> UpdateCommentAsync(int commentId, string userId, string text)
        {
            var comment = await GetCommentEntityAsync(commentId);

            if (comment.UserId != userId)
                return new ServiceResult(false, "You are not authorized to update this comment");

            comment.Text = text;
            comment.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.CompleteAsync();

            var dto = new CommentDto
            {
                Id = comment.Id,      
                UserId = comment.UserId,
                Text = comment.Text,       
                UpdatedAt = comment.UpdatedAt
            };

            return new ServiceResult(true, "Comment updated successfully", dto);
        }


    }
}
