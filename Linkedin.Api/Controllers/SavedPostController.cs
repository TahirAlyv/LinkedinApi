using Linkedin.Core.Data;
using Linkedin.Core.Dtos;
using Linkedin.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Linkedin.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class SavedPostController : ControllerBase
    {
        private readonly AppDbContext _context;
        public SavedPostController(AppDbContext context) => _context = context;

        [HttpGet]
        public async Task<IActionResult> GetSavedPosts()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var posts = await _context.SavedPosts
                .AsNoTracking()
                .Where(item => item.UserId == userId && !item.Post.IsBlocked)
                .Include(item => item.Post).ThenInclude(post => post.User)
                .OrderByDescending(item => item.SavedAt)
                .Select(item => new PostDto
                {
                    Id = item.Post.Id,
                    PostOwnerId = item.Post.UserID,
                    Username = item.Post.User.UserName!,
                    UserPhoto = item.Post.User.ProfileImage,
                    Content = item.Post.Content,
                    ImageUrl = item.Post.ImageUrl,
                    VideoUrl = item.Post.VideoUrl,
                    CreatedAt = item.Post.CreatedAt,
                    LikeCount = item.Post.LikeCount,
                    CommentCount = item.Post.CommentCount,
                    IsSaved = true
                })
                .ToListAsync();
            return Ok(posts);
        }

        [HttpPost("{postId:int}")]
        public async Task<IActionResult> Save(int postId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            if (!await _context.Posts.AnyAsync(post => post.Id == postId && !post.IsBlocked))
                return NotFound();
            if (!await _context.SavedPosts.AnyAsync(item => item.UserId == userId && item.PostId == postId))
            {
                _context.SavedPosts.Add(new SavedPost { UserId = userId, PostId = postId });
                await _context.SaveChangesAsync();
            }
            return Ok();
        }

        [HttpDelete("{postId:int}")]
        public async Task<IActionResult> Remove(int postId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var item = await _context.SavedPosts.FirstOrDefaultAsync(x => x.UserId == userId && x.PostId == postId);
            if (item != null) { _context.SavedPosts.Remove(item); await _context.SaveChangesAsync(); }
            return NoContent();
        }
    }
}
