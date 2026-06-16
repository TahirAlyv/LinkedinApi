
using Linkedin.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.DataAccess.Repositories.Interfaces
{
    public interface IJobPostRepository : IRepository<JobPost>
    {
        Task<List<JobPost>> GetAllJobPostsAsync(int skip, int take, string? query);

        Task<JobPost?> GetJobPostDetailsAsync(int id);

        Task<List<JobPost>> GetMyJobPostsAsync(string employerId, int skip, int take);

        Task<List<JobPost>> GetJobPostsByEmployerUsernameAsync(
            string username,
            int skip,
            int take
        );

        Task<List<JobPost>> GetJobPostsByEmployerIdsAsync(
            List<string> employerIds,
            int skip,
            int take);

        Task<List<JobPost>> GetRecommendedJobPostsAsync(
            string currentUserId,
            int page,
            int pageSize);
    }


}
