using LinkedIn.Core.Entities;
using Linkedin.DataAccess.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Linkedin.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace Linkedin.DataAccess.Repositories.Concrete
{
    public class FollowRepository: Repository<Follow>, IFollowRepository
    {
        public FollowRepository(AppDbContext context) : base(context) { }

        public async Task<Follow> GetFollowRelationAsync(string followerId, string followingId)
        {
            return await _context.Follows.FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowingId == followingId);
        }

        public async Task<bool> IsFollowingAsync(string followerId, string followingId)
        {
            return await _context.Follows.AnyAsync(f => f.FollowerId == followerId && f.FollowingId == followingId);
        }
    }
}
