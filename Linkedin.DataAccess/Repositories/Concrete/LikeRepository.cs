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
    public class LikeRepository : Repository<Like>, ILikeRepository
    {
        public LikeRepository(AppDbContext context):base(context) { }

        public async Task<int> GetCountByPostIdAsync(int postId)
        {
            return await _context.Likes.CountAsync(l => l.PostId == postId);
        }

        public async Task<List<Like>> GetLikesByPostIdAsync(int postId)
        {
            return await _context.Likes
                .Where(l => l.PostId == postId)
                .Include(l => l.User)
                .ToListAsync();
        }

        public async Task<List<Like>> FindAsync(Expression<Func<Like, bool>> predicate)
        {
            return await _context.Likes
                                 .Where(predicate)
                                 .Include(l => l.User)  
                                 .ToListAsync();
        }

    

        public async Task<Like> GetLikeWithUserAsync(int likeId)
        {
            return await _context.Likes
                .Include(l => l.User)
                .FirstOrDefaultAsync(l => l.Id == likeId);
        }

    }
}
