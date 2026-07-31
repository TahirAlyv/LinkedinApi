using Linkedin.Business.Services.Interface;
using Linkedin.Core.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Linkedin.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class LikeController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILikeService _likeService;

        public LikeController(IUserService userService, ILikeService likeService)
        {
            _userService = userService;
            _likeService = likeService;
        }


        [HttpPost("{postId}")]
        public async Task<IActionResult> LikePost(int postId)
        {
            var user = await _userService.GetAuthenticatedUserAsync(User);
            if (user == null)
                return Unauthorized("User not found or unauthorized.");

            var result = await _likeService.ToggleLikeAsync(postId, user.Id);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpDelete("{postId}")]
        public async Task<IActionResult> UnlikePost(int postId)
        {
            var user = await _userService.GetAuthenticatedUserAsync(User);
            if (user == null)
                return Unauthorized("User not found or unauthorized.");

            var result = await _likeService.RemoveLikeAsync(postId, user.Id);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }



    }
}
