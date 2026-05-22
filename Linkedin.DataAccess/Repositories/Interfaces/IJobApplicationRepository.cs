using Linkedin.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.DataAccess.Repositories.Interfaces
{
    public interface IJobApplicationRepository : IRepository<JobApplication>
    {
        Task<JobApplication?> GetByUserAndJobAsync(string userId, int jobPostId);
        Task<List<JobApplication>> GetAppliedJobsByUserIdAsync(string userId, int skip, int take);
        Task<bool> IsAppliedAsync(string userId, int jobPostId);
    }
}
