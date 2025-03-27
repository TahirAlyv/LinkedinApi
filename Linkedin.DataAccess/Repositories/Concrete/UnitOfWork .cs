using Linkedin.Core.Data;
using Linkedin.DataAccess.Repositories.Interfaces;
using LinkedIn.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.DataAccess.Repositories.Concrete
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        public IUserRepository Users { get; private set; }
        public IPostRepository Posts { get; private set; }
        public IFollowRepository Follows { get; private set; }
        public IChatRepository Chats { get; private set; }
        public IJobPostRepository JobPosts { get; private set; }
        public IJobRepository Jobs  { get; private set; }
        public ICommentRepository Comments { get; private set; }

        public ILikeRepository Likes { get; private set; }

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
            Users = new UserRepository(context);
            Posts = new PostRepository(context);
            Follows = new FollowRepository(context);
            Chats = new ChatRepository(context);
            JobPosts=new JobPostRepository(context);
            Jobs = new JobRepository(context);
            Comments= new CommentRepository(context);
            Likes=new LikeRepository(context);

        }

        public async Task<int> CompleteAsync() => await _context.SaveChangesAsync();
        public void Dispose() => _context.Dispose();


    }
}
