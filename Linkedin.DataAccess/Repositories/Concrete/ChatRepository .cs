 
using Linkedin.DataAccess.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Linkedin.Core.Data;
using Microsoft.EntityFrameworkCore;
using Linkedin.Core.Entities;

namespace Linkedin.DataAccess.Repositories.Concrete
{
    public class ChatRepository : Repository<Chat>, IChatRepository
    {
        public ChatRepository(AppDbContext context) : base(context) { }

        public async Task<Chat> GetChatBetweenUsersAsync(string senderId, string receiverId)
        {
            return await _context.Chats
                .Include(c => c.Messages)
                .ThenInclude(m=> m.Sender)
                .FirstOrDefaultAsync(c =>
                    (c.SenderId == senderId && c.ReceiverId == receiverId) ||
                    (c.SenderId == receiverId && c.ReceiverId == senderId));
        }

        public async Task<IEnumerable<Message>> GetChatMessagesAsync(int chatId)
        {
            return await _context.Messages.Where(m => m.ChatId == chatId).ToListAsync();
        }

        public async Task<IEnumerable<Chat>> GetUserChatsAsync(string userId)
        {
                return await _context.Chats
                .Include(c => c.Messages)
                .Include(c=> c.Sender)
                .Include(c=> c.Receiver)
                .Where(c => c.SenderId == userId || c.ReceiverId == userId)
                .ToListAsync();
        }
    }
}
