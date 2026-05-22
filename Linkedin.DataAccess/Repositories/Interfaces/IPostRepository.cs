using Linkedin.Core.Dtos;
using Linkedin.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.DataAccess.Repositories.Interfaces
{
    public interface IPostRepository : IRepository<Post>
    {
        Task<List<Post>> GetAllPostsByFriendIdsAsync(List<string> friendIds);

        Task<Post?> GetUserPostAsync(string userId, int postId);

        Task<List<Post>> GetPostsByUserIdAsync(string userId, int skip, int take);

        Task<List<Post>> GetHomeFeedPostsAsync(
            List<string> allowedUserIds,
            int skip,
            int take);

        Task<Post?> GetPostByIdAsync(
            int postId,
            params Expression<Func<Post, object>>[] includes);
    }
};
