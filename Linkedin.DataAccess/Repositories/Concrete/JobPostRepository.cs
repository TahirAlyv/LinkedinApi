using Linkedin.Core.Data;
using Linkedin.Core.Dtos;
using Linkedin.Core.Entities;
using Linkedin.Core.Enums;
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


        public async Task<List<JobPost>> GetRecommendedJobPostsAsync(
            string currentUserId,
            int page,
            int pageSize)
        {
            if (string.IsNullOrWhiteSpace(currentUserId))
                return new List<JobPost>();

            if (page < 1)
                page = 1;

            if (pageSize < 1)
                pageSize = 10;

            if (pageSize > 50)
                pageSize = 50;

            var now = DateTime.UtcNow;

            var currentUser = await _context.Users
                .AsNoTracking()
                .Include(u => u.Skills)
                .FirstOrDefaultAsync(u => u.Id == currentUserId);

            if (currentUser == null)
                return new List<JobPost>();

            var followedEmployerIds = await _context.CompanyFollows
                .AsNoTracking()
                .Where(cf => cf.FollowerId == currentUserId)
                .Select(cf => cf.EmployerId)
                .Distinct()
                .ToListAsync();

            var searchKeywords = await _context.SearchHistories
                .AsNoTracking()
                .Where(x => x.UserId == currentUserId)
                .OrderByDescending(x => x.CreatedAt)
                .Take(10)
                .Select(x => x.NormalizedQuery)
                .ToListAsync();

            searchKeywords = searchKeywords
                .SelectMany(x => ExtractKeywords(x))
                .Distinct()
                .ToList();

            var positionKeywords = ExtractKeywords(currentUser.CurrentPosition);
            var locationKeywords = ExtractKeywords(currentUser.Location);

            var skillKeywords = currentUser.Skills
                .Where(s => !string.IsNullOrWhiteSpace(s.Name))
                .SelectMany(s => ExtractKeywords(s.Name))
                .Distinct()
                .ToList();

            var candidateJobs = await _context.JobPosts
                .AsNoTracking()
                .Where(j =>
                    !j.IsBlocked &&
                    j.IsActive &&
                    (!j.ExpiresAt.HasValue || j.ExpiresAt.Value > now) &&
                    j.Employer != null &&
                    !j.Employer.IsBlocked
                )
                .Include(j => j.Employer)
                    .ThenInclude(e => e.Company)
                .OrderByDescending(j => j.CreatedAt)
                .Take(500)
                .ToListAsync();

            var scoredJobs = candidateJobs
                .Select(job =>
                {
                    var searchableText = NormalizeText(
                        $"{job.Title} " +
                        $"{job.Description} " +
                        $"{job.Location} " +
                        $"{job.WorkplaceType} " +
                        $"{job.EmploymentType} " +
                        $"{job.Employer?.FullName} " +
                        $"{job.Employer?.CurrentPosition} " +
                        $"{job.Employer?.Location} " +
                        $"{job.Employer?.Company?.Name} " +
                        $"{job.Employer?.Company?.Industry} " +
                        $"{job.Employer?.Company?.Location} " +
                        $"{job.Employer?.Company?.Bio}"
                    );

                    var hasPositionMatch = positionKeywords.Any(k => searchableText.Contains(k));
                    var hasLocationMatch = locationKeywords.Any(k => searchableText.Contains(k));
                    var hasSkillMatch = skillKeywords.Any(k => searchableText.Contains(k));
                    var hasSearchMatch = searchKeywords.Any(k => searchableText.Contains(k));

                    var isFollowedEmployerJob = followedEmployerIds.Contains(job.EmployerId);

                    var strongRelevant =
                        isFollowedEmployerJob ||
                        hasSkillMatch ||
                        hasPositionMatch ||
                        hasSearchMatch ||
                        (hasLocationMatch && (hasPositionMatch || hasSkillMatch));

                    var weakRelevant =
                        hasLocationMatch ||
                        currentUser.UserType == Linkedin.Core.Enums.UserType.JobSeeker;

                    var score = 0;

                    if (isFollowedEmployerJob)
                        score += 25;

                    if (hasSkillMatch)
                        score += 40;

                    if (hasPositionMatch)
                        score += 35;

                    if (hasLocationMatch)
                        score += 20;

                    if (hasSearchMatch)
                        score += 35;

                    if (hasPositionMatch && hasSkillMatch)
                        score += 40;

                    if (hasLocationMatch && hasPositionMatch)
                        score += 25;

                    if (hasLocationMatch && hasSkillMatch)
                        score += 20;

                    if (hasLocationMatch && hasPositionMatch && hasSkillMatch)
                        score += 70;

                    if (hasSearchMatch && hasSkillMatch)
                        score += 30;

                    if (hasSearchMatch && hasPositionMatch)
                        score += 30;

                    if (hasSearchMatch && hasLocationMatch && hasPositionMatch && hasSkillMatch)
                        score += 80;

                    if (strongRelevant || weakRelevant)
                    {
                        if (currentUser.UserType == Linkedin.Core.Enums.UserType.JobSeeker)
                            score += 20;

                        if (job.CreatedAt >= DateTime.UtcNow.AddDays(-7))
                            score += 10;
                        else if (job.CreatedAt >= DateTime.UtcNow.AddDays(-30))
                            score += 5;

                        score += (job.Id * 17) % 10;
                    }

                    return new
                    {
                        Job = job,
                        Score = score,
                        StrongRelevant = strongRelevant,
                        WeakRelevant = weakRelevant
                    };
                })
                .ToList();

            var strongJobs = scoredJobs
                .Where(x => x.StrongRelevant)
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.Job.CreatedAt)
                .ToList();

            var weakJobs = scoredJobs
                .Where(x => !x.StrongRelevant && x.WeakRelevant)
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.Job.CreatedAt)
                .Take(Math.Max(1, pageSize / 4))
                .ToList();

            var fallbackJobs = scoredJobs
                .Where(x => !x.StrongRelevant && !x.WeakRelevant)
                .OrderByDescending(x => x.Job.CreatedAt)
                .Take(Math.Max(1, pageSize / 5))
                .ToList();

            var finalJobs = strongJobs
                .Concat(weakJobs)
                .Concat(fallbackJobs)
                .GroupBy(x => x.Job.Id)
                .Select(g => g.First())
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.Job.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => x.Job)
                .ToList();

            return finalJobs;
        }

        private static string NormalizeText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";

            return text.Trim().ToLower();
        }

        private static List<string> ExtractKeywords(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<string>();

            char[] separators =
            {
            ' ', ',', '.', ';', ':', '-', '_', '/', '\\',
            '(', ')', '[', ']', '{', '}', '\n', '\r', '\t'
            };

            return text
                .ToLower()
                .Split(separators, StringSplitOptions.RemoveEmptyEntries)
                .Where(x => x.Length >= 2)
                .Distinct()
                .ToList();
        }
    }
}
