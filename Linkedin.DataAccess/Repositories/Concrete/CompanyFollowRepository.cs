using Linkedin.Core.Data;
using Linkedin.Core.Entities;
using Linkedin.DataAccess.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.DataAccess.Repositories.Concrete
{
   public class CompanyFollowRepository : Repository<CompanyFollow>, ICompanyFollowRepository
    {
        private readonly AppDbContext _context;

        public CompanyFollowRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<CompanyFollow?> GetFollowAsync(string followerId, string employerId)
        {
            return await _context.CompanyFollows
                .FirstOrDefaultAsync(cf =>
                    cf.FollowerId == followerId &&
                    cf.EmployerId == employerId);
        }

        public async Task<List<CompanyFollow>> GetFollowedCompaniesAsync(string followerId)
        {
            return await _context.CompanyFollows
                .AsNoTracking()
                .Include(cf => cf.Employer)
                    .ThenInclude(e => e.Company)
                .Where(cf => cf.FollowerId == followerId)
                .OrderByDescending(cf => cf.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<CompanyFollow>> GetCompanyFollowersAsync(string employerId)
        {
            return await _context.CompanyFollows
                .AsNoTracking()
                .Include(cf => cf.Follower)
                .Where(cf => cf.EmployerId == employerId)
                .OrderByDescending(cf => cf.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> GetFollowerCountAsync(string employerId)
        {
            return await _context.CompanyFollows
                .CountAsync(cf => cf.EmployerId == employerId);
        }
    }
}
