using Linkedin.Core.Common;
using Linkedin.Core.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Business.Services.Interface
{
    public interface IJobPostService
    {
        Task<ServiceResult> GetAllJobPostsAsync(
            string? currentUserId,
            int page,
            int pageSize,
            string? query
        );

        Task<ServiceResult> GetJobPostByIdAsync(int id, string? currentUserId);

        Task<ServiceResult> GetMyJobPostsAsync(string employerId, int page, int pageSize);

        Task<ServiceResult> GetJobPostsByEmployerUsernameAsync(
            string username,
            string? currentUserId,
            int page,
            int pageSize
        );

        Task<ServiceResult> CreateJobPostAsync(CreateJobPostDto dto, string employerId);

        Task<ServiceResult> UpdateJobPostAsync(int id, UpdateJobPostDto dto, string employerId);

        Task<ServiceResult> DeleteJobPostAsync(int id, string employerId);

        Task<ServiceResult> SaveJobAsync(int jobPostId, string userId);

        Task<ServiceResult> UnsaveJobAsync(int jobPostId, string userId);

        Task<ServiceResult> GetSavedJobsAsync(string userId, int page, int pageSize);

        Task<ServiceResult> ApplyToJobAsync(int jobPostId, string userId);

        Task<ServiceResult> WithdrawApplicationAsync(int jobPostId, string userId);

        Task<ServiceResult> GetAppliedJobsAsync(string userId, int page, int pageSize);
    }
}
