
using Linkedin.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.DataAccess.Repositories.Interfaces
{
    public interface IMessageRepository:IRepository<Message>
    {
        Task<List<Message>> GetMessagesByChatIdAsync(int chatId);
        Task<Message> GetMessageByIdAsync(int messageId);   
    }
}
