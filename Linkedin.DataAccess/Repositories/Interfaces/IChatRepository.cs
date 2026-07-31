
using Linkedin.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.DataAccess.Repositories.Interfaces
{
    public interface IChatRepository : IRepository<Chat>
    {
        Task<IEnumerable<Message>> GetChatMessagesAsync(int chatId);
        Task<Chat?> GetChatBetweenUsersAsync(
            string senderId,
            string receiverId);
        Task<IEnumerable<Chat>> GetUserChatsAsync(string userId);
    }
}
