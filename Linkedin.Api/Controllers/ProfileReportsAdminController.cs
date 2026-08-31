using Linkedin.Core.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Linkedin.Api.Controllers
{
    [Route("api/Admin")]
    [ApiController]
    [Authorize(Roles = "Admin,Moderator")]
    public class ProfileReportsAdminController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProfileReportsAdminController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("profile-reports")]
        public async Task<IActionResult> GetProfileReports()
        {
            var since = DateTime.UtcNow.AddDays(-30);
            var reports = await _context.ProfileReports
                .AsNoTracking()
                .Include(item => item.Reporter)
                .Include(item => item.ReportedUser)
                .OrderByDescending(item => item.CreatedAt)
                .ToListAsync();

            var result = reports
                .GroupBy(item => item.ReportedUserId)
                .Select(group =>
                {
                    var pending = group.Where(item => !item.IsReviewed).ToList();
                    var uniqueReporters = pending.Select(item => item.ReporterId).Distinct().Count();
                    var recentReports = pending.Count(item => item.CreatedAt >= since);
                    var riskScore = Math.Min(100,
                        uniqueReporters * 6 +
                        recentReports * 3 +
                        pending.Sum(item => item.Severity * 2));
                    var target = group.First().ReportedUser;

                    return new
                    {
                        userId = group.Key,
                        fullName = target.FullName,
                        username = target.UserName,
                        profileImage = target.ProfileImage,
                        isBlocked = target.IsBlocked,
                        riskScore,
                        riskLevel = riskScore >= 50 ? "High" : riskScore >= 25 ? "Medium" : "Low",
                        reportCount = pending.Count,
                        uniqueReporters,
                        recentReports,
                        reports = group.Select(item => new
                        {
                            id = item.Id,
                            category = item.Category,
                            details = item.Details,
                            severity = item.Severity,
                            isReviewed = item.IsReviewed,
                            createdAt = item.CreatedAt,
                            reporterName = item.Reporter.FullName,
                            reporterUsername = item.Reporter.UserName
                        })
                    };
                })
                .OrderByDescending(item => item.riskScore)
                .ThenByDescending(item => item.reportCount)
                .ToList();

            return Ok(result);
        }

        [HttpPost("profile-reports/{reportId:int}/review")]
        public async Task<IActionResult> ReviewProfileReport(int reportId)
        {
            var report = await _context.ProfileReports.FindAsync(reportId);
            if (report == null)
                return NotFound(new { message = "Profile report not found." });

            report.IsReviewed = true;
            report.ReviewedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Profile report marked as reviewed." });
        }

        [HttpGet("profile-reports/{reportId:int}")]
        public async Task<IActionResult> GetProfileReportDetails(int reportId)
        {
            var report = await _context.ProfileReports.AsNoTracking()
                .Include(item => item.Reporter)
                .Include(item => item.ReportedUser)
                .FirstOrDefaultAsync(item => item.Id == reportId);
            if (report == null)
                return NotFound(new { message = "Profile report not found." });

            var target = report.ReportedUser;
            var postCount = await _context.Posts.CountAsync(post => post.UserID == target.Id);
            var openReports = await _context.ProfileReports.CountAsync(item => item.ReportedUserId == target.Id && !item.IsReviewed);

            return Ok(new
            {
                id = report.Id, category = report.Category, details = report.Details,
                severity = report.Severity, isReviewed = report.IsReviewed,
                createdAt = report.CreatedAt, reviewedAt = report.ReviewedAt,
                reporter = new { id = report.ReporterId, name = report.Reporter.FullName, username = report.Reporter.UserName, email = report.Reporter.Email, profileImage = report.Reporter.ProfileImage },
                reportedUser = new
                {
                    id = target.Id, fullName = target.FullName, username = target.UserName,
                    email = target.Email, profileImage = target.ProfileImage,
                    userType = target.UserType.ToString(), createdAt = target.CreatedAt,
                    bio = target.Bio, currentPosition = target.CurrentPosition,
                    location = target.Location, isBlocked = target.IsBlocked,
                    blockReason = target.BlockReason, postCount, openReports
                }
            });
        }

        [HttpPost("profile-reports/users/{userId}/review")]
        public async Task<IActionResult> ReviewProfileReportsForUser(string userId)
        {
            var reports = await _context.ProfileReports
                .Where(report => report.ReportedUserId == userId && !report.IsReviewed)
                .ToListAsync();

            if (reports.Count == 0)
                return NotFound(new { message = "No pending profile reports were found." });

            var reviewedAt = DateTime.UtcNow;
            foreach (var report in reports)
            {
                report.IsReviewed = true;
                report.ReviewedAt = reviewedAt;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Profile reports marked as reviewed.",
                userId,
                reviewedCount = reports.Count
            });
        }
    }
}
