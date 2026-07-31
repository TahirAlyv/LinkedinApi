using Linkedin.Core.Data;
using Linkedin.Core.Entities;
using Linkedin.DataAccess.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Linkedin.DataAccess.Repositories.Concrete
{
    public class MessageRepository
        : Repository<Message>, IMessageRepository
    {
        public MessageRepository(AppDbContext context)
            : base(context)
        {
        }

        public async Task<Message?> GetMessageByIdAsync(
            int messageId)
        {
            return await _context.Messages
                .Include(message => message.Chat)
                .Include(message => message.Sender)
                .Include(message => message.Attachments)
                .FirstOrDefaultAsync(message =>
                    message.Id == messageId &&
                    !message.IsDeleted);
        }

        public async Task<List<Message>>
        GetMessagesByChatIdAsync(int chatId)
        {
            return await _context.Messages
                .Include(message => message.Sender)
                .Include(message => message.Attachments)
                .Where(message =>
                    message.ChatId == chatId &&
                    !message.IsDeleted)
                .OrderBy(message => message.DateTime)
                .ToListAsync();
        }
    }
}