using Linkedin.Core.Data;
using Linkedin.Core.Entities;
using Linkedin.DataAccess.Repositories.Interfaces;
 
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.DataAccess.Repositories.Concrete
{
    public class MessageRepository : Repository<Message>, IMessageRepository
    {
        public MessageRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Message> GetMessageByIdAsync(int messageId)
        {
            return await _context.Messages
                .Include(m => m.Chat)
                .FirstOrDefaultAsync(m => m.Id == messageId);
        }

        public async Task<List<Message>> GetMessagesByChatIdAsync(int chatId)
        {
            return await _context.Messages.Where(m => m.ChatId == chatId)
               .OrderBy(m => m.DateTime).ToListAsync();
        }

        

       
    }
     
}
