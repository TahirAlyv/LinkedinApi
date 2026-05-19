using Linkedin.Api.Hubs;
using Linkedin.Business.Services.Interface;
using Linkedin.Core.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Linkedin.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CommentController : ControllerBase
    {
        private readonly ICommentService _commentService;
        private readonly IUserService _userService;
        private readonly IHubContext<CommentHub> _commentHubContext;

        public CommentController(
            ICommentService commentService,
            IUserService userService,
            IHubContext<CommentHub> commentHubContext)
        {
            _commentService = commentService;
            _userService = userService;
            _commentHubContext = commentHubContext;
        }

        [HttpGet("comments/{postId}")]
        public async Task<IActionResult> GetCommentsByPostId(
            [FromRoute] int postId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 5)
        {
            if (page < 1)
                page = 1;

            if (pageSize < 1)
                pageSize = 5;

            var result = await _commentService.GetCommentsByPostIdAsync(
                postId,
                page,
                pageSize
            );

            return Ok(result);
        }

        [HttpDelete("comment/{commentId}")]
        public async Task<IActionResult> DeleteComment([FromRoute] int commentId)
        {
            var user = await _userService.GetAuthenticatedUserAsync(User);
            if (user == null)
            {
                return Unauthorized("User not found or unauthorized!");
            }

            var postId = await _commentService.GetPostIdByCommentIdAsync(commentId);

            var result = await _commentService.DeleteByCommentIdAsync(commentId, user.Id);
            if (!result.Success)
            {
                return BadRequest(result.Message);
            }

            if (postId.HasValue)
            {
                await _commentHubContext.Clients
                    .Group($"post-{postId.Value}")
                    .SendAsync("ReceiveCommentDeleted", commentId);

                var commentCount = await _commentService.GetCommentCountByPostIdAsync(postId.Value);

                await _commentHubContext.Clients
                    .Group($"post-count-{postId.Value}")
                    .SendAsync("ReceiveCommentCountUpdated", postId.Value, commentCount);
            }

            return Ok(result.Message);
        }


        [HttpPut("comment/{commentId}")]
        public async Task<IActionResult> UpdateComment(
        [FromRoute] int commentId,
        [FromBody] UpdateCommentDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Text))
                return BadRequest("Comment text cannot be empty.");

            var user = await _userService.GetAuthenticatedUserAsync(User);
            if (user == null)
                return Unauthorized("User not found or unauthorized!");

            var postId = await _commentService.GetPostIdByCommentIdAsync(commentId);

            var result = await _commentService.UpdateCommentAsync(
                commentId,
                user.Id,
                dto.Text
            );

            if (!result.Success)
                return BadRequest(result.Message);

            if (postId.HasValue)
            {
                await _commentHubContext.Clients
                    .Group($"post-{postId.Value}")
                    .SendAsync("ReceiveCommentUpdated", result.Data);
            }

            return Ok(result.Data);
        }
    }
}