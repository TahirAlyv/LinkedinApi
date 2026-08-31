using Linkedin.Business.Services.Interface;
using Linkedin.Core.Data;
using Linkedin.DataAccess.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Linkedin.Core.Enums;

namespace Linkedin.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserService _userService;
        private readonly INotficationsService _notificationService;
        private readonly AppDbContext _context;

        public NotificationsController(
            IUnitOfWork unitOfWork,
            IUserService userService,
            INotficationsService notificationService,
            AppDbContext context)
        {
            _unitOfWork = unitOfWork;
            _userService = userService;
            _notificationService = notificationService;
            _context = context;
        }

        [HttpGet("notifications")]
        public async Task<IActionResult> GetNotifications()
        {
            var user = await _userService.GetAuthenticatedUserAsync(User);

            if (user == null)
                return Unauthorized("User not found!");

            var notifications = await _notificationService.GetNotificationsForUserAsync(user.Id);

            return Ok(notifications);
        }

        [HttpPost("mark-all-as-read")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var user = await _userService.GetAuthenticatedUserAsync(User);

            if (user == null)
                return Unauthorized("User not found!");

            await _notificationService.MarkAllAsReadAsync(user.Id);

            return Ok(new
            {
                message = "Notifications marked as read."
            });
        }

        [HttpPost("{id:int}/mark-as-read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(item =>
                    item.Id == id &&
                    item.ReceiverId == userId);

            if (notification == null)
                return NotFound();

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(item =>
                    item.Id == id &&
                    item.ReceiverId == userId);

            if (notification == null)
                return NotFound();

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("{id:int}/details")]
        public async Task<IActionResult> GetDetails(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var notification = await _context.Notifications.AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id && item.ReceiverId == userId);
            if (notification == null) return NotFound();

            if ((notification.Type == NotificationType.SystemPostRestricted ||
                 notification.Type == NotificationType.PostModerationWarning) &&
                notification.PostId.HasValue)
            {
                var post = await _context.Posts.AsNoTracking()
                    .FirstOrDefaultAsync(item => item.Id == notification.PostId.Value && item.UserID == userId);
                return Ok(new
                {
                    type = notification.Type.ToString(),
                    title = notification.Type == NotificationType.PostModerationWarning
                        ? "Post sent for review"
                        : "Post restricted",
                    message = notification.ContentPreview, createdAt = notification.LastTriggeredAt ?? notification.CreatedAt,
                    post = post == null ? (object?)null : new
                    {
                        id = post.Id,
                        content = post.Content,
                        imageUrl = post.ImageUrl,
                        videoUrl = post.VideoUrl,
                        createdAt = post.CreatedAt,
                        reason = notification.Type == NotificationType.PostModerationWarning
                            ? post.AiModerationReason
                            : post.BlockReason
                    }
                });
            }

            return Ok(new
            {
                type = notification.Type.ToString(),
                title = notification.Type == NotificationType.SystemAccountRestricted ? "Account restricted" : "Notification",
                message = notification.ContentPreview,
                createdAt = notification.LastTriggeredAt ?? notification.CreatedAt
            });
        }
    }
}
