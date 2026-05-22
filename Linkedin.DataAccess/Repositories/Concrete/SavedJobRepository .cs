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
    public class SavedJobRepository : Repository<SavedJob>, ISavedJobRepository
    {
        public SavedJobRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<SavedJob?> GetByUserAndJobAsync(string userId, int jobPostId)
        {
            return await _context.SavedJobs
                .FirstOrDefaultAsync(s => s.UserId == userId && s.JobPostId == jobPostId);
        }

        public async Task<List<SavedJob>> GetSavedJobsByUserIdAsync(string userId, int skip, int take)
        {
            var now = DateTime.UtcNow;

            return await _context.SavedJobs
                .Where(s =>
                     s.UserId == userId &&
                    !s.JobPost.IsBlocked &&
                    !s.JobPost.Employer.IsBlocked
                )
                .Include(s => s.JobPost)
                    .ThenInclude(j => j.Employer)
                        .ThenInclude(e => e.Company)
                .OrderByDescending(s => s.JobPost.IsActive && (s.JobPost.ExpiresAt == null || s.JobPost.ExpiresAt > now))
                .ThenByDescending(s => s.SavedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<bool> IsSavedAsync(string userId, int jobPostId)
        {
            return await _context.SavedJobs
                .AnyAsync(s => s.UserId == userId && s.JobPostId == jobPostId);
        }
    }
}
