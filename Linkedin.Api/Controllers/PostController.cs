using Linkedin.Business.Services.Interface;
using Linkedin.Core.Dtos;
using Linkedin.DataAccess.Repositories.Interfaces;
using LinkedIn.Core.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Linkedin.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]

    public class PostController : ControllerBase
    {
        private readonly IPostService _postService;
        private readonly IJobPostService _jobPostService;
        private readonly IUserService _userService;

        public PostController(IPostService postService, IUnitOfWork unitOfWork, IJobPostService jobPostService , IUserService userService)
        {
            _postService = postService; 
            _jobPostService = jobPostService;
            _userService = userService;
        }

 
        [HttpGet("get-user-claims")]
        public IActionResult GetUserClaims()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; // "f9388df4-..."
            Console.WriteLine($"Token'daki User ID: {userId}");

            return Ok(userId);
        }


        [Authorize(Roles = "JobSeeker")]
        [HttpPost("test")]
        public IActionResult Test()
        {
           

            return Ok("Isleyir");
        }



        [Authorize(Roles = "JobSeeker")]
        [HttpPost("posts")]

        public async Task<IActionResult> CreatePost([FromForm] CreatePostDto dto)
        {

            var user = await _userService.GetAuthenticatedUserAsync(User);
            if (user == null)
            {
                return Unauthorized("User not found or unauthorized!");
            }
          
            var result= await _postService.CreatePostAsync(dto, user.Id);


            if (result.Success)
            {
                return Ok(result);
            } 

            return BadRequest(result);
        }

        [Authorize(Roles = "Employer")]
        [HttpPost("job-posts")]

        public async Task<IActionResult> CreateJobPostPost([FromForm] CreateJobPostDto dto)
        {
            var user = await _userService.GetAuthenticatedUserAsync(User);
            if (user == null)
            {
                return Unauthorized("User Not found!");
            }
            var result = await _jobPostService.CreateJobPostAsync(dto,user.Id);


            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }


    }


}
