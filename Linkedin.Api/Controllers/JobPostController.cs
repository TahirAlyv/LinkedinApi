using Linkedin.Business.Services.Interface;
using Linkedin.Core.Data;
using Linkedin.Core.Dtos;
using Linkedin.Core.Entities;
using Linkedin.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Linkedin.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class JobPostController : ControllerBase
    {
        private readonly IJobPostService _jobPostService;
        private readonly AppDbContext _context;

        public JobPostController(
            IJobPostService jobPostService,
            AppDbContext context)
        {
            _jobPostService = jobPostService;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllJobPosts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? query = null
            )
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var result = await _jobPostService.GetAllJobPostsAsync(
                currentUserId,
                page,
                pageSize,
                query
            );

            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetJobPostById(int id)
        {
            var currentUserId = GetCurrentUserId();

            var result = await _jobPostService.GetJobPostByIdAsync(id, currentUserId);

            if (!result.Success)
                return NotFound(result);

            await TrackJobViewAsync(id, currentUserId);

            return Ok(result);
        }

        [HttpGet("my")]
        [Authorize(Roles = "Employer")]
        public async Task<IActionResult> GetMyJobPosts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var currentUserId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(currentUserId))
                return Unauthorized();

            var result = await _jobPostService.GetMyJobPostsAsync(currentUserId, page, pageSize);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("employer/{username}")]
        public async Task<IActionResult> GetEmployerJobPosts(
            string username,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var currentUserId = GetCurrentUserId();

            var result = await _jobPostService.GetJobPostsByEmployerUsernameAsync(username, currentUserId, page, pageSize);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = "Employer")]
        public async Task<IActionResult> CreateJobPost([FromBody] CreateJobPostDto dto)
        {
            var currentUserId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(currentUserId))
                return Unauthorized();

            var result = await _jobPostService.CreateJobPostAsync(dto, currentUserId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Employer")]
        public async Task<IActionResult> UpdateJobPost(int id, [FromBody] UpdateJobPostDto dto)
        {
            var currentUserId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(currentUserId))
                return Unauthorized();

            var result = await _jobPostService.UpdateJobPostAsync(id, dto, currentUserId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Employer")]
        public async Task<IActionResult> DeleteJobPost(int id)
        {
            var currentUserId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(currentUserId))
                return Unauthorized();

            var result = await _jobPostService.DeleteJobPostAsync(id, currentUserId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("save/{jobPostId:int}")]
        [Authorize(Roles = "JobSeeker")]
        public async Task<IActionResult> SaveJob(int jobPostId)
        {
            var currentUserId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(currentUserId))
                return Unauthorized();

            var result = await _jobPostService.SaveJobAsync(jobPostId, currentUserId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpDelete("save/{jobPostId:int}")]
        [Authorize(Roles = "JobSeeker")]
        public async Task<IActionResult> UnsaveJob(int jobPostId)
        {
            var currentUserId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(currentUserId))
                return Unauthorized();

            var result = await _jobPostService.UnsaveJobAsync(jobPostId, currentUserId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("saved")]
        [Authorize(Roles = "JobSeeker")]
        public async Task<IActionResult> GetSavedJobs(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var currentUserId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(currentUserId))
                return Unauthorized();

            var result = await _jobPostService.GetSavedJobsAsync(currentUserId, page, pageSize);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("apply/{jobPostId:int}")]
        [Authorize(Roles = "JobSeeker")]
        public async Task<IActionResult> ApplyJob(int jobPostId)
        {
            var currentUserId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(currentUserId))
                return Unauthorized();

            var result = await _jobPostService.ApplyToJobAsync(jobPostId, currentUserId);

            if (!result.Success)
                return BadRequest(result);

            await TrackApplicationClickAsync(jobPostId, currentUserId);

            return Ok(result);
        }

        [HttpGet("applied")]
        [Authorize(Roles = "JobSeeker")]
        public async Task<IActionResult> GetAppliedJobs(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var currentUserId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(currentUserId))
                return Unauthorized();

            var result = await _jobPostService.GetAppliedJobsAsync(currentUserId, page, pageSize);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpDelete("apply/{jobPostId:int}")]
        [Authorize(Roles = "JobSeeker")]
        public async Task<IActionResult> WithdrawApplication(int jobPostId)
        {
            var currentUserId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(currentUserId))
                return Unauthorized();

            var result = await _jobPostService
                .WithdrawApplicationAsync(jobPostId, currentUserId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        private string? GetCurrentUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        private async Task TrackApplicationClickAsync(
            int jobPostId,
            string viewerId)
        {
            var job = await _context.JobPosts.AsNoTracking()
                .Where(item => item.Id == jobPostId)
                .Select(item => new
                {
                    item.Id,
                    item.EmployerId
                })
                .FirstOrDefaultAsync();

            if (job == null || job.EmployerId == viewerId)
                return;

            var since = DateTime.UtcNow.AddMinutes(-30);
            var exists = await _context.AnalyticsEvents.AsNoTracking()
                .AnyAsync(item =>
                    item.EventType == AnalyticsEventType.JobApplyClick &&
                    item.ViewerUserId == viewerId &&
                    item.JobPostId == jobPostId &&
                    item.CreatedAt >= since);

            if (exists)
                return;

            _context.AnalyticsEvents.Add(new AnalyticsEvent
            {
                EventType = AnalyticsEventType.JobApplyClick,
                ViewerUserId = viewerId,
                TargetUserId = job.EmployerId,
                JobPostId = job.Id
            });

            await _context.SaveChangesAsync();
        }

        private async Task TrackJobViewAsync(int jobPostId, string? viewerId)
        {
            if (string.IsNullOrWhiteSpace(viewerId))
                return;

            var job = await _context.JobPosts.AsNoTracking()
                .Where(item => item.Id == jobPostId)
                .Select(item => new { item.Id, item.EmployerId })
                .FirstOrDefaultAsync();

            if (job == null || job.EmployerId == viewerId)
                return;

            var since = DateTime.UtcNow.AddMinutes(-30);
            var exists = await _context.AnalyticsEvents.AsNoTracking()
                .AnyAsync(item =>
                    item.EventType == AnalyticsEventType.JobView &&
                    item.ViewerUserId == viewerId &&
                    item.JobPostId == jobPostId &&
                    item.CreatedAt >= since);

            if (exists)
                return;

            _context.AnalyticsEvents.Add(new AnalyticsEvent
            {
                EventType = AnalyticsEventType.JobView,
                ViewerUserId = viewerId,
                TargetUserId = job.EmployerId,
                JobPostId = job.Id
            });
            await _context.SaveChangesAsync();
        }
    }
}
