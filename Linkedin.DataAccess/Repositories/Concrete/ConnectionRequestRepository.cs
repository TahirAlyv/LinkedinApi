using Linkedin.Core.Data;
using Linkedin.Core.Entities;
using Linkedin.Core.Enums;
using Linkedin.DataAccess.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.DataAccess.Repositories.Concrete
{
    public class ConnectionRequestRepository
         : Repository<ConnectionRequest>, IConnectionRequestRepository
    {
        public ConnectionRequestRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<ConnectionRequest?> GetPendingRequestBetweenUsersAsync(
            string firstUserId,
            string secondUserId)
        {
            return await _context.ConnectionRequests
                .FirstOrDefaultAsync(cr =>
                    cr.Status == ConnectionRequestStatus.Pending &&
                    (
                        (cr.SenderId == firstUserId && cr.ReceiverId == secondUserId) ||
                        (cr.SenderId == secondUserId && cr.ReceiverId == firstUserId)
                    ));
        }

        public async Task<ConnectionRequest?> GetRequestWithUsersAsync(int requestId)
        {
            return await _context.ConnectionRequests
                .Include(cr => cr.Sender)
                .Include(cr => cr.Receiver)
                .FirstOrDefaultAsync(cr => cr.Id == requestId);
        }

        public async Task<List<ConnectionRequest>> GetReceivedPendingRequestsAsync(
            string currentUserId)
        {
            return await _context.ConnectionRequests
                .Include(cr => cr.Sender)
                .Include(cr => cr.Receiver)
                .Where(cr =>
                    cr.ReceiverId == currentUserId &&
                    cr.Status == ConnectionRequestStatus.Pending)
                .OrderByDescending(cr => cr.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<ConnectionRequest>> GetSentPendingRequestsAsync(
            string currentUserId)
        {
            return await _context.ConnectionRequests
                .Include(cr => cr.Sender)
                .Include(cr => cr.Receiver)
                .Where(cr =>
                    cr.SenderId == currentUserId &&
                    cr.Status == ConnectionRequestStatus.Pending)
                .OrderByDescending(cr => cr.CreatedAt)
                .ToListAsync();
        }
    }
}
