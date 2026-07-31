using System.Security.Claims;
using Linkedin.Core.Data;
using Linkedin.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Linkedin.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReportsController(AppDbContext context)
        {
            _context = context;
        }

        // POST: api/Reports/post/{postId}
        [HttpPost("post/{postId}")]
        public async Task<IActionResult> ReportPost(int postId, [FromBody] CreateReportDto dto)
        {
            var reporterId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(reporterId))
                return Unauthorized();

            var postExists = await _context.Posts.AnyAsync(p => p.Id == postId);

            if (!postExists)
                return NotFound(new { message = "Post not found." });

            var category = dto.Category?.Trim();
            var allowedCategories = new[] { "Spam", "Harassment", "Hate speech", "Misinformation", "Other" };
            if (string.IsNullOrWhiteSpace(category) || !allowedCategories.Contains(category, StringComparer.OrdinalIgnoreCase))
                return BadRequest(new { message = "Choose a valid report reason." });

            var details = dto.Details?.Trim();
            if (string.Equals(category, "Other", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(details))
                return BadRequest(new { message = "Please explain the reason for reporting this post." });

            if (details?.Length > 500)
                return BadRequest(new { message = "Report details cannot exceed 500 characters." });

            var alreadyReported = await _context.Reports.AnyAsync(r =>
                r.ReporterId == reporterId &&
                r.PostId == postId &&
                !r.IsReviewed);

            if (alreadyReported)
                return BadRequest(new { message = "You already reported this post." });

            var report = new Report
            {
                ReporterId = reporterId,
                PostId = postId,
                Reason = string.IsNullOrWhiteSpace(details) ? category : $"{category}: {details}",
                IsReviewed = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Reports.Add(report);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Report submitted successfully.",
                reportId = report.Id
            });
        }

        // POST: api/Reports/profile/{username}
        [HttpPost("profile/{username}")]
        public async Task<IActionResult> ReportProfile(string username, [FromBody] CreateReportDto dto)
        {
            var reporterId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(reporterId))
                return Unauthorized();

            var target = await _context.Users
                .FirstOrDefaultAsync(user => user.UserName != null && user.UserName.ToLower() == username.ToLower());

            if (target == null)
                return NotFound(new { message = "Profile not found." });

            if (target.Id == reporterId)
                return BadRequest(new { message = "You cannot report your own profile." });

            var category = dto.Category?.Trim();
            var severityByCategory = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["Fake profile"] = 3,
                ["Harassment"] = 3,
                ["Spam"] = 2,
                ["Inappropriate content"] = 2,
                ["Other"] = 1
            };

            if (string.IsNullOrWhiteSpace(category) || !severityByCategory.TryGetValue(category, out var severity))
                return BadRequest(new { message = "Choose a valid report reason." });

            var details = dto.Details?.Trim();
            if (string.Equals(category, "Other", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(details))
                return BadRequest(new { message = "Please briefly explain the issue." });

            if (details?.Length > 500)
                return BadRequest(new { message = "Report details cannot exceed 500 characters." });

            var alreadyReported = await _context.ProfileReports.AnyAsync(report =>
                report.ReporterId == reporterId &&
                report.ReportedUserId == target.Id &&
                !report.IsReviewed);

            if (alreadyReported)
                return BadRequest(new { message = "You already have a pending report for this profile." });

            var report = new ProfileReport
            {
                ReporterId = reporterId,
                ReportedUserId = target.Id,
                Category = category,
                Details = details,
                Severity = severity,
                IsReviewed = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.ProfileReports.Add(report);
            await _context.SaveChangesAsync();

            var since = DateTime.UtcNow.AddDays(-30);
            var profileReports = await _context.ProfileReports
                .Where(item => item.ReportedUserId == target.Id && !item.IsReviewed)
                .ToListAsync();

            var uniqueReporters = profileReports.Select(item => item.ReporterId).Distinct().Count();
            var recentReports = profileReports.Count(item => item.CreatedAt >= since);
            var riskScore = Math.Min(100,
                uniqueReporters * 6 +
                recentReports * 3 +
                profileReports.Sum(item => item.Severity * 2));

            return Ok(new
            {
                message = "Report submitted successfully.",
                reportId = report.Id,
                moderation = new { riskScore, reportCount = profileReports.Count, uniqueReporters }
            });
        }
    }

    public class CreateReportDto
    {
        public string Category { get; set; } = null!;
        public string? Details { get; set; }
    }
}
