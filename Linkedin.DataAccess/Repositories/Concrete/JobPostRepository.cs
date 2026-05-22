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
        public JobPostRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<List<JobPost>> GetAllJobPostsAsync(int skip, int take, string? query)
        {
            var now = DateTime.UtcNow;

            var jobsQuery = _context.JobPosts
                .AsNoTracking()
                .Where(j => !j.IsBlocked)
                .Include(j => j.Employer)
                    .ThenInclude(e => e.Company)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                var search = query.Trim().ToLower();

                jobsQuery = jobsQuery.Where(j =>
                    j.Title.ToLower().Contains(search) ||
                    j.Description.ToLower().Contains(search) ||
                    (j.Location != null && j.Location.ToLower().Contains(search)) ||
                    (j.WorkplaceType != null && j.WorkplaceType.ToLower().Contains(search)) ||
                    (j.EmploymentType != null && j.EmploymentType.ToLower().Contains(search)) ||
                    (j.Employer.FullName != null && j.Employer.FullName.ToLower().Contains(search)) ||
                    (j.Employer.Company != null &&
                     j.Employer.Company.Name != null &&
                     j.Employer.Company.Name.ToLower().Contains(search)) ||
                    (j.Employer.Company != null &&
                     j.Employer.Company.Industry != null &&
                     j.Employer.Company.Industry.ToLower().Contains(search))
                );
            }

            return await jobsQuery
                .OrderByDescending(j => j.IsActive && (j.ExpiresAt == null || j.ExpiresAt > now))
                .ThenByDescending(j => j.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<JobPost?> GetJobPostDetailsAsync(int id)
        {
            return await _context.JobPosts
                .Include(j => j.Employer)
                    .ThenInclude(e => e.Company)
                .FirstOrDefaultAsync(j => j.Id == id);
        }

        public async Task<List<JobPost>> GetMyJobPostsAsync(string employerId, int skip, int take)
        {
            var now = DateTime.UtcNow;

            return await _context.JobPosts
                .Where(j => j.EmployerId == employerId)
                .Include(j => j.Employer)
                    .ThenInclude(e => e.Company)
                .OrderByDescending(j => j.IsActive && (j.ExpiresAt == null || j.ExpiresAt > now))
                .ThenByDescending(j => j.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<List<JobPost>> GetJobPostsByEmployerUsernameAsync(string username, int skip, int take)
        {
            var now = DateTime.UtcNow;

            return await _context.JobPosts
                .Where(j => j.Employer.UserName == username && !j.IsBlocked)
                .Include(j => j.Employer)
                    .ThenInclude(e => e.Company)
                .OrderByDescending(j => j.IsActive && (j.ExpiresAt == null || j.ExpiresAt > now))
                .ThenByDescending(j => j.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<List<JobPost>> GetJobPostsByEmployerIdsAsync(
        List<string> employerIds,
        int skip,
        int take)
        {
            if (employerIds == null || !employerIds.Any())
                return new List<JobPost>();

            var now = DateTime.UtcNow;

            return await _context.JobPosts
                .AsNoTracking()
                .Where(j => employerIds.Contains(j.EmployerId) && !j.IsBlocked)
                .Include(j => j.Employer)
                    .ThenInclude(e => e.Company)
                .OrderByDescending(j => j.IsActive && (j.ExpiresAt == null || j.ExpiresAt > now))
                .ThenByDescending(j => j.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }
    }
}
