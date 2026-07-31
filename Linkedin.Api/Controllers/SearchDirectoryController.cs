using System.Security.Claims;
using System.Text.RegularExpressions;
using Linkedin.Core.Data;
using Linkedin.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Linkedin.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class SearchDirectoryController : ControllerBase
{
    private readonly AppDbContext _context;

    public SearchDirectoryController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("people")]
    public async Task<IActionResult> People(
        [FromQuery] string? query = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 6)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var term = Normalize(query);
        NormalizePaging(ref page, ref pageSize);

        var candidatesQuery = _context.Users
            .AsNoTracking()
            .Include(user => user.Skills)
            .Include(user => user.Experiences)
            .Include(user => user.Educations)
            .Where(user =>
                !user.IsBlocked &&
                user.Id != currentUserId &&
                user.UserType == UserType.JobSeeker);

        if (term.Length > 0)
        {
            candidatesQuery = candidatesQuery.Where(user =>
                (user.UserName != null && user.UserName.ToLower().Contains(term)) ||
                (user.FullName != null && user.FullName.ToLower().Contains(term)) ||
                (user.CurrentPosition != null && user.CurrentPosition.ToLower().Contains(term)) ||
                (user.Location != null && user.Location.ToLower().Contains(term)) ||
                (user.Bio != null && user.Bio.ToLower().Contains(term)) ||
                user.Skills.Any(skill => skill.Name.ToLower().Contains(term)) ||
                user.Experiences.Any(experience =>
                    experience.CompanyName.ToLower().Contains(term) ||
                    experience.Title.ToLower().Contains(term)) ||
                user.Educations.Any(education =>
                    education.School.ToLower().Contains(term) ||
                    (education.Field != null && education.Field.ToLower().Contains(term))));
        }

        var candidates = await candidatesQuery.Take(500).ToListAsync();
        var ranked = candidates
            .Select(user =>
            {
                var matchingExperience = user.Experiences.FirstOrDefault(item =>
                    term.Length > 0 &&
                    (Normalize(item.CompanyName).Contains(term) || Normalize(item.Title).Contains(term)));
                var matchingEducation = user.Educations.FirstOrDefault(item =>
                    term.Length > 0 &&
                    (Normalize(item.School).Contains(term) || Normalize(item.Field).Contains(term)));
                var matchingSkill = user.Skills.FirstOrDefault(item =>
                    term.Length > 0 && Normalize(item.Name).Contains(term));

                var score = 0;
                if (term.Length > 0 && (Normalize(user.FullName) == term || Normalize(user.UserName) == term)) score += 100;
                if (term.Length > 0 && (Normalize(user.FullName).StartsWith(term) || Normalize(user.UserName).StartsWith(term))) score += 55;
                if (matchingExperience != null) score += 45;
                if (matchingEducation != null) score += 40;
                if (matchingSkill != null) score += 35;
                if (term.Length > 0 && Normalize(user.CurrentPosition).Contains(term)) score += 30;

                var reason = matchingExperience != null
                    ? $"Works/has worked at {matchingExperience.CompanyName}"
                    : matchingEducation != null
                        ? $"Studied at {matchingEducation.School}"
                        : matchingSkill != null
                            ? $"Skill: {matchingSkill.Name}"
                            : null;

                return new { User = user, Score = score, RelationReason = reason };
            })
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.User.FullName)
            .ToList();

        var totalCount = ranked.Count;
        var items = ranked.Skip((page - 1) * pageSize).Take(pageSize).Select(item => new
        {
            item.User.Id,
            Username = item.User.UserName,
            item.User.FullName,
            item.User.CurrentPosition,
            item.User.ProfileImage,
            item.User.Bio,
            item.User.Location,
            item.RelationReason,
            relevanceScore = item.Score
        }).ToList();

        return Ok(Page(items, page, pageSize, totalCount));
    }

    [HttpGet("companies")]
    public async Task<IActionResult> Companies(
        [FromQuery] string? query = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 6)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var term = Normalize(query);
        NormalizePaging(ref page, ref pageSize);

        var source = _context.Set<Linkedin.Core.Entities.Company>()
            .AsNoTracking()
            .Where(company => company.User != null && !company.User.IsBlocked);

        if (term.Length > 0)
        {
            source = source.Where(company =>
                company.Name.ToLower().Contains(term) ||
                (company.Industry != null && company.Industry.ToLower().Contains(term)) ||
                (company.Location != null && company.Location.ToLower().Contains(term)) ||
                (company.Bio != null && company.Bio.ToLower().Contains(term)));
        }

        var totalCount = await source.CountAsync();
        var items = await source
            .OrderByDescending(company => term.Length > 0 && company.Name.ToLower() == term)
            .ThenByDescending(company => term.Length > 0 && company.Name.ToLower().StartsWith(term))
            .ThenByDescending(company => _context.CompanyFollows.Count(follow => follow.EmployerId == company.UserId))
            .ThenBy(company => company.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(company => new
            {
                company.Id,
                company.Name,
                username = company.User!.UserName,
                logoUrl = company.LogoUrl ?? company.User.ProfileImage,
                company.Industry,
                company.Location,
                company.Bio,
                company.IsVerified,
                followerCount = _context.CompanyFollows.Count(follow => follow.EmployerId == company.UserId),
                isFollowing = _context.CompanyFollows.Any(follow =>
                    follow.EmployerId == company.UserId && follow.FollowerId == currentUserId)
            })
            .ToListAsync();

        return Ok(Page(items, page, pageSize, totalCount));
    }

    [HttpGet("jobs")]
    public async Task<IActionResult> Jobs(
        [FromQuery] string? query = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 6)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var term = Normalize(query);
        NormalizePaging(ref page, ref pageSize);
        var now = DateTime.UtcNow;

        var currentUser = await _context.Users.AsNoTracking()
            .Include(user => user.Skills)
            .FirstOrDefaultAsync(user => user.Id == currentUserId);
        if (currentUser == null) return Unauthorized();

        var skillTerms = currentUser.Skills.Select(skill => Normalize(skill.Name)).Where(value => value.Length > 1).ToList();
        var positionTerms = Words(currentUser.CurrentPosition);
        var locationTerms = Words(currentUser.Location);

        var jobsQuery = _context.JobPosts.AsNoTracking()
            .Include(job => job.Employer).ThenInclude(employer => employer.Company)
            .Where(job => !job.IsBlocked && job.IsActive &&
                (!job.ExpiresAt.HasValue || job.ExpiresAt > now) && !job.Employer.IsBlocked);

        if (term.Length > 0)
        {
            jobsQuery = jobsQuery.Where(job =>
                job.Title.ToLower().Contains(term) ||
                job.Description.ToLower().Contains(term) ||
                (job.Location != null && job.Location.ToLower().Contains(term)) ||
                (job.Employer.Company != null && job.Employer.Company.Name.ToLower().Contains(term)) ||
                (job.Employer.Company != null && job.Employer.Company.Industry != null &&
                 job.Employer.Company.Industry.ToLower().Contains(term)));
        }

        var candidates = await jobsQuery.OrderByDescending(job => job.CreatedAt).Take(500).ToListAsync();
        var ranked = candidates.Select(job =>
        {
            var text = Normalize($"{job.Title} {job.Description} {job.Location} {job.WorkplaceType} " +
                                 $"{job.EmploymentType} {job.Employer.Company?.Name} {job.Employer.Company?.Industry}");
            var queryMatch = term.Length > 0 && text.Contains(term);
            var skillMatches = skillTerms.Count(text.Contains);
            var positionMatch = positionTerms.Any(text.Contains);
            var locationMatch = locationTerms.Any(text.Contains);
            var score = (queryMatch ? 100 : 0) + skillMatches * 35 + (positionMatch ? 40 : 0) +
                        (locationMatch ? 20 : 0) + (job.CreatedAt >= now.AddDays(-7) ? 10 : 0);
            var reasons = new List<string>();
            if (skillMatches > 0) reasons.Add($"{skillMatches} skill match");
            if (positionMatch) reasons.Add("position match");
            if (locationMatch) reasons.Add("location match");
            return new { Job = job, Score = score, MatchReason = string.Join(" · ", reasons) };
        }).OrderByDescending(item => item.Score).ThenByDescending(item => item.Job.CreatedAt).ToList();

        var totalCount = ranked.Count;
        var pageJobs = ranked.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var jobIds = pageJobs.Select(item => item.Job.Id).ToList();
        var savedIds = await _context.SavedJobs.AsNoTracking()
            .Where(item => item.UserId == currentUserId && jobIds.Contains(item.JobPostId))
            .Select(item => item.JobPostId).ToListAsync();
        var appliedIds = await _context.JobApplications.AsNoTracking()
            .Where(item => item.ApplicantId == currentUserId && jobIds.Contains(item.JobPostId))
            .Select(item => item.JobPostId).ToListAsync();

        var items = pageJobs.Select(item => new
        {
            item.Job.Id,
            item.Job.Title,
            item.Job.Description,
            item.Job.Location,
            item.Job.WorkplaceType,
            item.Job.EmploymentType,
            item.Job.CreatedAt,
            item.Job.ExpiresAt,
            companyName = item.Job.Employer.Company?.Name ?? item.Job.Employer.FullName,
            companyLogo = item.Job.Employer.Company?.LogoUrl ?? item.Job.Employer.ProfileImage,
            companyUsername = item.Job.Employer.UserName,
            isSaved = savedIds.Contains(item.Job.Id),
            isApplied = appliedIds.Contains(item.Job.Id),
            canApply = item.Job.IsActive && (!item.Job.ExpiresAt.HasValue || item.Job.ExpiresAt > now),
            relevanceScore = item.Score,
            item.MatchReason
        }).ToList();

        return Ok(Page(items, page, pageSize, totalCount));
    }

    [HttpGet("hashtags")]
    public async Task<IActionResult> Hashtags([FromQuery] string? query = null, [FromQuery] int take = 5)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        take = Math.Clamp(take, 1, 10);
        var term = Normalize(query).TrimStart('#');
        var user = await _context.Users.AsNoTracking().Include(item => item.Skills)
            .FirstOrDefaultAsync(item => item.Id == currentUserId);
        var interests = user?.Skills.Select(skill => Normalize(skill.Name)).ToList() ?? new List<string>();
        interests.AddRange(Words(user?.CurrentPosition));
        interests.AddRange(await _context.SearchHistories.AsNoTracking()
            .Where(item => item.UserId == currentUserId)
            .OrderByDescending(item => item.CreatedAt).Take(8)
            .Select(item => item.NormalizedQuery).ToListAsync());

        var contents = await _context.Posts.AsNoTracking()
            .Where(post => !post.IsBlocked && post.ModerationStatus == PostModerationStatus.Published && post.Content != null)
            .OrderByDescending(post => post.CreatedAt).Select(post => post.Content!).Take(300).ToListAsync();
        var pattern = new Regex(@"(?<![\p{L}\p{N}_])#([\p{L}\p{N}_-]+)", RegexOptions.IgnoreCase);
        var tags = contents.SelectMany(content => pattern.Matches(content).Cast<Match>().Select(match => match.Groups[1].Value))
            .GroupBy(tag => tag, StringComparer.OrdinalIgnoreCase)
            .Select(group => new
            {
                name = group.Key,
                postCount = group.Count(),
                score = group.Count() * 5 +
                        (interests.Any(interest => interest.Length > 1 && Normalize(group.Key).Contains(interest)) ? 50 : 0) +
                        (term.Length > 0 && Normalize(group.Key).Contains(term) ? 30 : 0)
            })
            .OrderByDescending(item => item.score).ThenByDescending(item => item.postCount).ThenBy(item => item.name)
            .Take(take).ToList();

        return Ok(tags);
    }

    [HttpGet("events")]
    public async Task<IActionResult> Events(
        [FromQuery] string? query = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 6,
        [FromQuery] bool recommended = true)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var term = Normalize(query);
        NormalizePaging(ref page, ref pageSize);
        var now = DateTime.UtcNow;

        var user = await _context.Users.AsNoTracking().Include(item => item.Skills)
            .FirstOrDefaultAsync(item => item.Id == currentUserId);
        var interests = user?.Skills.Select(skill => Normalize(skill.Name)).Where(value => value.Length > 1).ToList()
                        ?? new List<string>();
        interests.AddRange(Words(user?.CurrentPosition));
        var followedEmployerIds = await _context.CompanyFollows.AsNoTracking()
            .Where(item => item.FollowerId == currentUserId)
            .Select(item => item.EmployerId).ToListAsync();

        var source = _context.Events.AsNoTracking()
            .Include(item => item.Employer).ThenInclude(employer => employer.Company)
            .Include(item => item.Attendees)
            .Where(item => !item.Employer.IsBlocked);
        if (term.Length > 0)
        {
            source = source.Where(item =>
                item.Title.ToLower().Contains(term) ||
                (item.Description != null && item.Description.ToLower().Contains(term)) ||
                (item.Location != null && item.Location.ToLower().Contains(term)) ||
                (item.Employer.Company != null && item.Employer.Company.Name.ToLower().Contains(term)));
        }

        var candidates = await source.Take(500).ToListAsync();
        var ranked = candidates.Select(item =>
        {
            var text = Normalize($"{item.Title} {item.Description} {item.Location} " +
                                 $"{item.Employer.Company?.Name} {item.Employer.Company?.Industry}");
            var score = 0;
            if (term.Length > 0 && Normalize(item.Title) == term) score += 130;
            else if (term.Length > 0 && Normalize(item.Title).StartsWith(term)) score += 105;
            else if (term.Length > 0 && text.Contains(term)) score += 80;
            if (recommended && interests.Any(text.Contains)) score += 45;
            if (recommended && followedEmployerIds.Contains(item.EmployerId)) score += 25;
            if (item.StartsAt >= now) score += 20;
            score += Math.Min(item.Attendees.Count, 20);
            return new { Event = item, Score = score };
        }).OrderByDescending(item => item.Score)
          .ThenBy(item => item.Event.StartsAt >= now ? item.Event.StartsAt : DateTime.MaxValue)
          .ThenByDescending(item => item.Event.StartsAt)
          .ToList();

        var totalCount = ranked.Count;
        var items = ranked.Skip((page - 1) * pageSize).Take(pageSize).Select(item => new
        {
            item.Event.Id,
            item.Event.Title,
            item.Event.Description,
            item.Event.Location,
            item.Event.ImageUrl,
            item.Event.StartsAt,
            username = item.Event.Employer.UserName,
            companyName = item.Event.Employer.Company?.Name ?? item.Event.Employer.FullName,
            attendeeCount = item.Event.Attendees.Count,
            isAttending = item.Event.Attendees.Any(attendee => attendee.UserId == currentUserId),
            isOwner = item.Event.EmployerId == currentUserId,
            relevanceScore = item.Score
        }).ToList();

        return Ok(Page(items, page, pageSize, totalCount));
    }

    private static object Page<T>(List<T> items, int page, int pageSize, int totalCount)
    {
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        return new { items, page, pageSize, totalCount, totalPages, hasMore = page < totalPages };
    }

    private static void NormalizePaging(ref int page, ref int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 20);
    }

    private static string Normalize(string? value) => value?.Trim().ToLowerInvariant() ?? string.Empty;

    private static List<string> Words(string? value) => Normalize(value)
        .Split(new[] { ' ', ',', '.', ';', ':', '-', '_', '/', '\\', '(', ')', '[', ']' }, StringSplitOptions.RemoveEmptyEntries)
        .Where(word => word.Length > 1).Distinct().ToList();
}
