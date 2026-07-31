using System.Security.Claims;
using Linkedin.Core.Data;
using Linkedin.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Linkedin.Api.Controllers
{
    [ApiController]
    [Authorize(Roles = "Employer")]
    [Route("api/company/hiring")]
    public class CompanyHiringController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CompanyHiringController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("overview")]
        public async Task<IActionResult> Overview()
        {
            var employerId = CurrentUserId();
            var now = DateTime.UtcNow;
            var from = now.AddDays(-30);
            var jobs = await _context.JobPosts.AsNoTracking()
                .Where(item => item.EmployerId == employerId)
                .Select(item => new
                {
                    item.Id,
                    item.IsActive,
                    item.IsBlocked,
                    item.ExpiresAt,
                    Saves = item.SavedJobs.Count,
                    Invitations = item.Invitations.Count
                })
                .ToListAsync();

            var jobIds = jobs.Select(item => item.Id).ToList();
            var applyClicks = await _context.AnalyticsEvents.AsNoTracking()
                .CountAsync(item =>
                    jobIds.Contains(item.JobPostId ?? 0) &&
                    item.EventType == AnalyticsEventType.JobApplyClick);
            var recentApplyClicks = await _context.AnalyticsEvents.AsNoTracking()
                .CountAsync(item =>
                    jobIds.Contains(item.JobPostId ?? 0) &&
                    item.EventType == AnalyticsEventType.JobApplyClick &&
                    item.CreatedAt >= from);

            return Ok(new
            {
                periodDays = 30,
                metrics = new
                {
                    activeJobs = jobs.Count(item =>
                        item.IsActive &&
                        !item.IsBlocked &&
                        (!item.ExpiresAt.HasValue ||
                         item.ExpiresAt > now)),
                    closedJobs = jobs.Count(item =>
                        !item.IsActive ||
                        (item.ExpiresAt.HasValue &&
                         item.ExpiresAt <= now)),
                    totalSaves = jobs.Sum(item => item.Saves),
                    externalApplyClicks = applyClicks,
                    recentExternalApplyClicks = recentApplyClicks,
                    sentInvitations = jobs.Sum(item => item.Invitations)
                }
            });
        }

        [HttpGet("jobs")]
        public async Task<IActionResult> Jobs(
            [FromQuery] string status = "all",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 50);
            var employerId = CurrentUserId();
            var now = DateTime.UtcNow;
            var query = _context.JobPosts.AsNoTracking()
                .Where(item => item.EmployerId == employerId);

            if (string.Equals(
                    status,
                    "active",
                    StringComparison.OrdinalIgnoreCase))
                query = query.Where(item =>
                    item.IsActive &&
                    (!item.ExpiresAt.HasValue ||
                     item.ExpiresAt > now));
            else if (string.Equals(
                         status,
                         "closed",
                         StringComparison.OrdinalIgnoreCase))
                query = query.Where(item =>
                    !item.IsActive ||
                    (item.ExpiresAt.HasValue &&
                     item.ExpiresAt <= now));

            var totalCount = await query.CountAsync();
            var jobs = await query
                .OrderByDescending(item => item.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(item => new
                {
                    item.Id,
                    item.Title,
                    item.Location,
                    item.WorkplaceType,
                    item.EmploymentType,
                    item.RequiredSkills,
                    item.MinimumExperienceYears,
                    item.CreatedAt,
                    item.UpdatedAt,
                    item.ExpiresAt,
                    item.IsActive,
                    item.IsBlocked,
                    saves = item.SavedJobs.Count,
                    views = _context.AnalyticsEvents.Count(
                        analytics =>
                            analytics.JobPostId == item.Id &&
                            analytics.EventType ==
                            AnalyticsEventType.JobView),
                    externalApplyClicks = _context.AnalyticsEvents.Count(
                        analytics =>
                            analytics.JobPostId == item.Id &&
                            analytics.EventType ==
                            AnalyticsEventType.JobApplyClick),
                    invitations = item.Invitations.Count
                })
                .ToListAsync();

            var candidatePreferences = await _context.JobPreferences
                .AsNoTracking()
                .Where(item => item.IsOpenToWork)
                .ToDictionaryAsync(item => item.UserId);
            var openCandidateIds = candidatePreferences.Keys.ToList();

            var allCandidates = await _context.Users.AsNoTracking()
                .Where(item =>
                    item.UserType == UserType.JobSeeker &&
                    !item.IsBlocked &&
                    openCandidateIds.Contains(item.Id))
                .Include(item => item.Skills)
                .Include(item => item.Experiences)
                .ToListAsync();

            var items = jobs.Select(job =>
            {
                var skills = Split(job.RequiredSkills);
                var matchingTalent = allCandidates.Count(candidate =>
                {
                    var preference = candidatePreferences[candidate.Id];
                    var skillMatched = skills.Count == 0 ||
                        candidate.Skills.Any(skill =>
                            skills.Contains(
                                skill.Name,
                                StringComparer.OrdinalIgnoreCase));
                    var workplaceMatched =
                        string.IsNullOrWhiteSpace(job.WorkplaceType) ||
                        Split(preference.WorkplaceTypes).Contains(
                            job.WorkplaceType,
                            StringComparer.OrdinalIgnoreCase);
                    var employmentMatched =
                        string.IsNullOrWhiteSpace(job.EmploymentType) ||
                        Split(preference.EmploymentTypes).Contains(
                            job.EmploymentType,
                            StringComparer.OrdinalIgnoreCase);
                    var preferredLocations = string.Equals(
                            job.WorkplaceType,
                            "Remote",
                            StringComparison.OrdinalIgnoreCase)
                        ? Split(preference.RemoteLocations)
                        : Split(preference.OnsiteLocations);
                    var locationMatched =
                        string.IsNullOrWhiteSpace(job.Location) ||
                        candidate.Location?.Contains(
                            job.Location,
                            StringComparison.OrdinalIgnoreCase) == true ||
                        preferredLocations.Any(location =>
                            location.Contains(
                                job.Location,
                                StringComparison.OrdinalIgnoreCase) ||
                            job.Location.Contains(
                                location,
                                StringComparison.OrdinalIgnoreCase));
                    var experienceMatched =
                        ExperienceYears(candidate.Experiences) >=
                        job.MinimumExperienceYears;

                    return skillMatched &&
                        workplaceMatched &&
                        employmentMatched &&
                        locationMatched &&
                        experienceMatched;
                });

                return new
                {
                    job.Id,
                    job.Title,
                    job.Location,
                    job.WorkplaceType,
                    job.EmploymentType,
                    requiredSkills = skills,
                    job.MinimumExperienceYears,
                    job.CreatedAt,
                    job.UpdatedAt,
                    job.ExpiresAt,
                    job.IsActive,
                    job.IsBlocked,
                    job.saves,
                    job.views,
                    job.externalApplyClicks,
                    job.invitations,
                    matchingTalent
                };
            }).ToList();

            return Ok(new
            {
                items,
                page,
                pageSize,
                totalCount,
                totalPages = Math.Max(
                    1,
                    (int)Math.Ceiling(totalCount / (double)pageSize))
            });
        }

        [HttpPatch("jobs/{jobId:int}/status")]
        public async Task<IActionResult> ChangeStatus(
            int jobId,
            [FromBody] ChangeJobStatusRequest request)
        {
            var employerId = CurrentUserId();
            var job = await _context.JobPosts.FirstOrDefaultAsync(item =>
                item.Id == jobId &&
                item.EmployerId == employerId);

            if (job == null)
                return NotFound();

            job.IsActive = request.IsActive;
            job.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { job.Id, job.IsActive, job.UpdatedAt });
        }

        [HttpGet("jobs/{jobId:int}/insights")]
        public async Task<IActionResult> Insights(int jobId)
        {
            var employerId = CurrentUserId();
            var job = await _context.JobPosts.AsNoTracking()
                .Where(item =>
                    item.Id == jobId &&
                    item.EmployerId == employerId)
                .Select(item => new
                {
                    item.Id,
                    item.Title,
                    item.CreatedAt,
                    saves = item.SavedJobs.Count,
                    invitations = item.Invitations.Count,
                    views = _context.AnalyticsEvents.Count(analytics =>
                        analytics.JobPostId == item.Id &&
                        analytics.EventType == AnalyticsEventType.JobView)
                })
                .FirstOrDefaultAsync();

            if (job == null)
                return NotFound();

            var clicks = await _context.AnalyticsEvents.AsNoTracking()
                .Where(item =>
                    item.JobPostId == jobId &&
                    item.EventType == AnalyticsEventType.JobApplyClick)
                .Select(item => item.CreatedAt)
                .ToListAsync();
            var days = 30;
            var from = DateTime.UtcNow.Date.AddDays(-(days - 1));
            var chart = Enumerable.Range(0, days)
                .Select(index =>
                {
                    var date = from.AddDays(index);
                    var next = date.AddDays(1);
                    return new
                    {
                        date = date.ToString("yyyy-MM-dd"),
                        externalApplyClicks = clicks.Count(item =>
                            item >= date &&
                            item < next)
                    };
                })
                .ToList();

            return Ok(new
            {
                job,
                externalApplyClicks = clicks.Count,
                applyClickConversion = job.views == 0
                    ? 0
                    : Math.Round(clicks.Count * 100d / job.views, 1),
                chart
            });
        }

        private string CurrentUserId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User id claim is missing.");

        private static List<string> Split(string? value) =>
            string.IsNullOrWhiteSpace(value)
                ? new List<string>()
                : value.Split(
                        new[] { ',', '|', ';' },
                        StringSplitOptions.RemoveEmptyEntries |
                        StringSplitOptions.TrimEntries)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

        private static int ExperienceYears(
            IEnumerable<Linkedin.Core.Entities.Experience> experiences)
        {
            var months = experiences.Sum(item =>
            {
                if (!item.StartYear.HasValue)
                    return 0;
                var startMonth = Math.Clamp(item.StartMonth ?? 1, 1, 12);
                var endYear = item.IsCurrent
                    ? DateTime.UtcNow.Year
                    : item.EndYear ?? item.StartYear.Value;
                var endMonth = item.IsCurrent
                    ? DateTime.UtcNow.Month
                    : Math.Clamp(item.EndMonth ?? 12, 1, 12);
                return Math.Max(
                    0,
                    ((endYear - item.StartYear.Value) * 12) +
                    endMonth -
                    startMonth +
                    1);
            });
            return (int)Math.Round(
                months / 12d,
                MidpointRounding.AwayFromZero);
        }

        public sealed class ChangeJobStatusRequest
        {
            public bool IsActive { get; set; }
        }
    }
}
