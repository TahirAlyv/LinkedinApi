using Linkedin.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.DataAccess.Repositories.Interfaces
{
    public interface INotificationsRepository : IRepository<Notification>
    {
        Task<List<Notification>> GetNotificationsAsync(string userId);
        Task<bool> ExistsAsync(Expression<Func<Notification, bool>> predicate);
        Task<Notification?> GetSingleAsync(Expression<Func<Notification, bool>> predicate);

        Task MarkAllAsReadAsync(string userId);
    }
        
}
