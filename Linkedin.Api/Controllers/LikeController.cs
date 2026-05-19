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
        public IActionResult LikePost(int postId)
            => BadRequest("Use SignalR LikeHub");

        [HttpDelete("{postId}")]
        public IActionResult UnlikePost(int postId)
            => BadRequest("Use SignalR LikeHub");



    }
}
