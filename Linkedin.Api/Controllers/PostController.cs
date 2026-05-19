using Linkedin.Business.Services.Interface;
using Linkedin.Core.Dtos;
using Linkedin.Core.Entities;
using Linkedin.DataAccess.Repositories.Interfaces;
 
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
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
        private readonly UserManager<ApplicationUser> _userManager;

        public PostController(IPostService postService, IUnitOfWork unitOfWork, IJobPostService jobPostService , IUserService userService, UserManager<ApplicationUser> userManager)
        {
            _postService = postService; 
            _jobPostService = jobPostService;
            _userService = userService;
            _userManager = userManager;
        }


 
        [HttpPost("posts")]

        public async Task<IActionResult> CreatePost([FromForm] CreatePostDto dto)
        {
            var user = await _userService.GetAuthenticatedUserAsync(User);
            if (user == null)
            {
                return Unauthorized("User not found or unauthorized!");
            }
          
            var result= await _postService.CreatePostAsync(dto, user.Id);
            var post = result.Data as PostDto;

            if (result.Success)
            {
                return Ok(post);
            } 

            return BadRequest(post);
        }
 

       

        [HttpGet("my")]
        public async Task<IActionResult> GetMyPosts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
 
            var postOwnerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (postOwnerId == null)
                return Unauthorized();

 
            var result = await _postService.GetPostsByUserIdAsync(
                 postOwnerId,postOwnerId,page,pageSize);

            if (!result.Success)
                return Ok(new List<PostDto>());  

            return Ok(result.Data);
        }


        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserPosts(
            string userId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
 

            var result = await _postService.GetPostsByUserIdAsync(
                userId, currentUserId, page, pageSize);

            if (!result.Success)
                return Ok(new List<PostDto>());

            return Ok(result.Data);
        }

        [HttpPut("posts/{postId}")]
        public async Task<IActionResult> UpdatePost(int postId, [FromForm] UpdatePostDto dto)
        {
            var user = await _userService.GetAuthenticatedUserAsync(User);
            if (user == null)
                return Unauthorized("User not found or unauthorized!");

            dto.PostId = postId;  
            var result = await _postService.UpdatePost(user.Id, dto);

            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result.Data);
        }


        [HttpDelete("posts/{postId}")]
        public async Task<IActionResult> DeletePost(int postId)
        {
            var user = await _userService.GetAuthenticatedUserAsync(User);
            if (user == null)
                return Unauthorized("User not found or unauthorized!");

            var result = await _postService.DeletePostAsync(user.Id, postId);

            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(new { message = "Post deleted successfully" });
        }

    }


}
