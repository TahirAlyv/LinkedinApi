using Linkedin.Core.Dtos;
using Linkedin.Core.Dtos.Pagination;
using Linkedin.Core.Dtos.Profile.Read;
using Linkedin.Core.Entities;
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
        Task<ApplicationUser> GetUserWithPostsAsync(string userId);
        Task<List<SearchedUserDto>> GetSearchUsers(string query);
        Task<ApplicationUser> GetUserByUsername(string username);
        IQueryable<ApplicationUser> GetQuery();
        Task<ProfileDetailsDto?> GetMyProfileDetailsAsync(string userId, string currentUserRole);

        Task<bool> IsUsernameTakenAsync(string username, string currentUserId);
        Task<bool> IsEmailTakenAsync(string email, string currentUserId);
        Task<ProfileDetailsDto?> GetProfileDetailsByUsernameAsync(string username, string currentUserId, string targetUserRole);
        Task<UserLookupDto?> GetUserByUsernameAsync(string username);

        Task<PagedResultDto<SearchedUserDto>> GetEmployersPagedAsync(
            string currentUserId,
            int pageNumber,
            int pageSize);

        Task<PagedResultDto<SearchedUserDto>> GetJobSeekersPagedAsync(
            string currentUserId,
            int pageNumber,
            int pageSize);

        Task AddSearchHistoryAsync(string userId, string query);



    }
}
