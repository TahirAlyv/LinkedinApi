using Linkedin.Core.Common;
using Linkedin.Core.Dtos;
 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Business.Services.Interface
{
    public interface ICommentService
    {
        Task<CommentNotificationDto> AddComment(CreateCommentDto dto, string userId);
        Task<List<CommentDto>> GetCommentsByPostIdAsync(int postId, int page, int pageSize);
        Task<ServiceResult> DeleteByCommentIdAsync(int commentId, string userId);
        Task<CommentDto> GetByCommentId(int comentId);
        Task<CommentNotificationDto> GetCommentNotificationDto(int commentId);
        Task<int> GetCommentCountByPostIdAsync(int postId);
        Task<int?> GetPostIdByCommentIdAsync(int commentId);
        Task<ServiceResult> UpdateCommentAsync(
             int commentId,
             string userId,
             string text);

    }
        
}
