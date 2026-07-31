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
    public class JobApplicationRepository : Repository<JobApplication>, IJobApplicationRepository
    {
        public JobApplicationRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<JobApplication?> GetByUserAndJobAsync(string userId, int jobPostId)
        {
            return await _context.JobApplications
                .FirstOrDefaultAsync(a => a.ApplicantId == userId && a.JobPostId == jobPostId);
        }

        public async Task<List<JobApplication>> GetAppliedJobsByUserIdAsync(string userId, int skip, int take)
        {
            var now = DateTime.UtcNow;

            return await _context.JobApplications
                .Where(a =>
                    a.ApplicantId == userId &&
                   !a.JobPost.IsBlocked &&
                   !a.JobPost.Employer.IsBlocked
                )
                .Include(a => a.JobPost)
                    .ThenInclude(j => j.Employer)
                        .ThenInclude(e => e.Company)
                .Include(a => a.JobPost.SavedJobs)
                .Include(a => a.JobPost.Applications)
                .OrderByDescending(a => a.JobPost.IsActive && (a.JobPost.ExpiresAt == null || a.JobPost.ExpiresAt > now))
                .ThenByDescending(a => a.AppliedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<bool> IsAppliedAsync(string userId, int jobPostId)
        {
            return await _context.JobApplications
                .AnyAsync(a => a.ApplicantId == userId && a.JobPostId == jobPostId);
        }
    }
}
