using Linkedin.Core.Data;
using Linkedin.Core.Entities;
using Linkedin.DataAccess.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.DataAccess.Repositories.Concrete
{
    public class NotificationsRepositor : Repository<Notification>, INotificationsRepository
    {
        public NotificationsRepositor(AppDbContext context) : base(context) { }

        public async Task<bool> ExistsAsync(Expression<Func<Notification, bool>> predicate)
        {
            return await _context.Notifications.AnyAsync(predicate);
        }

        public async Task<List<Notification>> GetNotificationsAsync(string userId)
        {
            return await _context.Notifications
                .Where(n => n.ReceiverId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Include(n => n.Sender)
                .Include(n => n.Receiver)
                .ToListAsync();
        }

        public async Task<Notification?> GetSingleAsync(
       Expression<Func<Notification, bool>> predicate)
        {
            return await _context.Notifications
                .Include(n => n.Sender)
                .Include(n => n.Receiver)
                .FirstOrDefaultAsync(predicate);
        }
    }
}
