
using Linkedin.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.DataAccess.Repositories.Interfaces
{
    public interface IJobPostRepository:IRepository<JobPost>
    {
        Task<List<JobPost>> GetAllPostsByFriendIdsAsync(List<string> friendIds);
        Task<List<JobPost>> GetAllAsync();
        Task<List<JobPost>> GetJobPostsByUserIdAsync(string userId, int skip, int take);
    }
}
