using Linkedin.Core.Common;
using Linkedin.Core.Dtos;
using Linkedin.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Business.Services.Interface
{
    public interface IPostService
    {
        Task<ServiceResult> CreatePostAsync(CreatePostDto postDto, string userId);

        Task<ServiceResult> GetPostsByUserIdAsync(
            string postOwnerId,
            string? currentUserId,
            int page,
            int pageSize);

        Task<ServiceResult> GetHomeFeedAsync(
            string currentUserId,
            int page,
            int pageSize);

        Task<ServiceResult> UpdatePost(string userId, UpdatePostDto postDto);

        Task<ServiceResult> DeletePostAsync(string userId, int postId);

        Task<PostDto> GetUserIdPostId(string userId, int postId);
    }
}
