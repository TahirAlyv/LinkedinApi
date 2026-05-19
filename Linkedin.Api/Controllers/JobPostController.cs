using Linkedin.Business.Services.Interface;
using Linkedin.Core.Dtos;
using Linkedin.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Linkedin.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class JobPostController : Controller
    {

        private readonly IJobPostService _jobPostService;
        private readonly IUserService _userService;
        private readonly UserManager<ApplicationUser> _userManager;


        public JobPostController(IJobPostService jobPostService, IUserService userService, UserManager<ApplicationUser> userManager)
        {
            _jobPostService = jobPostService;
            _userService = userService;
            _userManager = userManager;
        }



        [HttpPost("jobposts")]
        public async Task<IActionResult> CreateJobPost([FromForm] CreateJobPostDto dto)
        {
            var user = await _userService.GetAuthenticatedUserAsync(User);
            if (user == null)
            {
                return Unauthorized("User not found or unauthorized!");
            }
            var result = await _jobPostService.CreateJobPostAsync(dto, user.Id);

            var jobPost = result.Data as JobPostDto;

            if (result.Success)
            {
                return Ok(jobPost);
            }
            return BadRequest(jobPost);

        }


        [HttpGet("my")]
        public async Task<IActionResult> GetMyAllJobPosts([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var user = await _userService.GetAuthenticatedUserAsync(User);
            if (user == null)
                return Unauthorized("User not found or unauthorized!");

            var result = await _jobPostService.GetAllJobPostsByUserId(user.Id, user.Id, page, pageSize);

            if (result.Success)
                return Ok(result.Data); // ✅ List<JobPostDto>

            return BadRequest(result.Message);
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserAllJobPosts(string userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var user = await _userService.GetAuthenticatedUserAsync(User);
            if (user == null)
                return Unauthorized("User not found or unauthorized!");

            var result = await _jobPostService.GetAllJobPostsByUserId(userId, user.Id, page, pageSize);

            if (result.Success)
                return Ok(result.Data); // ✅ List<JobPostDto>

            return BadRequest(result.Message);
        }


        [HttpDelete("jobposts/{id}")]

        public async Task<IActionResult> DeleteJobPost(int id)
        {
            var user = await _userService.GetAuthenticatedUserAsync(User);
            if (user == null)
            {
                return Unauthorized("User not found or unauthorized!");
            }
            var result = await _jobPostService.DeleteJobPostAsync(id, user.Id);
            if (result)
            {
                return Ok("post was successfully deleted!");
            }
            return BadRequest("The problem occurred when the post was deleted.");
        }



        [HttpPut("jobposts/{id}")]

        public async Task<IActionResult> UpdateJobPost(int id, [FromForm] UpdateJobPostDto dto)
        {
            var user = await _userService.GetAuthenticatedUserAsync(User);
            if (user == null)
            {
                return Unauthorized("User not found or unauthorized!");
            }
            var result = await _jobPostService.UpdateJobPostAsync(id, dto, user.Id);
            var jobPost = result.Data as JobPostDto;
            if (result.Success)
            {
                return Ok(jobPost);
            }
            return BadRequest(result.Message);

        }

    }
}
