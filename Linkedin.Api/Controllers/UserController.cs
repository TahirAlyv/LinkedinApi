using Linkedin.Business.Services.Interface;
using Linkedin.Core.Dtos;
using Linkedin.DataAccess.Repositories.Interfaces;
using LinkedIn.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Linkedin.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IFollowService _followService;
        public UserController(IUserService userService, IFollowService followService)
        {
            _userService = userService;
            _followService = followService;
        }

        [HttpGet("me")]

        public async Task<IActionResult> GetUser()
        {
            var user = await _userService.GetAuthenticatedUserAsync(User);
            if(user == null) 
                return Unauthorized("user not found!");

            var dto = new UserDto
            {
                Username = user.UserName,
                Email = user.Email,
                PhotoUrl = user.ProfileImage,
                Bio = user.Bio,
                Skills=user.Skills,
                Experience=user.Experience,

            };

            return Ok(dto);
        }

        [HttpGet("users")]

        public async Task<IActionResult> SearchUser([FromQuery] string query )
        {

            var user = await _userService.GetAuthenticatedUserAsync(User);
            if (user == null)
                return Unauthorized("user not found!");

            var result= await _userService.GetSearchUser(query,user.UserName!);

            if (!result.Success)
                return BadRequest(result);


            var users = result.Data as List<SearchedUserDto>;
            return Ok(users);


        }
        [HttpGet("otheruser/{username}")]

        public async Task<IActionResult> GetOtherUser(string username)
        {
            var user = await _userService.GetAuthenticatedUserAsync(User);
            if (user == null)
                return Unauthorized("user not found!");

            var result= await _userService.GetUserByUserName(username);

            if (!result.Success)
                return NotFound("user not found!");

            var otherUser= result.Data as ApplicationUser;
            var userDto = new OtherUserDto
            {
                Username = otherUser.UserName,
                PhotoUrl = otherUser.ProfileImage,
                Bio = otherUser.Bio,
                Visibility = otherUser.Visibility,
                Followers = otherUser.Followers?.Count() ?? 0,
                Following = otherUser.Following?.Count() ?? 0,
                IsFollowing=await _followService.IsFollowing(user.Id,otherUser.Id),

            };

            return Ok(userDto);

 
        }

    }
}
