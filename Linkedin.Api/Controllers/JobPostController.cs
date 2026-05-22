using Linkedin.Business.Services.Interface;
using Linkedin.Core.Dtos;
using Linkedin.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Linkedin.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class JobPostController : ControllerBase
    {
        private readonly IJobPostService _jobPostService;

        public JobPostController(IJobPostService jobPostService)
        {
            _jobPostService = jobPostService;
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

            return Ok(result);
        }

        [HttpGet("my")]
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
        public async Task<IActionResult> ApplyJob(int jobPostId)
        {
            var currentUserId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(currentUserId))
                return Unauthorized();

            var result = await _jobPostService.ApplyToJobAsync(jobPostId, currentUserId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("applied")]
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

        private string? GetCurrentUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
}
