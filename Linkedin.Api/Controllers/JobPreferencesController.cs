using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Linkedin.Core.Data;
using Linkedin.Core.Dtos.JobPreferences;
using Linkedin.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Linkedin.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class JobPreferencesController : ControllerBase
    {
        private static readonly string[] AllowedWorkplaces = { "On-site", "Hybrid", "Remote" };
        private static readonly string[] AllowedEmploymentTypes = { "Full-time", "Part-time", "Internship", "Contract" };
        private static readonly string[] AllowedStartAvailability = { "Immediately", "Flexible" };
        private readonly AppDbContext _context;

        public JobPreferencesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetPreferences()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var preference = await _context.Set<JobPreference>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == userId);

            return Ok(ToDto(preference));
        }

        [HttpPut]
        public async Task<IActionResult> SavePreferences([FromBody] JobPreferenceDto dto)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var titles = Clean(dto.JobTitles, 8, 100);
            var locations = Clean(dto.Locations, 8, 100);
            var workplaces = CleanAllowed(dto.WorkplaceTypes, AllowedWorkplaces);
            var employmentTypes = CleanAllowed(dto.EmploymentTypes, AllowedEmploymentTypes);
            var onsiteLocations = Clean(dto.OnsiteLocations, 8, 100);
            var remoteLocations = Clean(dto.RemoteLocations, 8, 100);
            var startAvailability = AllowedStartAvailability.FirstOrDefault(value =>
                string.Equals(
                    value,
                    dto.StartAvailability?.Trim(),
                    StringComparison.OrdinalIgnoreCase)) ?? "Immediately";

            var preference = await _context.Set<JobPreference>()
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (preference == null)
            {
                preference = new JobPreference { UserId = userId };
                await _context.Set<JobPreference>().AddAsync(preference);
            }

            preference.JobTitles = Join(titles);
            preference.Locations = Join(locations);
            preference.WorkplaceTypes = Join(workplaces);
            preference.EmploymentTypes = Join(employmentTypes);
            preference.IsOpenToWork = dto.IsOpenToWork;
            preference.OnsiteLocations = Join(onsiteLocations);
            preference.RemoteLocations = Join(remoteLocations);
            preference.StartAvailability = startAvailability;
            preference.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(ToDto(preference));
        }

        [HttpGet("recommended")]
        public async Task<IActionResult> GetRecommendedJobs([FromQuery] int take = 6)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();
            take = Math.Clamp(take, 1, 20);

            var now = DateTime.UtcNow;
            var user = await _context.Users
                .AsNoTracking()
                .Include(x => x.Skills)
                .FirstOrDefaultAsync(x => x.Id == userId);
            if (user == null) return NotFound();

            var preference = await _context.Set<JobPreference>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == userId);

            var titles = Split(preference?.JobTitles);
            var locations = Split(preference?.Locations);
            var workplaces = Split(preference?.WorkplaceTypes);
            var employmentTypes = Split(preference?.EmploymentTypes);
            var skills = user.Skills
                .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                .Select(x => Normalize(x.Name))
                .Distinct()
                .ToList();
            var currentPosition = Normalize(user.CurrentPosition);
            var currentLocation = Normalize(user.Location);
            var followedEmployerIds = (await _context.CompanyFollows
                .AsNoTracking()
                .Where(x => x.FollowerId == userId)
                .Select(x => x.EmployerId)
                .ToListAsync()).ToHashSet();

            var candidates = await _context.JobPosts
                .AsNoTracking()
                .Where(x => !x.IsBlocked && x.IsActive && (!x.ExpiresAt.HasValue || x.ExpiresAt > now))
                .Include(x => x.Employer)
                    .ThenInclude(x => x.Company)
                .OrderByDescending(x => x.CreatedAt)
                .Take(500)
                .ToListAsync();

            var savedIds = (await _context.SavedJobs
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .Select(x => x.JobPostId)
                .ToListAsync()).ToHashSet();
            var applications = await _context.JobApplications
                .AsNoTracking()
                .Where(x => x.ApplicantId == userId)
                .Select(x => new { x.JobPostId, x.AppliedAt })
                .ToListAsync();
            var applicationMap = applications.ToDictionary(x => x.JobPostId, x => (DateTime?)x.AppliedAt);

            var scored = candidates.Select(job =>
            {
                var searchable = Normalize($"{job.Title} {job.Description} {job.Location} {job.Employer?.Company?.Industry}");
                var reasons = new List<string>();
                var score = 0;

                if (titles.Any(x => searchable.Contains(Normalize(x)))) { score += 45; reasons.Add("preferred title"); }
                if (!string.IsNullOrWhiteSpace(currentPosition) && searchable.Contains(currentPosition)) { score += 20; reasons.Add("your position"); }

                var skillMatches = skills.Count(x => searchable.Contains(x));
                if (skillMatches > 0) { score += Math.Min(36, skillMatches * 12); reasons.Add($"{skillMatches} skill match"); }

                if (locations.Any(x => Normalize(job.Location).Contains(Normalize(x)))) { score += 25; reasons.Add("preferred location"); }
                else if (!string.IsNullOrWhiteSpace(currentLocation) && Normalize(job.Location).Contains(currentLocation)) { score += 10; reasons.Add("your location"); }

                if (workplaces.Any(x => string.Equals(x, job.WorkplaceType, StringComparison.OrdinalIgnoreCase))) { score += 15; reasons.Add(job.WorkplaceType); }
                if (employmentTypes.Any(x => string.Equals(x, job.EmploymentType, StringComparison.OrdinalIgnoreCase))) { score += 15; reasons.Add(job.EmploymentType); }
                var hasProfileMatch = score > 0;
                var isFromFollowedCompany = followedEmployerIds.Contains(job.EmployerId);
                if (hasProfileMatch && isFromFollowedCompany)
                {
                    score += 20;
                    reasons.Add("company you follow");
                }
                if (job.CreatedAt >= now.AddDays(-7)) score += 5;

                return new
                {
                    Job = job,
                    Score = score,
                    Reasons = reasons,
                    IsFromFollowedCompany = isFromFollowedCompany,
                    HasProfileMatch = hasProfileMatch
                };
            }).ToList();

            var selected = scored
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.Job.CreatedAt)
                .Take(take)
                .ToList();

            if (selected.Count == 0)
            {
                selected = scored
                    .OrderByDescending(x => x.Job.CreatedAt)
                    .Take(take)
                    .Select(x => new
                    {
                        x.Job,
                        Score = 25,
                        Reasons = new List<string> { "new opportunity" },
                        x.IsFromFollowedCompany,
                        x.HasProfileMatch
                    })
                    .ToList();
            }

            var result = selected.Select(x =>
            {
                var job = x.Job;
                var expired = job.ExpiresAt.HasValue && job.ExpiresAt <= now;
                applicationMap.TryGetValue(job.Id, out var appliedAt);
                return new RecommendedJobDto
                {
                    Id = job.Id,
                    EmployerId = job.EmployerId,
                    CompanyName = job.Employer?.Company?.Name ?? job.Employer?.FullName,
                    CompanyLogo = job.Employer?.Company?.LogoUrl ?? job.Employer?.ProfileImage,
                    CompanyUsername = job.Employer?.UserName,
                    Industry = job.Employer?.Company?.Industry,
                    Title = job.Title,
                    Description = job.Description,
                    Location = job.Location,
                    WorkplaceType = job.WorkplaceType,
                    EmploymentType = job.EmploymentType,
                    ApplyUrl = job.ApplyUrl,
                    CreatedAt = job.CreatedAt,
                    UpdatedAt = job.UpdatedAt,
                    ExpiresAt = job.ExpiresAt,
                    IsActive = job.IsActive,
                    IsExpired = expired,
                    CanApply = job.IsActive && !expired && !string.IsNullOrWhiteSpace(job.ApplyUrl),
                    HasApplyUrl = !string.IsNullOrWhiteSpace(job.ApplyUrl),
                    IsOwner = job.EmployerId == userId,
                    IsSaved = savedIds.Contains(job.Id),
                    IsApplied = applicationMap.ContainsKey(job.Id),
                    AppliedAt = appliedAt,
                    MatchScore = Math.Min(99, x.Score),
                    RecommendationReason = "Matches " + string.Join(", ", x.Reasons.Take(2)),
                    IsFromFollowedCompany = x.IsFromFollowedCompany,
                    HasProfileMatch = x.HasProfileMatch
                };
            });

            return Ok(result);
        }

        private string? GetCurrentUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        private static JobPreferenceDto ToDto(JobPreference? value) => new()
        {
            JobTitles = Split(value?.JobTitles),
            Locations = Split(value?.Locations),
            WorkplaceTypes = Split(value?.WorkplaceTypes),
            EmploymentTypes = Split(value?.EmploymentTypes),
            IsOpenToWork = value?.IsOpenToWork ?? false,
            OnsiteLocations = Split(value?.OnsiteLocations),
            RemoteLocations = Split(value?.RemoteLocations),
            StartAvailability = string.IsNullOrWhiteSpace(value?.StartAvailability)
                ? "Immediately"
                : value!.StartAvailability!,
            UpdatedAt = value?.UpdatedAt
        };

        private static List<string> Clean(IEnumerable<string>? values, int maxItems, int maxLength) =>
            (values ?? Array.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Where(x => x.Length <= maxLength && !x.Contains('|'))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(maxItems)
                .ToList();

        private static List<string> CleanAllowed(IEnumerable<string>? values, IEnumerable<string> allowed) =>
            Clean(values, 8, 100)
                .Where(x => allowed.Contains(x, StringComparer.OrdinalIgnoreCase))
                .ToList();

        private static string? Join(IEnumerable<string> values)
        {
            var result = string.Join('|', values);
            return string.IsNullOrWhiteSpace(result) ? null : result;
        }

        private static List<string> Split(string? value) =>
            string.IsNullOrWhiteSpace(value)
                ? new List<string>()
                : value.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

        private static string Normalize(string? value) => value?.Trim().ToLowerInvariant() ?? string.Empty;
    }
}
