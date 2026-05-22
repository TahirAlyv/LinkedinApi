using Linkedin.Business.Services.Interface;
using Linkedin.Core.Dtos;
using Linkedin.DataAccess.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Linkedin.Api.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;
        private readonly IUserService _userService;
        private readonly IUnitOfWork _unitOfWork;

        public ChatHub(
            IChatService chatService,
            IUserService userService,
            IUnitOfWork unitOfWork)
        {
            _chatService = chatService;
            _userService = userService;
            _unitOfWork = unitOfWork;
        }

        public override Task OnConnectedAsync()
        {
            Console.WriteLine($"ChatHub connected: {Context.UserIdentifier}");
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            Console.WriteLine($"ChatHub disconnected: {Context.UserIdentifier}");
            return base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(string username, string message)
        {
            var senderId = Context.UserIdentifier;

            if (string.IsNullOrWhiteSpace(senderId))
                return;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(message))
                return;

            var sender = await _unitOfWork.Users.GetByIdAsync(senderId);
            var receiver = await _userService.GetUserEntityByUsernameAsync(username);

            if (sender == null || receiver == null)
                return;

            var areConnected = await _unitOfWork.Connections.AreConnectedAsync(senderId, receiver.Id);

            if (!areConnected)
            {
                await Clients.Caller.SendAsync(
                    "MessageError",
                    "You can message only connected users."
                );

                return;
            }

            var savedMessage = await _chatService.SendMessageAsync(
                senderId,
                receiver.Id,
                new MessageDto
                {
                    Content = message.Trim(),
                    IsImage = false
                });

            var msgObj = new
            {
                id = savedMessage.Id,
                chatId = savedMessage.ChatId,
                sender = sender.UserName,
                senderId = sender.Id,
                senderProfileImage = sender.ProfileImage,
                receiver = receiver.UserName,
                receiverId = receiver.Id,
                content = savedMessage.Content,
                isImage = savedMessage.IsImage,
                dateTime = savedMessage.DateTime,
                hasSeen = savedMessage.HasSeen
            };

            await Clients.User(receiver.Id).SendAsync("ReceiveMessage", msgObj);
            await Clients.User(senderId).SendAsync("ReceiveOwnMessage", msgObj);
        }
    }
}