using Linkedin.Core.Data;
using Linkedin.Core.Dtos;
using Linkedin.DataAccess.Repositories.Interfaces;
using LinkedIn.Core.Entities;
using LinkedIn.Core.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.DataAccess.Repositories.Concrete
{
    public class UserRepository : Repository<ApplicationUser>, IUserRepository
    {
        public UserRepository(AppDbContext context) : base(context) { }

        public async Task<List<SearchedUserDto>> GetSearchUsers(string query,string username)
        {
            return await _context.Users
            .Where(u => u.UserName.Contains(query) && u.UserName!=username)
            .Select(u => new SearchedUserDto
            {
                UserName=u.UserName,
                ProfileImage=u.ProfileImage,
                Bio=u.Bio,
                Visibility=u.Visibility,

            }) 
            .ToListAsync();

        }

        public async Task<ApplicationUser> GetUserByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

 
        public async Task<ApplicationUser> GetuserByIdAsync(string id)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<ApplicationUser> GetUserByUsername(string username)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.UserName == username);
        }

        public async Task<ApplicationUser> GetUserWithPostsAsync(string userId)
        {
            return await _context.Users.Include(u => u.Posts).FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<ApplicationUser?> GetUserWithFollowersAsync(string username)
        {
           var user = await _context.Users
                .Include(u => u.Followers)
                .Include(u => u.Following)
                .FirstOrDefaultAsync(u => u.UserName == username);

            return user;
        }
    }
}
