using Linkedin.Core.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Linkedin.Api.Controllers
{
    [Route("api/Admin")]
    [ApiController]
    [Authorize(Roles = "Admin")]
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
    }
}
