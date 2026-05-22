using Linkedin.Core.Data;
using Linkedin.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Linkedin.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
 
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var totalUsers = await _context.Users.CountAsync();
            var totalPosts = await _context.Posts.CountAsync();
            var totalJobPosts = await _context.JobPosts.CountAsync();
            var totalReports = await _context.Reports.CountAsync();

            var blockedUsers = await _context.Users.CountAsync(u => u.IsBlocked);
            var blockedPosts = await _context.Posts.CountAsync(p => p.IsBlocked);
            var blockedJobPosts = await _context.JobPosts.CountAsync(j => j.IsBlocked);

            var monthlyRegistrations = await _context.Users
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
                totalPosts,
                totalJobPosts,
                totalReports,
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
        public async Task<IActionResult> BlockUser(string userId, [FromBody] AdminReasonDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound(new { message = "User not found." });

            user.IsBlocked = true;
            user.BlockReason = string.IsNullOrWhiteSpace(dto?.Reason)
                ? "Blocked by admin"
                : dto.Reason;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "User blocked successfully.",
                userId = user.Id,
                isBlocked = user.IsBlocked,
                blockReason = user.BlockReason
            });
        }

 
        [HttpPost("users/{userId}/unblock")]
        public async Task<IActionResult> UnblockUser(string userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

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
            var post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == postId);

            if (post == null)
                return NotFound(new { message = "Post not found." });

            post.IsBlocked = true;
            post.BlockReason = string.IsNullOrWhiteSpace(dto?.Reason)
                ? "Blocked by admin"
                : dto.Reason;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Post blocked successfully.",
                postId = post.Id,
                isBlocked = post.IsBlocked,
                blockReason = post.BlockReason
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

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Post unblocked successfully.",
                postId = post.Id,
                isBlocked = post.IsBlocked
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
    }

    public class AdminReasonDto
    {
        public string? Reason { get; set; }
    }
}