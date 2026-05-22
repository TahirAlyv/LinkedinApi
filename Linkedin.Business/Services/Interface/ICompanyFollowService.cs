using Linkedin.Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Business.Services.Interface
{
    public interface ICompanyFollowService
    {
        Task<ServiceResult> FollowCompanyAsync(string currentUserId, string employerUsername);
        Task<ServiceResult> UnfollowCompanyAsync(string currentUserId, string employerUsername);
        Task<ServiceResult> GetFollowStatusAsync(string currentUserId, string employerUsername);
        Task<ServiceResult> GetMyFollowedCompaniesAsync(string currentUserId);
        Task<ServiceResult> GetMyCompanyFollowersAsync(string currentUserId);
        Task<ServiceResult> GetCompanyFollowerCountAsync(string employerUsername);
    }
}
