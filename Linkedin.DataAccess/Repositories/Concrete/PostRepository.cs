using Linkedin.Core.Data;
using Linkedin.Core.Dtos;
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
    public class PostRepository : Repository<Post>, IPostRepository
    {
        public PostRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<List<Post>> GetAllPostsByFriendIdsAsync(List<string> friendIds)
        {
            return await _context.Posts
                 .Where(p => friendIds.Contains(p.UserID) && !p.IsBlocked)
                .Include(p => p.User)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<Post?> GetUserPostAsync(string userId, int postId)
        {
            var post = await _context.Posts
                .Include(p => p.Comments)
                .Include(p => p.Likes)
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserID == userId && p.Id == postId);

            return post;
        }

        public async Task<List<Post>> GetPostsByUserIdAsync(string userId, int skip, int take)
        {
            return await _context.Posts
                .Where(p => p.UserID == userId && !p.IsBlocked)
                .Include(p => p.Likes)
                .Include(p => p.Comments)
                .Include(p => p.User)
                    .ThenInclude(u => u.Company)
                .OrderByDescending(p => p.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<List<Post>> GetHomeFeedPostsAsync(
            List<string> allowedUserIds,
            int skip,
            int take)
        {
            if (allowedUserIds == null || !allowedUserIds.Any())
                return new List<Post>();

            return await _context.Posts
                .AsNoTracking()
                 .Where(p => allowedUserIds.Contains(p.UserID) && !p.IsBlocked)
                .Include(p => p.Likes)
                .Include(p => p.Comments)
                .Include(p => p.User)
                    .ThenInclude(u => u.Company)
                .OrderByDescending(p => p.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<Post?> GetPostByIdAsync(
            int postId,
            params Expression<Func<Post, object>>[] includes)
        {
            IQueryable<Post> query = _context.Posts;

            foreach (var include in includes)
            {
                query = query.Include(include);
            }

            return await query.FirstOrDefaultAsync(p => p.Id == postId);
        }
    }

}
 