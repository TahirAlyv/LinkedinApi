using Linkedin.Core.Dtos;
using Linkedin.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Business.Services.Interface
{
    public interface IChatService
    {
        Task<IEnumerable<Message>> GetChatMessagesAsync(string senderId, string receiverId);
        Task<Message> SendMessageAsync(string senderId, string receiverId, MessageDto dto);
        Task<bool> DeleteMessageAsync(int messageId);
        Task<IEnumerable<Chat>> GetUserChatsAsync(string userId);
        Task<Chat> GetOrCreateChatAsync(string senderId, string receiverId);
        Task MarkAsSeenAsync(int messageId);
    }
}
