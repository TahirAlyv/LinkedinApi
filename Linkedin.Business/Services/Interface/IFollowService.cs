using Linkedin.Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Business.Services.Interface
{
    public interface IFollowService
    {
        Task<ServiceResult> FollowAsync(string followerUserId, string followedUserId);
        Task<bool> IsFollowing(string followerUserId, string followedUserId);
        Task<ServiceResult> UnfollowAsync(string followerUserId, string followedUserId);
    }
}
