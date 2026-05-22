using Linkedin.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.DataAccess.Repositories.Interfaces
{
    public interface ICompanyFollowRepository : IRepository<CompanyFollow>
    {
        Task<CompanyFollow?> GetFollowAsync(string followerId, string employerId);
        Task<List<CompanyFollow>> GetFollowedCompaniesAsync(string followerId);
        Task<List<CompanyFollow>> GetCompanyFollowersAsync(string employerId);
        Task<int> GetFollowerCountAsync(string employerId);
    }
}
