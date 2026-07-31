using Linkedin.Core.Data;
using Linkedin.Core.Entities;
using Linkedin.DataAccess.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Linkedin.DataAccess.Repositories.Concrete
{
    public class ChatRepository : Repository<Chat>, IChatRepository
    {
        public ChatRepository(AppDbContext context)
            : base(context)
        {
        }

        public async Task<Chat?> GetChatBetweenUsersAsync(
            string senderId,
            string receiverId)
        {
            // Burada yalnız chat tapılır.
            // Bütün mesaj tarixçəsini yükləmək performans problemi yaradardı.
            return await _context.Chats
                .FirstOrDefaultAsync(chat =>
                    (chat.SenderId == senderId &&
                     chat.ReceiverId == receiverId) ||
                    (chat.SenderId == receiverId &&
                     chat.ReceiverId == senderId));
        }

        public async Task<IEnumerable<Message>>
            GetChatMessagesAsync(int chatId)
        {
            return await _context.Messages
                .AsNoTracking()
                .Include(message => message.Sender)
                .Include(message => message.Attachments)
                .Where(message =>
                    message.ChatId == chatId &&
                    !message.IsDeleted)
                .OrderBy(message => message.DateTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<Chat>>
            GetUserChatsAsync(string userId)
        {
            return await _context.Chats
                .AsNoTracking()
                .AsSplitQuery()

                // Chat iştirakçıları
                .Include(chat => chat.Sender)
                .Include(chat => chat.Receiver)

                // Yalnız silinməmiş mesajlar və onların attachment-ləri
                .Include(chat => chat.Messages
                    .Where(message => !message.IsDeleted))
                    .ThenInclude(message => message.Attachments)

                // Son mesajın sender məlumatı lazım ola bilər
                .Include(chat => chat.Messages
                    .Where(message => !message.IsDeleted))
                    .ThenInclude(message => message.Sender)

                .Where(chat =>
                    chat.SenderId == userId ||
                    chat.ReceiverId == userId)

                .ToListAsync();
        }
    }
}