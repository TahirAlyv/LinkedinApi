using Linkedin.Business.Services.Interface;
using Linkedin.Core.Common;
using Linkedin.Core.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Linkedin.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CommentController : ControllerBase
    {
        private ICommentService _commentService;
        private IUserService _userService;

      
        public CommentController(ICommentService commentService, IUserService userService)
        {
            _commentService = commentService;
            _userService = userService;
        }

        [HttpPost("comment")]
        public async Task<IActionResult> CreateComment([FromBody] CreateCommentDto comment)
        {
            var user = await _userService.GetAuthenticatedUserAsync(User);

            if(user == null)
            {
                return Unauthorized("User not found or unauthorized!");
            }
            var result = await _commentService.AddComment(comment, user.Id);

            if(!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);

        }

    }
}
