using Linkedin.Business.Services.Interface;
using Linkedin.DataAccess.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Linkedin.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly IUserService _userService;
        private readonly IUnitOfWork _unitOfWork;

        public ChatController(
            IChatService chatService,
            IUserService userService,
            IUnitOfWork unitOfWork)
        {
            _chatService = chatService;
            _userService = userService;
            _unitOfWork = unitOfWork;
        }

        private string? GetCurrentUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        [HttpGet("messages/{username}")]
        public async Task<IActionResult> GetMessages(string username)
        {
            var currentUserId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(currentUserId))
                return Unauthorized();

            var otherUser = await _userService.GetUserEntityByUsernameAsync(username);

            if (otherUser == null)
                return NotFound("User not found.");

            var messages = await _chatService.GetChatMessagesAsync(currentUserId, otherUser.Id);

            var result = messages.Select(m => new
            {
                id = m.Id,
                chatId = m.ChatId,
                sender = m.Sender?.UserName,
                senderId = m.SenderId,
                senderProfileImage = m.Sender?.ProfileImage,
                content = m.Content,
                isImage = m.IsImage,
                dateTime = m.DateTime,
                hasSeen = m.HasSeen
            });

            return Ok(result);
        }

        [HttpGet("user-chats")]
        public async Task<IActionResult> GetUserChats()
        {
            var currentUserId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(currentUserId))
                return Unauthorized();

            var chats = await _chatService.GetUserChatsAsync(currentUserId);

            var result = chats.Select(chat =>
            {
                var otherUser = chat.SenderId == currentUserId
                    ? chat.Receiver
                    : chat.Sender;

                var lastMessage = chat.Messages?
                    .OrderByDescending(m => m.DateTime)
                    .FirstOrDefault();

                var unreadCount = chat.Messages?
                    .Count(m => m.SenderId != currentUserId && !m.HasSeen) ?? 0;

                return new
                {
                    chatId = chat.Id,
                    username = otherUser?.UserName,
                    fullName = otherUser?.FullName,
                    profileImage = otherUser?.ProfileImage,
                    lastMessage = lastMessage == null ? null : new
                    {
                        id = lastMessage.Id,
                        content = lastMessage.Content,
                        dateTime = lastMessage.DateTime,
                        senderId = lastMessage.SenderId,
                        hasSeen = lastMessage.HasSeen
                    },
                    unreadCount
                };
            })
            .OrderByDescending(x => x.lastMessage != null ? x.lastMessage.dateTime : DateTime.MinValue)
            .ToList();

            return Ok(result);
        }

        [HttpPost("mark-as-seen/{username}")]
        public async Task<IActionResult> MarkChatAsSeen(string username)
        {
            var currentUserId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(currentUserId))
                return Unauthorized();

            var otherUser = await _userService.GetUserEntityByUsernameAsync(username);

            if (otherUser == null)
                return NotFound("User not found.");

            await _chatService.MarkChatAsSeenAsync(currentUserId, otherUser.Id);

            return Ok(new { success = true });
        }
    }
}