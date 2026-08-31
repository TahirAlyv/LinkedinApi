using System.Security.Claims;
using Linkedin.Core.Data;
using Linkedin.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Linkedin.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class CompanyController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CompanyController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            var employerId = CurrentUserId();
            var employer = await _context.Users.AsNoTracking()
                .Where(item =>
                    item.Id == employerId &&
                    item.UserType == UserType.Employer &&
                    !item.IsBlocked)
                .Select(item => new
                {
                    item.Id,
                    Username = item.UserName,
                    Name = item.Company != null
                        ? item.Company.Name
                        : item.FullName,
                    Logo = item.Company != null
                        ? item.Company.LogoUrl
                        : item.ProfileImage
                })
                .FirstOrDefaultAsync();

            if (employer == null)
                return Forbid();

            var now = DateTime.UtcNow;
            var from = now.AddDays(-30);

            var profileViews = await _context.AnalyticsEvents.AsNoTracking()
                .CountAsync(item =>
                    item.TargetUserId == employerId &&
                    item.EventType == AnalyticsEventType.ProfileView &&
                    item.CreatedAt >= from);

            var followers = await _context.CompanyFollows.AsNoTracking()
                .CountAsync(item => item.EmployerId == employerId);

            var activeJobs = await _context.JobPosts.AsNoTracking()
                .CountAsync(item =>
                    item.EmployerId == employerId &&
                    item.IsActive &&
                    (!item.ExpiresAt.HasValue || item.ExpiresAt > now) &&
                    !item.IsBlocked);

            var applicationLinkClicks = await _context.AnalyticsEvents.AsNoTracking()
                .CountAsync(item =>
                    item.TargetUserId == employerId &&
                    item.EventType == AnalyticsEventType.JobApplyClick);

            var jobViews = await _context.AnalyticsEvents.AsNoTracking()
                .CountAsync(item =>
                    item.TargetUserId == employerId &&
                    item.EventType == AnalyticsEventType.JobView &&
                    item.CreatedAt >= from);

            var jobSaves = await _context.SavedJobs.AsNoTracking()
                .CountAsync(item =>
                    item.JobPost.EmployerId == employerId);

            var sentInvitations = await _context.JobInvitations.AsNoTracking()
                .CountAsync(item => item.EmployerId == employerId);

            var activeRequiredSkills = await _context.JobPosts.AsNoTracking()
                .Where(item =>
                    item.EmployerId == employerId &&
                    item.IsActive &&
                    !item.IsBlocked &&
                    item.RequiredSkills != null)
                .Select(item => item.RequiredSkills!)
                .ToListAsync();
            var requiredSkillSet = activeRequiredSkills
                .SelectMany(value => value.Split(
                    '|',
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var matchingTalent = requiredSkillSet.Count == 0
                ? 0
                : await _context.Users.AsNoTracking()
                    .CountAsync(item =>
                        item.UserType == UserType.JobSeeker &&
                        !item.IsBlocked &&
                        _context.JobPreferences.Any(preference =>
                            preference.UserId == item.Id &&
                            preference.IsOpenToWork) &&
                        item.Skills.Any(skill =>
                            requiredSkillSet.Contains(skill.Name)));

            var upcomingEvents = await _context.Events.AsNoTracking()
                .Where(item =>
                    item.EmployerId == employerId &&
                    item.StartsAt >= now)
                .OrderBy(item => item.StartsAt)
                .Select(item => new
                {
                    item.Id,
                    item.Title,
                    item.StartsAt,
                    item.Location,
                    attendeeCount = item.Attendees.Count
                })
                .Take(5)
                .ToListAsync();

            var attendeeCount = await _context.EventAttendances.AsNoTracking()
                .CountAsync(item => item.EventItem.EmployerId == employerId);

            var postIds = await _context.Posts.AsNoTracking()
                .Where(item =>
                    item.UserID == employerId &&
                    !item.IsBlocked &&
                    item.ModerationStatus == PostModerationStatus.Published)
                .Select(item => item.Id)
                .ToListAsync();

            var postLikes = await _context.Likes.AsNoTracking()
                .CountAsync(item =>
                    postIds.Contains(item.PostId) &&
                    item.isLiked &&
                    item.CreatedAt >= from);

            var postComments = await _context.Comments.AsNoTracking()
                .CountAsync(item =>
                    postIds.Contains(item.PostId) &&
                    item.CreatedAt >= from);

            var recentJobs = await _context.JobPosts.AsNoTracking()
                .Where(item => item.EmployerId == employerId && !item.IsBlocked)
                .OrderByDescending(item => item.CreatedAt)
                .Select(item => new
                {
                    item.Id,
                    item.Title,
                    item.IsActive,
                    item.ExpiresAt,
                    applicationLinkClicks = _context.AnalyticsEvents.Count(analytics =>
                        analytics.JobPostId == item.Id &&
                        analytics.EventType == AnalyticsEventType.JobApplyClick)
                })
                .Take(5)
                .ToListAsync();

            return Ok(new
            {
                company = employer,
                periodDays = 30,
                metrics = new
                {
                    profileViews,
                    followers,
                    activeJobs,
                    applicationLinkClicks,
                    jobViews,
                    jobSaves,
                    sentInvitations,
                    matchingTalent,
                    upcomingEvents = upcomingEvents.Count,
                    attendeeCount,
                    postEngagement = postLikes + postComments,
                    postLikes,
                    postComments
                },
                upcomingEventItems = upcomingEvents,
                recentJobs
            });
        }

        [HttpGet("{username}/community")]
        [HttpGet("{username}/posts")]
        public async Task<IActionResult> Community(
            string username,
            [FromQuery] string type = "all",
            [FromQuery] string sort = "latest",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 30);

            var company = await FindCompanyAsync(username);
            if (company == null)
                return NotFound("Company was not found.");

            var currentUserId = CurrentUserId();
            var legacyMentionPattern = $"%@{company.Username}%";
            var query = _context.Posts.AsNoTracking()
                .Where(item =>
                    (item.UserID == company.UserId ||
                     item.MentionedCompanyId == company.CompanyId ||
                     (item.MentionedCompanyId == null &&
                      item.Content != null &&
                      EF.Functions.Like(
                          item.Content,
                          legacyMentionPattern))) &&
                    !item.IsBlocked &&
                    !item.User.IsBlocked &&
                    item.ModerationStatus == PostModerationStatus.Published);

            if (string.Equals(
                    type,
                    "official",
                    StringComparison.OrdinalIgnoreCase))
            {
                type = "official";
                query = query.Where(item =>
                    item.UserID == company.UserId);
            }
            else if (string.Equals(
                         type,
                         "mentions",
                         StringComparison.OrdinalIgnoreCase))
            {
                type = "mentions";
                query = query.Where(item =>
                    item.UserID != company.UserId &&
                    (item.MentionedCompanyId == company.CompanyId ||
                     (item.MentionedCompanyId == null &&
                      item.Content != null &&
                      EF.Functions.Like(
                          item.Content,
                          legacyMentionPattern))));
            }
            else
            {
                type = "all";
            }

            if (string.Equals(sort, "popular", StringComparison.OrdinalIgnoreCase))
            {
                var popularFrom = DateTime.UtcNow.AddDays(-30);
                query = query
                    .Where(item => item.CreatedAt >= popularFrom)
                    .OrderByDescending(item =>
                        item.Likes.Count(like => like.isLiked) +
                        (item.Comments.Count * 2))
                    .ThenByDescending(item => item.CreatedAt);
            }
            else
            {
                sort = "latest";
                query = query.OrderByDescending(item => item.CreatedAt);
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(item => new
                {
                    item.Id,
                    postOwnerId = item.UserID,
                    username = item.User.UserName,
                    userPhoto =
                        item.User.UserType == UserType.Employer &&
                        item.User.Company != null
                            ? item.User.Company.LogoUrl
                            : item.User.ProfileImage,
                    role = item.User.UserType.ToString(),
                    item.Content,
                    item.ImageUrl,
                    item.VideoUrl,
                    item.CreatedAt,
                    commentCount = item.Comments.Count,
                    likeCount = item.Likes.Count(like => like.isLiked),
                    isLikedByCurrentUser = item.Likes.Any(like =>
                        like.UserId == currentUserId && like.isLiked),
                    isSaved = item.Id > 0 && _context.SavedPosts.Any(saved =>
                        saved.PostId == item.Id &&
                        saved.UserId == currentUserId),
                    mentionedCompanyId = item.MentionedCompanyId,
                    mentionedCompanyName = item.MentionedCompany != null
                        ? item.MentionedCompany.Name
                        : null,
                    mentionedCompanyUsername =
                        item.MentionedCompany != null &&
                        item.MentionedCompany.User != null
                            ? item.MentionedCompany.User.UserName
                            : null,
                    canManage = item.UserID == currentUserId,
                    isOfficial = item.UserID == company.UserId,
                    popularityScore =
                        item.Likes.Count(like => like.isLiked) +
                        (item.Comments.Count * 2)
                })
                .ToListAsync();

            var totalPages = Math.Max(
                1,
                (int)Math.Ceiling(totalCount / (double)pageSize));

            return Ok(new
            {
                company = new
                {
                    id = company.CompanyId,
                    company.UserId,
                    company.Username,
                    company.Name
                },
                sort,
                type,
                items,
                page,
                pageSize,
                totalCount,
                totalPages,
                hasMore = page < totalPages
            });
        }

        [HttpGet("{username}/overview")]
        public async Task<IActionResult> Overview(string username)
        {
            var company = await _context.Users.AsNoTracking()
                .Where(item =>
                    item.UserName == username &&
                    item.UserType == UserType.Employer &&
                    item.Company != null &&
                    !item.IsBlocked)
                .Select(item => new
                {
                    companyId = item.Company!.Id,
                    userId = item.Id,
                    username = item.UserName,
                    name = item.Company.Name,
                    item.Company.Tagline,
                    item.Company.Industry,
                    item.Company.Bio,
                    item.Company.Website,
                    item.Company.Location,
                    logoUrl = item.Company.LogoUrl,
                    item.BackgroundImage,
                    item.Company.CompanySize,
                    item.Company.FoundedYear,
                    item.Company.IsVerified,
                    followers = _context.CompanyFollows.Count(follow =>
                        follow.EmployerId == item.Id),
                    employees = _context.Experience
                        .Where(experience =>
                            experience.CompanyId == item.Company.Id &&
                            experience.IsCurrent &&
                            !experience.User.IsBlocked)
                        .Select(experience => experience.UserId)
                        .Distinct()
                        .Count(),
                    activeJobs = _context.JobPosts.Count(job =>
                        job.EmployerId == item.Id &&
                        job.IsActive &&
                        !job.IsBlocked &&
                        (!job.ExpiresAt.HasValue ||
                         job.ExpiresAt > DateTime.UtcNow)),
                    upcomingEvents = _context.Events.Count(eventItem =>
                        eventItem.EmployerId == item.Id &&
                        eventItem.StartsAt > DateTime.UtcNow)
                })
                .FirstOrDefaultAsync();

            if (company == null)
                return NotFound("Company was not found.");

            var legacyMentionPattern = $"%@{company.username}%";
            var popularMentions = await _context.Posts.AsNoTracking()
                .Where(item =>
                    (item.MentionedCompanyId == company.companyId ||
                     (item.MentionedCompanyId == null &&
                      item.Content != null &&
                      EF.Functions.Like(
                          item.Content,
                          legacyMentionPattern))) &&
                    item.UserID != company.userId &&
                    !item.IsBlocked &&
                    !item.User.IsBlocked &&
                    item.ModerationStatus ==
                    PostModerationStatus.Published)
                .OrderByDescending(item =>
                    item.Likes.Count(like => like.isLiked) +
                    (item.Comments.Count * 2))
                .ThenByDescending(item => item.CreatedAt)
                .Take(3)
                .Select(item => new
                {
                    item.Id,
                    username = item.User.UserName,
                    item.User.FullName,
                    profileImage = item.User.ProfileImage,
                    item.Content,
                    item.ImageUrl,
                    item.VideoUrl,
                    item.CreatedAt,
                    likeCount = item.Likes.Count(like => like.isLiked),
                    commentCount = item.Comments.Count,
                    popularityScore =
                        item.Likes.Count(like => like.isLiked) +
                        (item.Comments.Count * 2)
                })
                .ToListAsync();

            return Ok(new
            {
                company,
                popularMentions
            });
        }

        [HttpGet("{username}/people")]
        public async Task<IActionResult> People(
            string username,
            [FromQuery] int take = 6)
        {
            take = Math.Clamp(take, 1, 20);
            var company = await FindCompanyAsync(username);

            if (company == null)
                return NotFound("Company was not found.");

            var currentUserId = CurrentUserId();
            var currentSkills = await _context.userSkills.AsNoTracking()
                .Where(item => item.UserId == currentUserId)
                .Select(item => item.Name)
                .ToListAsync();

            var skillSet = currentSkills
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var employeeIds = await _context.Experience.AsNoTracking()
                .Where(item =>
                    item.CompanyId == company.CompanyId &&
                    item.IsCurrent &&
                    !item.User.IsBlocked)
                .Select(item => item.UserId)
                .Distinct()
                .ToListAsync();

            var employees = await _context.Users.AsNoTracking()
                .Where(item =>
                    employeeIds.Contains(item.Id) &&
                    item.Id != currentUserId &&
                    !item.IsBlocked)
                .Include(item => item.Skills)
                .ToListAsync();

            var items = employees
                .Select(employee =>
                {
                    var sharedSkills = employee.Skills
                        .Select(skill => skill.Name)
                        .Where(skill =>
                            !string.IsNullOrWhiteSpace(skill) &&
                            skillSet.Contains(skill))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Take(3)
                        .ToList();

                    return new
                    {
                        employee.Id,
                        username = employee.UserName,
                        employee.FullName,
                        employee.CurrentPosition,
                        profileImage = employee.ProfileImage,
                        worksAt = company.Name,
                        sharedSkillCount = sharedSkills.Count,
                        sharedSkills
                    };
                })
                .OrderByDescending(item => item.sharedSkillCount)
                .ThenBy(item => item.FullName)
                .Take(take)
                .ToList();

            return Ok(items);
        }

        private async Task<CompanyLookup?> FindCompanyAsync(string username)
        {
            var cleanUsername = username.Trim();

            return await _context.Users.AsNoTracking()
                .Where(item =>
                    item.UserName == cleanUsername &&
                    item.UserType == UserType.Employer &&
                    item.Company != null &&
                    !item.IsBlocked)
                .Select(item => new CompanyLookup(
                    item.Company!.Id,
                    item.Id,
                    item.UserName!,
                    item.Company.Name))
                .FirstOrDefaultAsync();
        }

        private string CurrentUserId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User id claim is missing.");

        private sealed record CompanyLookup(
            int CompanyId,
            string UserId,
            string Username,
            string Name);
    }
}
