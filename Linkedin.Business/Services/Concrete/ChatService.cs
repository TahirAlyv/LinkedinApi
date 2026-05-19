using Linkedin.Business.Services.Interface;
using Linkedin.Core.Dtos;
using Linkedin.Core.Entities;
using Linkedin.DataAccess.Repositories.Interfaces;
 
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Business.Services.Concrete
{
    public class ChatService : IChatService
    {

        private readonly IUnitOfWork _unitOfWork;

        public ChatService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public Task<bool> DeleteMessageAsync(int messageId)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<Message>> GetChatMessagesAsync(string senderId, string receiverId)
        {
            var chat = await _unitOfWork.Chats.GetChatBetweenUsersAsync(senderId, receiverId);
       
            var messages = await _unitOfWork.Messages.GetMessagesByChatIdAsync(chat.Id);
 
            return messages;
             
        }
        public async Task<Chat> GetOrCreateChatAsync(string senderId, string receiverId)
        {
            var chat = await _unitOfWork.Chats.GetChatBetweenUsersAsync(senderId, receiverId);


            if (chat != null) return chat;

            var newChat = new Chat
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                CreatedAt = DateTime.UtcNow,
                Messages = new List<Message>()
            };

            _unitOfWork.Chats.AddAsync(newChat);
            await _unitOfWork.CompleteAsync();  

            return newChat;
        }

        public async Task<IEnumerable<Chat>> GetUserChatsAsync(string userId)
        {
             return await _unitOfWork.Chats.GetUserChatsAsync(userId);
        }

        public async Task MarkAsSeenAsync(int messageId)
        {
            var message = await _unitOfWork.Messages.GetMessageByIdAsync(messageId);
            if (message != null && !message.HasSeen)
            {
                message.HasSeen = true;
                await _unitOfWork.CompleteAsync();
            }
        }


        public async Task<Message> SendMessageAsync(string senderId, string receiverId, MessageDto dto)
        {
 
            var chat = await GetOrCreateChatAsync(senderId, receiverId);

 
            var message = new Message
            {
                ChatId = chat.Id,
                SenderId = senderId,
                Content = dto.Content,
                IsImage = dto.IsImage,
                DateTime = DateTime.UtcNow,
                HasSeen = false
            };

            _unitOfWork.Messages.AddAsync(message);
            await _unitOfWork.CompleteAsync();
            return message;
        }
    }
}
