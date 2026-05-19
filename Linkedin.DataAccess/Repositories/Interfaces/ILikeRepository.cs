
using Linkedin.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.DataAccess.Repositories.Interfaces
{
    public interface ILikeRepository:IRepository<Like>
    {
        Task<List<Like>> GetLikesByPostIdAsync(int postId);
        Task<int> GetCountByPostIdAsync(int postId);
        Task<List<Like>> FindAsync(Expression<Func<Like, bool>> predicate);
        Task<Like> GetLikeWithUserAsync(int likeId);
    }
}
