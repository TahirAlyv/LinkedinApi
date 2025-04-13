using Linkedin.Business.Services.Interface;
using LinkedIn.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Linkedin.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FollowController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IFollowService _followService;

        public FollowController(IUserService userService, IFollowService followService)
        {
            _userService = userService;
            _followService = followService;
        }

        [HttpPost("follow/{username}")]

        public async Task<IActionResult> FollowUser(string username)
        {

            var currentUser = await _userService.GetAuthenticatedUserAsync(User);
            if (currentUser == null)
                return Unauthorized("User not found!");

            var targetUserResult = await _userService.GetUserByUserName(username);
            if (!targetUserResult.Success)
                return NotFound("Target user not found!");

            var targetUser = targetUserResult.Data as ApplicationUser;

            var result = await _followService.FollowAsync(currentUser.Id, targetUser.Id);

            if (!result.Success)
                return BadRequest(result);

            return Ok(new { result.Success,result.Message});
        }

        [HttpPost("unfollow/{username}")]
        public async Task<IActionResult> UnfollowUser(string username)
        {
            var currentUser = await _userService.GetAuthenticatedUserAsync(User);
            var targetUserResult = await _userService.GetUserByUserName(username);
            if (!targetUserResult.Success)
                return NotFound("Target user not found!");
            var targetUser = targetUserResult.Data as ApplicationUser;

            var result = await _followService.UnfollowAsync(currentUser.Id, targetUser.Id);

            if (!result.Success)
                return BadRequest(result);

            return Ok(new {result.Success,result.Message });
        }
    }
}
