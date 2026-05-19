using Linkedin.Business.Services.Interface;
using Linkedin.Core.Dtos;
using Linkedin.Core.Entities;
using Linkedin.DataAccess.Repositories.Interfaces;
 
using Microsoft.AspNetCore.SignalR;

namespace Linkedin.Api.Hubs
{
    public class ChatHub:Hub
    {
        private readonly IChatService _chatService;
        private readonly IUserService _userService;
        private readonly IUnitOfWork _unitOfWork;
        public ChatHub(IChatService chatService,IUserService userService,IUnitOfWork unitOfWork)
        {
            _chatService = chatService;
            _userService = userService;
            _unitOfWork = unitOfWork;
        }

        public override Task OnConnectedAsync()
        {
            Console.WriteLine($"User connected: {Context.UserIdentifier}");
            return base.OnConnectedAsync();
        }
        public override Task OnDisconnectedAsync(Exception? exception)
        {
            Console.WriteLine($"User disconnected: {Context.UserIdentifier}");
            return base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(string Username, string message)
        {
            var senderId = Context.UserIdentifier;
            var sender = await _unitOfWork.Users.GetByIdAsync(senderId);
            var receiver = await _userService.GetUserEntityByUsernameAsync(Username);

            if (receiver.Id == null) return;

            // Veritabanına kaydet
            var savedMessage = await _chatService.SendMessageAsync(senderId, receiver.Id, new MessageDto
            {
                Content = message,
                IsImage = false
            });

            // Yayılacak mesaj objesi
            var msgObj = new
            {
                Sender = sender.UserName,
                Receiver = receiver.UserName,
                Content = message,
                Timestamp = savedMessage.DateTime.ToString("s"),
            };

            // Alıcıya mesaj gönder
            await Clients.User(receiver.Id).SendAsync("ReceiveMessage", msgObj);

            // Bildirim oluşturma
            var notificationObj = new
            {
                SenderUsername = sender.UserName,
                SenderImage = sender.ProfileImage,
                Message = $"size bir mesaj gönderdi: {message}",
                Timestamp = savedMessage.DateTime.ToString("s")
            };

            // Alıcıya bildirim yayını yap
            await Clients.User(receiver.Id).SendAsync("ReceiveNotification", notificationObj);

            // Gönderene de gönder (opsiyonel ama UX için iyi)
            await Clients.User(senderId).SendAsync("ReceiveOwnMessage", msgObj);
        }


    }
}
