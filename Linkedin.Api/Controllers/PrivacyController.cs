using System.Security.Claims;
using Linkedin.Core.Data;
using Linkedin.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Linkedin.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/privacy")]
    public sealed class PrivacyController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PrivacyController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("blocked-users")]
        public async Task<IActionResult> GetBlockedUsers()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var users = await _context.UserBlocks
                .Where(item => item.BlockerId == userId)
                .OrderByDescending(item => item.CreatedAt)
                .Select(item => new
                {
                    item.BlockedUser.UserName,
                    item.BlockedUser.FullName,
                    item.BlockedUser.ProfileImage,
                    item.CreatedAt
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpPost("block/{username}")]
        public async Task<IActionResult> Block(string username)
        {
            var blockerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(blockerId)) return Unauthorized();

            var target = await _userManager.FindByNameAsync(username.Trim().ToLowerInvariant());
            if (target == null) return NotFound(new { message = "User not found." });
            if (target.Id == blockerId) return BadRequest(new { message = "You cannot block yourself." });

            var exists = await _context.UserBlocks.AnyAsync(item =>
                item.BlockerId == blockerId && item.BlockedUserId == target.Id);
            if (exists) return Ok(new { message = "This user is already blocked." });

            _context.UserBlocks.Add(new UserBlock { BlockerId = blockerId, BlockedUserId = target.Id });

            // A block makes the relationship private immediately: remove any
            // connection and pending request in either direction.
            var connections = await _context.Connections.Where(item =>
                (item.UserId == blockerId && item.ConnectedUserId == target.Id) ||
                (item.UserId == target.Id && item.ConnectedUserId == blockerId)).ToListAsync();
            var requests = await _context.ConnectionRequests.Where(item =>
                (item.SenderId == blockerId && item.ReceiverId == target.Id) ||
                (item.SenderId == target.Id && item.ReceiverId == blockerId)).ToListAsync();
            _context.Connections.RemoveRange(connections);
            _context.ConnectionRequests.RemoveRange(requests);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User blocked." });
        }

        [HttpDelete("block/{username}")]
        public async Task<IActionResult> Unblock(string username)
        {
            var blockerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(blockerId)) return Unauthorized();

            var target = await _userManager.FindByNameAsync(username.Trim().ToLowerInvariant());
            if (target == null) return NotFound(new { message = "User not found." });

            var block = await _context.UserBlocks.SingleOrDefaultAsync(item =>
                item.BlockerId == blockerId && item.BlockedUserId == target.Id);
            if (block == null) return NotFound(new { message = "This user is not blocked." });

            _context.UserBlocks.Remove(block);
            await _context.SaveChangesAsync();
            return Ok(new { message = "User unblocked." });
        }
    }
}
