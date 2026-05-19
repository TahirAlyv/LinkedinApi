using Linkedin.Core.Data;
using Linkedin.Core.Dtos;
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
    public class CommentRepository:Repository<Comment>, ICommentRepository
    {
        public CommentRepository(AppDbContext context) : base(context) { }

        public async Task<List<CommentDto>> GetByPostIdAsync(int postId, int page, int pageSize)
        {
            var skip = (page - 1) * pageSize;

            var comments = await _context.Comments
                .Where(c => c.PostId == postId)
                .Include(c => c.User)
                .OrderBy(c => c.CreatedAt)
                .Skip(skip)
                .Take(pageSize)
                .Select(c => new CommentDto
                {
                    Id = c.Id,
                    UserPhoto = c.User.ProfileImage!,
                    Username = c.User.UserName!,
                    UserId = c.UserId,
                    CreatedAt = c.CreatedAt,
                    Text = c.Text
                })
                .ToListAsync();

            return comments;
        }

        public async Task<Comment?> GetWithIncludesAsync(int commentId)
        {
            return await _context.Comments
            .Include(c => c.User)
            .Include(c => c.Post)
            .FirstOrDefaultAsync(c => c.Id == commentId);
        }

        public async Task<int?> GetPostIdByCommentIdAsync(int commentId)
        {
            return await _context.Comments
                .Where(c => c.Id == commentId)
                .Select(c => (int?)c.PostId)
                .FirstOrDefaultAsync();
        }

        public async Task<int> GetCommentCountByPostIdAsync(int postId)
        {
            return await _context.Comments
                .CountAsync(c => c.PostId == postId);
        }
    }
}
