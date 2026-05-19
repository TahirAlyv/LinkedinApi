using Linkedin.Core.Data;
using Linkedin.Core.Dtos;
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
    public class JobPostRepository : Repository<JobPost>, IJobPostRepository
    {
        public JobPostRepository(AppDbContext dbContext) : base(dbContext) { }

        public async Task<List<JobPost>> GetAllPostsByFriendIdsAsync(List<string> friendIds)
        {


            return await _context.JobPosts
                .Where(p => friendIds.Contains(p.EmployerId))
                .Include(p => p.Employer)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();


        }

 

        public async Task<List<JobPost>> GetAllAsync()
        {
            return await _context.JobPosts
                .Include(p => p.Employer)            
                    .ThenInclude(e => e.Company)        
                .OrderByDescending(p => p.CreatedAt)     
                .ToListAsync();
        }

        public async Task<List<JobPost>> GetJobPostsByUserIdAsync(string userId, int skip, int take)
        {
            return await _context.JobPosts
                .Where(p => p.EmployerId == userId)   
                .Include(p => p.Employer)
                    .ThenInclude(u => u.Company)
                .OrderByDescending(p => p.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        
    }
}
