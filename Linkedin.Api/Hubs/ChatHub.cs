using Linkedin.Business.Exceptions;
using Linkedin.Business.Services.Interface;
using Linkedin.Core.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Linkedin.Api.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;
        private readonly IUserService _userService;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(
            IChatService chatService,
            IUserService userService,
            ILogger<ChatHub> logger)
        {
            _chatService = chatService;
            _userService = userService;
            _logger = logger;
        }

        public override Task OnConnectedAsync()
        {
            _logger.LogInformation(
                "ChatHub connected. UserId: {UserId}",
                Context.UserIdentifier);

            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation(
                exception,
                "ChatHub disconnected. UserId: {UserId}",
                Context.UserIdentifier);

            return base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(
            string username,
            string message)
        {
            var senderId = Context.UserIdentifier;

            if (string.IsNullOrWhiteSpace(senderId))
            {
                await Clients.Caller.SendAsync(
                    "MessageError",
                    "User is not authenticated.");

                return;
            }

            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(message))
            {
                await Clients.Caller.SendAsync(
                    "MessageError",
                    "Receiver and message are required.");

                return;
            }

            var receiver =
                await _userService.GetUserEntityByUsernameAsync(username);

            if (receiver == null)
            {
                await Clients.Caller.SendAsync(
                    "MessageError",
                    "Receiver was not found.");

                return;
            }

            try
            {
                var savedMessage =
                    await _chatService.SendMessageAsync(
                        senderId,
                        receiver.Id,
                        new SendMessageDto
                        {
                            Content = message
                        });

                try
                {
                    await Clients.User(receiver.Id)
                        .SendAsync(
                            "ReceiveMessage",
                            savedMessage);

                    await Clients.User(senderId)
                        .SendAsync(
                            "ReceiveOwnMessage",
                            savedMessage);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Message {MessageId} was saved, but SignalR delivery failed.",
                        savedMessage.Id);
                }
            }
            catch (ChatMessageException ex)
            {
                await Clients.Caller.SendAsync(
                    "MessageError",
                    ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error while sending a chat message.");

                await Clients.Caller.SendAsync(
                    "MessageError",
                    "The message could not be sent.");
            }
        }
    }
}
