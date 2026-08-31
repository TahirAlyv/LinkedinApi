using Linkedin.Core.Data;
using Linkedin.Core.Entities;
using Linkedin.Core.Enums;
using Linkedin.Business.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Security.Claims;

namespace Linkedin.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Moderator")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotficationsService _notificationService;
        private readonly IEmailService _emailService;

        public AdminController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            INotficationsService notificationService,
            IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
            _emailService = emailService;
        }
 
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var now = DateTime.UtcNow;
            var sevenDaysAgo = now.AddDays(-7);
            var monthStart = new DateTime(now.Year, now.Month, 1).AddMonths(-11);

            var totalUsers = await _context.Users.CountAsync(
                user => user.UserType != Linkedin.Core.Enums.UserType.Staff);
            var totalJobSeekers = await _context.Users.CountAsync(user =>
                user.UserType == Linkedin.Core.Enums.UserType.JobSeeker);
            var totalEmployers = await _context.Users.CountAsync(user =>
                user.UserType == Linkedin.Core.Enums.UserType.Employer);
            var newUsersLast7Days = await _context.Users.CountAsync(user =>
                user.UserType != Linkedin.Core.Enums.UserType.Staff &&
                user.CreatedAt >= sevenDaysAgo);
            var totalPosts = await _context.Posts.CountAsync();
            var newPostsLast7Days = await _context.Posts.CountAsync(post =>
                post.CreatedAt >= sevenDaysAgo);
            var publishedPosts = await _context.Posts.CountAsync(post =>
                post.ModerationStatus == Linkedin.Core.Enums.PostModerationStatus.Published &&
                !post.IsBlocked);
            var rejectedPosts = await _context.Posts.CountAsync(post =>
                post.ModerationStatus == Linkedin.Core.Enums.PostModerationStatus.Rejected);
            var totalJobPosts = await _context.JobPosts.CountAsync();
            var activeJobPosts = await _context.JobPosts.CountAsync(job =>
                job.IsActive &&
                !job.IsBlocked &&
                (!job.ExpiresAt.HasValue || job.ExpiresAt > now));
            var totalReports = await _context.Reports.CountAsync() +
                await _context.ProfileReports.CountAsync();
            var pendingAiReview = await _context.Posts.CountAsync(p =>
                p.ModerationStatus == Linkedin.Core.Enums.PostModerationStatus.PendingReview);
            var openPostReports = await _context.Reports.CountAsync(report => !report.IsReviewed);
            var openProfileReports = await _context.ProfileReports.CountAsync(report => !report.IsReviewed);
            var openReports = openPostReports + openProfileReports;

            var blockedUsers = await _context.Users.CountAsync(u =>
                u.UserType != Linkedin.Core.Enums.UserType.Staff &&
                u.IsBlocked);
            var blockedPosts = await _context.Posts.CountAsync(p => p.IsBlocked);
            var blockedJobPosts = await _context.JobPosts.CountAsync(j => j.IsBlocked);

            var monthlyRegistrations = await _context.Users
                .Where(user =>
                    user.UserType != Linkedin.Core.Enums.UserType.Staff &&
                    user.CreatedAt >= monthStart)
                .GroupBy(u => new
                {
                    u.CreatedAt.Year,
                    u.CreatedAt.Month
                })
                .Select(g => new
                {
                    year = g.Key.Year,
                    month = g.Key.Month,
                    count = g.Count()
                })
                .OrderBy(x => x.year)
                .ThenBy(x => x.month)
                .ToListAsync();

            return Ok(new
            {
                totalUsers,
                totalJobSeekers,
                totalEmployers,
                newUsersLast7Days,
                totalPosts,
                newPostsLast7Days,
                publishedPosts,
                rejectedPosts,
                totalJobPosts,
                activeJobPosts,
                totalReports,
                pendingAiReview,
                openReports,
                openPostReports,
                openProfileReports,
                blockedUsers,
                blockedPosts,
                blockedJobPosts,
                monthlyRegistrations
            });
        }

 
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users
                .Where(user => user.UserType != Linkedin.Core.Enums.UserType.Staff)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            var result = new List<object>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                result.Add(new
                {
                    id = user.Id,
                    fullName = user.FullName,
                    username = user.UserName,
                    email = user.Email,
                    profileImage = user.ProfileImage,
                    userType = user.UserType.ToString(),
                    roles = roles,
                    createdAt = user.CreatedAt,
                    isBlocked = user.IsBlocked,
                    blockReason = user.BlockReason
                });
            }

            return Ok(result);
        }

 
        [HttpPost("users/{userId}/block")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> BlockUser(string userId, [FromBody] AdminReasonDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.Id == userId &&
                u.UserType != Linkedin.Core.Enums.UserType.Staff);

            if (user == null)
                return NotFound(new { message = "User not found." });

            var reason = dto?.Reason?.Trim();
            if (string.IsNullOrWhiteSpace(reason))
                return BadRequest(new { message = "A restriction reason is required." });

            user.IsBlocked = true;
            user.BlockReason = reason;

            await _context.SaveChangesAsync();

            var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            await _notificationService.CreateOrUpdateAsync(
                actorId, user.Id, NotificationType.SystemAccountRestricted,
                null, $"Account restricted: {reason}", "Nexora System", "");

            var emailSent = await TrySendRestrictionEmailAsync(
                user,
                "Your Nexora account has been restricted",
                "Account restriction",
                reason);

            return Ok(new
            {
                message = "User blocked successfully.",
                userId = user.Id,
                isBlocked = user.IsBlocked,
                blockReason = user.BlockReason,
                emailSent
            });
        }

        [HttpGet("users/{userId}/details")]
        public async Task<IActionResult> GetUserDetails(string userId)
        {
            var user = await _context.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId && u.UserType != UserType.Staff);
            if (user == null) return NotFound(new { message = "User not found." });

            var posts = await _context.Posts.AsNoTracking()
                .Where(p => p.UserID == userId)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new
                {
                    id = p.Id, content = p.Content, imageUrl = p.ImageUrl,
                    videoUrl = p.VideoUrl, createdAt = p.CreatedAt,
                    isBlocked = p.IsBlocked, blockReason = p.BlockReason,
                    moderationStatus = p.ModerationStatus.ToString(),
                    commentCount = p.CommentCount, likeCount = p.LikeCount
                }).ToListAsync();

            var jobs = await _context.JobPosts.AsNoTracking()
                .Where(j => j.EmployerId == userId)
                .OrderByDescending(j => j.CreatedAt)
                .Select(j => new
                {
                    id = j.Id, title = j.Title, description = j.Description,
                    createdAt = j.CreatedAt, isActive = j.IsActive,
                    isBlocked = j.IsBlocked, blockReason = j.BlockReason
                }).ToListAsync();

            var profileReports = await _context.ProfileReports.AsNoTracking()
                .Where(r => r.ReportedUserId == userId)
                .Include(r => r.Reporter)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    id = r.Id, category = r.Category, details = r.Details,
                    severity = r.Severity, isReviewed = r.IsReviewed,
                    createdAt = r.CreatedAt, reviewedAt = r.ReviewedAt,
                    reporterName = r.Reporter.FullName,
                    reporterUsername = r.Reporter.UserName
                }).ToListAsync();

            var postReports = await _context.Reports.AsNoTracking()
                .Where(r => r.Post.UserID == userId)
                .Include(r => r.Reporter)
                .Include(r => r.Post)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    id = r.Id, reason = r.Reason, isReviewed = r.IsReviewed,
                    createdAt = r.CreatedAt, reporterName = r.Reporter.FullName,
                    reporterUsername = r.Reporter.UserName,
                    postId = r.PostId, postContent = r.Post.Content,
                    postIsBlocked = r.Post.IsBlocked
                }).ToListAsync();

            var followerCount = await _context.CompanyFollows.AsNoTracking()
                .CountAsync(follow => follow.EmployerId == userId);
            var connectionCount = await _context.Connections.AsNoTracking()
                .CountAsync(connection => connection.UserId == userId || connection.ConnectedUserId == userId);
            var uniqueReporters = profileReports.Select(r => r.reporterUsername)
                .Concat(postReports.Select(r => r.reporterUsername))
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();
            var openProfileReports = profileReports.Count(r => !r.isReviewed);
            var openPostReports = postReports.Count(r => !r.isReviewed);
            var riskScore = Math.Min(100,
                uniqueReporters * 6 +
                openProfileReports * 7 +
                openPostReports * 5 +
                posts.Count(p => p.isBlocked) * 8);

            var skills = await _context.userSkills.AsNoTracking()
                .Where(s => s.UserId == userId).Select(s => s.Name).ToListAsync();
            var experiences = await _context.Experience.AsNoTracking()
                .Where(e => e.UserId == userId)
                .Select(e => new { e.Id, e.Title, e.CompanyName, e.EmploymentType, e.IsCurrent, e.StartMonth, e.StartYear, e.EndMonth, e.EndYear, e.Location, e.Description })
                .ToListAsync();
            var educations = await _context.Education.AsNoTracking()
                .Where(e => e.UserId == userId)
                .Select(e => new { e.Id, e.School, e.Degree, e.Field, e.StartYear, e.EndYear, e.Note })
                .ToListAsync();

            return Ok(new
            {
                id = user.Id, fullName = user.FullName, username = user.UserName,
                email = user.Email, phoneNumber = user.PhoneNumber,
                profileImage = user.ProfileImage, backgroundImage = user.BackgroundImage,
                userType = user.UserType.ToString(), createdAt = user.CreatedAt,
                currentPosition = user.CurrentPosition, location = user.Location,
                address = user.Address, website = user.Website, bio = user.Bio,
                isBlocked = user.IsBlocked, blockReason = user.BlockReason,
                metrics = new
                {
                    postCount = posts.Count,
                    blockedPostCount = posts.Count(p => p.isBlocked),
                    jobCount = jobs.Count,
                    reportCount = profileReports.Count + postReports.Count,
                    openReportCount = openProfileReports + openPostReports,
                    followerCount,
                    connectionCount,
                    totalEngagement = posts.Sum(p => (p.likeCount ?? 0) + (p.commentCount ?? 0)),
                    uniqueReporters,
                    riskScore,
                    riskLevel = riskScore >= 50 ? "High" : riskScore >= 25 ? "Medium" : "Low"
                },
                posts, jobs, profileReports, postReports, skills, experiences, educations
            });
        }

 
        [HttpPost("users/{userId}/unblock")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UnblockUser(string userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.Id == userId &&
                u.UserType != Linkedin.Core.Enums.UserType.Staff);

            if (user == null)
                return NotFound(new { message = "User not found." });

            user.IsBlocked = false;
            user.BlockReason = null;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "User unblocked successfully.",
                userId = user.Id,
                isBlocked = user.IsBlocked
            });
        }
 
        [HttpGet("posts")]
        public async Task<IActionResult> GetPosts()
        {
            var posts = await _context.Posts
                .Include(p => p.User)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new
                {
                    id = p.Id,
                    content = p.Content,
                    imageUrl = p.ImageUrl,
                    videoUrl = p.VideoUrl,
                    createdAt = p.CreatedAt,
                    isBlocked = p.IsBlocked,
                    blockReason = p.BlockReason,
                    moderationStatus = p.ModerationStatus.ToString(),
                    isAiFlagged = p.IsAiFlagged,
                    aiModerationRiskLevel = p.AiModerationRiskLevel,
                    aiModerationCategories = p.AiModerationCategories,
                    aiModerationReason = p.AiModerationReason,
                    aiModerationCheckedAt = p.AiModerationCheckedAt,
                    authorId = p.UserID,
                    authorName = p.User.FullName,
                    authorUsername = p.User.UserName,
                    authorProfileImage = p.User.ProfileImage
                })
                .ToListAsync();

            return Ok(posts);
        }

 
        [HttpPost("posts/{postId}/block")]
        public async Task<IActionResult> BlockPost(int postId, [FromBody] AdminReasonDto dto)
        {
            var post = await _context.Posts.Include(p => p.User).FirstOrDefaultAsync(p => p.Id == postId);

            if (post == null)
                return NotFound(new { message = "Post not found." });

            var reason = dto?.Reason?.Trim();
            if (string.IsNullOrWhiteSpace(reason))
                return BadRequest(new { message = "A restriction reason is required." });

            post.IsBlocked = true;
            post.ModerationStatus = PostModerationStatus.Rejected;
            post.BlockReason = reason;

            await _context.SaveChangesAsync();

            var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            await _notificationService.CreateOrUpdateAsync(
                actorId, post.UserID, NotificationType.SystemPostRestricted,
                post.Id, $"Your post was restricted: {reason}", "Nexora System", "");

            var emailSent = await TrySendRestrictionEmailAsync(
                post.User,
                "A Nexora post was restricted",
                "Post restriction",
                reason,
                post.Content);

            return Ok(new
            {
                message = "Post blocked successfully.",
                postId = post.Id,
                isBlocked = post.IsBlocked,
                blockReason = post.BlockReason,
                emailSent
            });
        }

 
        [HttpPost("posts/{postId}/unblock")]
        public async Task<IActionResult> UnblockPost(int postId)
        {
            var post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == postId);

            if (post == null)
                return NotFound(new { message = "Post not found." });

            post.IsBlocked = false;
            post.BlockReason = null;
            post.ModerationStatus = Linkedin.Core.Enums.PostModerationStatus.Published;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Post unblocked successfully.",
                postId = post.Id,
                isBlocked = post.IsBlocked
            });
        }

        [HttpPost("posts/{postId}/approve")]
        public async Task<IActionResult> ApprovePost(int postId)
        {
            var post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == postId);

            if (post == null)
                return NotFound(new { message = "Post not found." });

            post.IsBlocked = false;
            post.BlockReason = null;
            post.ModerationStatus = Linkedin.Core.Enums.PostModerationStatus.Published;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Post approved and published successfully.",
                postId = post.Id,
                isBlocked = post.IsBlocked,
                moderationStatus = post.ModerationStatus.ToString()
            });
        }

 
        [HttpGet("job-posts")]
        public async Task<IActionResult> GetJobPosts()
        {
            var jobPosts = await _context.JobPosts
                .Include(j => j.Employer)
                    .ThenInclude(e => e.Company)
                .OrderByDescending(j => j.CreatedAt)
                .Select(j => new
                {
                    id = j.Id,
                    title = j.Title,
                    description = j.Description,
                    location = j.Location,
                    workplaceType = j.WorkplaceType,
                    employmentType = j.EmploymentType,
                    applyUrl = j.ApplyUrl,
                    createdAt = j.CreatedAt,
                    expiresAt = j.ExpiresAt,
                    isActive = j.IsActive,
                    isBlocked = j.IsBlocked,
                    blockReason = j.BlockReason,
                    employerId = j.EmployerId,
                    companyName = j.Employer.Company != null
                        ? j.Employer.Company.Name
                        : j.Employer.FullName,
                    companyUsername = j.Employer.UserName,
                    companyLogo = j.Employer.Company != null
                        ? j.Employer.Company.LogoUrl
                        : j.Employer.ProfileImage
                })
                .ToListAsync();

            return Ok(jobPosts);
        }
 
        [HttpPost("job-posts/{jobPostId}/block")]
        public async Task<IActionResult> BlockJobPost(int jobPostId, [FromBody] AdminReasonDto dto)
        {
            var jobPost = await _context.JobPosts.FirstOrDefaultAsync(j => j.Id == jobPostId);

            if (jobPost == null)
                return NotFound(new { message = "Job post not found." });

            jobPost.IsBlocked = true;
            jobPost.BlockReason = string.IsNullOrWhiteSpace(dto?.Reason)
                ? "Blocked by admin"
                : dto.Reason;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Job post blocked successfully.",
                jobPostId = jobPost.Id,
                isBlocked = jobPost.IsBlocked,
                blockReason = jobPost.BlockReason
            });
        }

 
        [HttpPost("job-posts/{jobPostId}/unblock")]
        public async Task<IActionResult> UnblockJobPost(int jobPostId)
        {
            var jobPost = await _context.JobPosts.FirstOrDefaultAsync(j => j.Id == jobPostId);

            if (jobPost == null)
                return NotFound(new { message = "Job post not found." });

            jobPost.IsBlocked = false;
            jobPost.BlockReason = null;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Job post unblocked successfully.",
                jobPostId = jobPost.Id,
                isBlocked = jobPost.IsBlocked
            });
        }

 
        [HttpGet("reports")]
        public async Task<IActionResult> GetReports()
        {
            var reports = await _context.Reports
                .Include(r => r.Reporter)
                .Include(r => r.Post)
                    .ThenInclude(p => p.User)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    id = r.Id,
                    reason = r.Reason,
                    isReviewed = r.IsReviewed,
                    createdAt = r.CreatedAt,

                    reporterId = r.ReporterId,
                    reporterName = r.Reporter.FullName,
                    reporterUsername = r.Reporter.UserName,
                    reporterProfileImage = r.Reporter.ProfileImage,

                    postId = r.PostId,
                    postContent = r.Post.Content,
                    postImageUrl = r.Post.ImageUrl,
                    postVideoUrl = r.Post.VideoUrl,
                    postIsBlocked = r.Post.IsBlocked,

                    postOwnerId = r.Post.UserID,
                    postOwnerName = r.Post.User.FullName,
                    postOwnerUsername = r.Post.User.UserName
                })
                .ToListAsync();

            return Ok(reports);
        }


        [HttpPost("reports/{reportId}/review")]
        public async Task<IActionResult> ReviewReport(int reportId)
        {
            var report = await _context.Reports.FirstOrDefaultAsync(r => r.Id == reportId);

            if (report == null)
                return NotFound(new { message = "Report not found." });

            report.IsReviewed = true;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Report marked as reviewed.",
                reportId = report.Id,
                isReviewed = report.IsReviewed
            });
        }

        [HttpGet("reports/{reportId:int}")]
        public async Task<IActionResult> GetReportDetails(int reportId)
        {
            var report = await _context.Reports.AsNoTracking()
                .Include(r => r.Reporter)
                .Include(r => r.Post).ThenInclude(p => p.User)
                .FirstOrDefaultAsync(r => r.Id == reportId);
            if (report == null) return NotFound(new { message = "Report not found." });

            return Ok(new
            {
                id = report.Id, reason = report.Reason, isReviewed = report.IsReviewed,
                createdAt = report.CreatedAt,
                reporter = new { id = report.ReporterId, name = report.Reporter.FullName, username = report.Reporter.UserName, email = report.Reporter.Email, profileImage = report.Reporter.ProfileImage },
                post = new
                {
                    id = report.PostId, content = report.Post.Content,
                    imageUrl = report.Post.ImageUrl, videoUrl = report.Post.VideoUrl,
                    createdAt = report.Post.CreatedAt, isBlocked = report.Post.IsBlocked,
                    blockReason = report.Post.BlockReason,
                    moderationStatus = report.Post.ModerationStatus.ToString(),
                    author = new { id = report.Post.UserID, name = report.Post.User.FullName, username = report.Post.User.UserName, profileImage = report.Post.User.ProfileImage }
                }
            });
        }

        private async Task<bool> TrySendRestrictionEmailAsync(
            ApplicationUser user,
            string subject,
            string actionTitle,
            string reason,
            string? content = null)
        {
            if (string.IsNullOrWhiteSpace(user.Email)) return false;
            try
            {
                var safeName = WebUtility.HtmlEncode(user.FullName ?? user.UserName ?? "Nexora member");
                var safeReason = WebUtility.HtmlEncode(reason);
                var safeContent = WebUtility.HtmlEncode(content ?? "");
                var contentBlock = string.IsNullOrWhiteSpace(content)
                    ? ""
                    : $"<p><strong>Content:</strong></p><div style='padding:12px;background:#f6f7fb;border-radius:8px'>{safeContent}</div>";
                await _emailService.SendAsync(user.Email, user.FullName ?? user.UserName ?? user.Email, subject,
                    $"<div style='font-family:Arial,sans-serif;line-height:1.6;color:#172033'><h2>{actionTitle}</h2><p>Hello {safeName},</p><p>A moderation action was applied to your Nexora account.</p><p><strong>Reason:</strong> {safeReason}</p>{contentBlock}<p>If you believe this was a mistake, contact Nexora support.</p></div>");
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    public class AdminReasonDto
    {
        public string? Reason { get; set; }
    }
}
