using Linkedin.Core.Data;
using Linkedin.Core.Entities;
using Linkedin.DataAccess.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

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
                .OrderByDescending(n => n.LastTriggeredAt ?? n.CreatedAt)
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

        public async Task MarkAllAsReadAsync(string userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.ReceiverId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }
        }
    }
}