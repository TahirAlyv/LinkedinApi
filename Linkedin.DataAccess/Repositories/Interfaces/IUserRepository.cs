using Linkedin.Core.Dtos;
using LinkedIn.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.DataAccess.Repositories.Interfaces
{
    public interface IUserRepository: IRepository<ApplicationUser>
    {
        Task<ApplicationUser> GetUserByEmailAsync(string email);
        Task<ApplicationUser> GetuserByIdAsync(string id);
        Task<ApplicationUser> GetUserWithPostsAsync(string userId);
        Task<List<SearchedUserDto>> GetSearchUsers(string query, string username);

        Task<ApplicationUser> GetUserByUsername(string username);
        Task<ApplicationUser?> GetUserWithFollowersAsync(string username);

    }
}
