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

            if (string.IsNullOrWhiteSpace(dto.Reason))
                return BadRequest(new { message = "Report reason is required." });

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
                Reason = dto.Reason.Trim(),
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
    }

    public class CreateReportDto
    {
        public string Reason { get; set; } = null!;
    }
}