using Linkedin.Core.Common;
using Linkedin.Core.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Business.Services.Interface
{
    public interface ILikeService
    {
        Task<ServiceResult> ToggleLikeAsync(int postId, string userId);
        Task<ServiceResult> RemoveLikeAsync(int postId, string userId);
        Task<(bool Success, int LikeCount)> GetLikeCountByPostId(int postId);
    }
}
