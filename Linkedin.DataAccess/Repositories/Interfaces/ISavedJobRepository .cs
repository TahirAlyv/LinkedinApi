using Linkedin.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.DataAccess.Repositories.Interfaces
{
    public interface ISavedJobRepository: IRepository<SavedJob>
    {
        Task<SavedJob?> GetByUserAndJobAsync(string userId, int jobPostId);
        Task<List<SavedJob>> GetSavedJobsByUserIdAsync(string userId, int skip, int take);
        Task<bool> IsSavedAsync(string userId, int jobPostId);
    }
}
