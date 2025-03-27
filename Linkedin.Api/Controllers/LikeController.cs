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

        [HttpPost]

        public async Task<IActionResult> CreateLike(CreateLikeDto dto)
        {
            var user = await _userService.GetAuthenticatedUserAsync(User);

            if(user == null)
            {
                return Unauthorized("User not found or unauthorized!");
            }

            var result = await _likeService.AddLikeAsync(dto, user.Id);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

    }
}
