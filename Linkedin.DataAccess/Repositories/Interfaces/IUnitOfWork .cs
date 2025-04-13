using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.DataAccess.Repositories.Interfaces
{
    public interface IUnitOfWork: IDisposable
    {
        IUserRepository Users { get; }
        IPostRepository Posts { get; }
        IJobPostRepository JobPosts { get; }
        IFollowRepository Follows { get; }
        IChatRepository Chats { get; }
        IJobRepository Jobs { get; }
        ICommentRepository Comments { get; }
        ILikeRepository Likes { get; }
        IRefreshToken RefreshTokens { get; }

        Task<int> CompleteAsync();
    }
}
