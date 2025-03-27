using Linkedin.Core.Data;
using Linkedin.DataAccess.Repositories.Interfaces;
using LinkedIn.Core.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.DataAccess.Repositories.Concrete
{
    public class UserRepository: Repository<ApplicationUser>, IUserRepository
    {
        public UserRepository(AppDbContext context) : base(context) { }

        public async Task<ApplicationUser> GetUserByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

 
        public async Task<ApplicationUser> GetuserByIdAsync(string id)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<ApplicationUser> GetUserWithPostsAsync(string userId)
        {
            return await _context.Users.Include(u => u.Posts).FirstOrDefaultAsync(u => u.Id == userId);
        }
    }
}
