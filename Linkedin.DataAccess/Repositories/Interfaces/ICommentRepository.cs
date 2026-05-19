
using Linkedin.Core.Dtos;
using Linkedin.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.DataAccess.Repositories.Interfaces
{
    public interface ICommentRepository:IRepository<Comment>
    {
        Task<Comment?> GetWithIncludesAsync(int commentId);
        Task<List<CommentDto>> GetByPostIdAsync(int postId, int page, int pageSize);
        Task<int?> GetPostIdByCommentIdAsync(int commentId);
        Task<int> GetCommentCountByPostIdAsync(int postId);



    }
}
