using Linkedin.Api.Hubs;
using Linkedin.Business.Exceptions;
using Linkedin.Business.Services.Interface;
using Linkedin.Core.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
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
        private readonly IHubContext<ChatHub> _chatHubContext;
        private readonly ILogger<ChatController> _logger;

        public ChatController(
            IChatService chatService,
            IUserService userService,
            IHubContext<ChatHub> chatHubContext,
            ILogger<ChatController> logger)
        {
            _chatService = chatService;
            _userService = userService;
            _chatHubContext = chatHubContext;
            _logger = logger;
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

            var otherUser =
                await _userService.GetUserEntityByUsernameAsync(username);

            if (otherUser == null)
                return NotFound(new { message = "User not found." });

            var messages = await _chatService.GetChatMessagesAsync(
                currentUserId,
                otherUser.Id);

            return Ok(messages);
        }

        [HttpGet("invitation/{username}")]
        public async Task<IActionResult> GetInvitationStatus(string username)
        {
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(currentUserId)) return Unauthorized();
            var otherUser = await _userService.GetUserEntityByUsernameAsync(username);
            if (otherUser == null) return NotFound(new { message = "User not found." });

            return Ok(await _chatService.GetInvitationStatusAsync(currentUserId, otherUser.Id));
        }

        [HttpPost("invitation/{username}/respond")]
        public async Task<IActionResult> RespondToInvitation(
            string username,
            [FromBody] ChatInvitationResponseDto dto)
        {
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(currentUserId)) return Unauthorized();
            var otherUser = await _userService.GetUserEntityByUsernameAsync(username);
            if (otherUser == null) return NotFound(new { message = "User not found." });

            try
            {
                var result = await _chatService.RespondToInvitationAsync(
                    currentUserId,
                    otherUser.Id,
                    dto.Accept);
                await _chatHubContext.Clients.User(otherUser.Id)
                    .SendAsync("ChatInvitationUpdated", result);
                return Ok(result);
            }
            catch (ChatMessageException ex)
            {
                return ToErrorResponse(ex);
            }
        }

        [HttpPost("messages/{username}")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(30L * 1024L * 1024L)]
        public async Task<IActionResult> SendMessage(
            string username,
            [FromForm] SendMessageDto dto,
            CancellationToken cancellationToken)
        {
            var senderId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(senderId))
            {
                return Unauthorized(new
                {
                    message = "User is not authenticated."
                });
            }

            var receiver =
                await _userService.GetUserEntityByUsernameAsync(username);

            if (receiver == null)
            {
                return NotFound(new
                {
                    message = "Receiver was not found."
                });
            }

            try
            {
                var message = await _chatService.SendMessageAsync(
                    senderId,
                    receiver.Id,
                    dto);

                try
                {
                    await _chatHubContext.Clients.User(receiver.Id)
                        .SendAsync(
                            "ReceiveMessage",
                            message,
                            cancellationToken);

                    await _chatHubContext.Clients.User(senderId)
                        .SendAsync(
                            "ReceiveOwnMessage",
                            message,
                            cancellationToken);
                }
                catch (Exception ex)
                {
                    // Message is already stored. Realtime failure must not
                    // roll back or hide the successful HTTP result.
                    _logger.LogError(
                        ex,
                        "Message {MessageId} was saved, but SignalR delivery failed.",
                        message.Id);
                }

                return Ok(message);
            }
            catch (ChatMessageException ex)
            {
                return ToErrorResponse(ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error while sending a chat message.");

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { message = "The message could not be sent." });
            }
        }

        [HttpGet("user-chats")]
        public async Task<IActionResult> GetUserChats()
        {
            var currentUserId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(currentUserId))
                return Unauthorized();

            var chats =
                await _chatService.GetUserChatsAsync(currentUserId);

            var result = chats.Select(chat =>
            {
                var otherUser = chat.SenderId == currentUserId
                    ? chat.Receiver
                    : chat.Sender;

                var lastMessage = chat.Messages?
                    .OrderByDescending(message => message.DateTime)
                    .FirstOrDefault();

                var unreadCount = chat.Messages?
                    .Count(message =>
                        message.SenderId != currentUserId &&
                        !message.HasSeen) ?? 0;

                return new
                {
                    chatId = chat.Id,
                    username = otherUser?.UserName,
                    fullName = otherUser?.FullName,
                    profileImage = otherUser?.ProfileImage,
                    lastMessage = lastMessage == null
                        ? null
                        : new
                        {
                            id = lastMessage.Id,
                            content = lastMessage.Content,
                            dateTime = lastMessage.DateTime,
                            senderId = lastMessage.SenderId,
                            hasSeen = lastMessage.HasSeen,
                            isImage = lastMessage.IsImage,
                            attachments = lastMessage.Attachments
                                .Select(attachment => new ChatAttachmentDto
                                {
                                    Id = attachment.Id,
                                    Url = attachment.Url,
                                    OriginalFileName =
                                        attachment.OriginalFileName,
                                    ContentType = attachment.ContentType,
                                    SizeBytes = attachment.SizeBytes,
                                    Type = attachment.Type
                                })
                                .ToList()
                        },
                    unreadCount,
                    requiresAcceptance = chat.RequiresAcceptance,
                    invitationStatus = chat.InvitationStatus.ToString().ToLowerInvariant(),
                    invitedByMe = chat.InvitedByUserId == currentUserId
                };
            })
            .OrderByDescending(item =>
                item.lastMessage != null
                    ? item.lastMessage.dateTime
                    : DateTime.MinValue)
            .ToList();

            return Ok(result);
        }

        [HttpPost("mark-as-seen/{username}")]
        public async Task<IActionResult> MarkChatAsSeen(string username)
        {
            var currentUserId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(currentUserId))
                return Unauthorized();

            var otherUser =
                await _userService.GetUserEntityByUsernameAsync(username);

            if (otherUser == null)
                return NotFound(new { message = "User not found." });

            await _chatService.MarkChatAsSeenAsync(
                currentUserId,
                otherUser.Id);

            return Ok(new { success = true });
        }

        [HttpDelete("chats/{chatId:int}")]
        public async Task<IActionResult> DeleteChat(int chatId)
        {
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(currentUserId)) return Unauthorized();
            try
            {
                await _chatService.DeleteChatForUserAsync(chatId, currentUserId);
                await _chatHubContext.Clients.User(currentUserId)
                    .SendAsync("ChatDeleted", new { chatId });
                return Ok(new { message = "Conversation deleted from your messages.", chatId });
            }
            catch (ChatMessageException ex)
            {
                return ex.Error switch
                {
                    ChatMessageError.MessageNotFound => NotFound(new { message = ex.Message }),
                    ChatMessageError.NotMessageOwner => StatusCode(403, new { message = ex.Message }),
                    ChatMessageError.SaveFailed => StatusCode(500, new { message = ex.Message }),
                    _ => BadRequest(new { message = ex.Message })
                };
            }
        }

        private IActionResult ToErrorResponse(
            ChatMessageException exception)
        {
            var response = new { message = exception.Message };

            return exception.Error switch
            {
                ChatMessageError.UserNotFound =>
                    NotFound(response),

                ChatMessageError.NotConnected =>
                    StatusCode(
                        StatusCodes.Status403Forbidden,
                        response),

                ChatMessageError.UserBlocked or
                ChatMessageError.InvitationPending or
                ChatMessageError.InvitationRejected or
                ChatMessageError.InvitationRequired =>
                    StatusCode(StatusCodes.Status403Forbidden, response),

                ChatMessageError.SaveFailed =>
                    StatusCode(
                        StatusCodes.Status500InternalServerError,
                        response),

                _ => BadRequest(response)
            };
        }

        [Authorize]
        [HttpDelete("messages/{messageId:int}")]
        public async Task<IActionResult> DeleteMessage(int messageId)
        {
            var currentUserId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return Unauthorized(new
                {
                    message = "User identity could not be determined."
                });
            }

            try
            {
                var result = await _chatService.DeleteMessageAsync(
                    messageId,
                    currentUserId);

                var signalRPayload = new
                {
                    messageId = result.MessageId,
                    chatId = result.ChatId,
                    deletedAt = result.DeletedAt
                };

                /*
                 * Mesaj artıq DB-də silinib.
                 * SignalR uğursuz olsa belə HTTP request uğurlu qalmalıdır.
                 */
                try
                {
                    await Task.WhenAll(
                        _chatHubContext.Clients
                            .User(result.SenderId)
                            .SendAsync(
                                "MessageDeleted",
                                signalRPayload),

                        _chatHubContext.Clients
                            .User(result.ReceiverId)
                            .SendAsync(
                                "MessageDeleted",
                                signalRPayload)
                    );
                }
                catch (Exception signalRException)
                {
                    _logger.LogError(
                        signalRException,
                        "Message was deleted from database, but SignalR notification failed. " +
                        "MessageId: {MessageId}",
                        result.MessageId);
                }

                return Ok(new
                {
                    message = "Message removed for everyone.",
                    data = signalRPayload
                });
            }
            catch (ChatMessageException ex)
            {
                return ex.Error switch
                {
                    ChatMessageError.InvalidRequest =>
                        BadRequest(new
                        {
                            message = ex.Message
                        }),

                    ChatMessageError.MessageNotFound =>
                        NotFound(new
                        {
                            message = ex.Message
                        }),

                    ChatMessageError.NotMessageOwner =>
                        StatusCode(
                            StatusCodes.Status403Forbidden,
                            new
                            {
                                message = ex.Message
                            }),

                    ChatMessageError.SaveFailed =>
                        StatusCode(
                            StatusCodes.Status500InternalServerError,
                            new
                            {
                                message = ex.Message
                            }),

                    _ =>
                        BadRequest(new
                        {
                            message = ex.Message
                        })
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error while deleting message. " +
                    "MessageId: {MessageId}, UserId: {UserId}",
                    messageId,
                    currentUserId);

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        message = "An unexpected error occurred while deleting the message."
                    });
            }
        }
    }
}
