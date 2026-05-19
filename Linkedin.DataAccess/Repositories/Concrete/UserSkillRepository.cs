using Linkedin.Core.Data;
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
    public class UserSkillRepository:Repository<UserSkill>, IUserSkillRepository
    {
        public UserSkillRepository(AppDbContext context) : base(context) { }
 
        public async Task<UserSkill?> GetByIdAsync(int id)
        {
            return await _context.Set<UserSkill>()
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<UserSkill?> GetUserSkillByNameAsync(string userId, string name)
        {
            return await _context.Set<UserSkill>()
                .FirstOrDefaultAsync(x => x.UserId == userId && x.Name.ToLower() == name.ToLower());
        }
        public async Task<List<UserSkill>> GetUserSkillsAsync(string userId)
        {
            return await _context.Set<UserSkill>()
                .Where(x => x.UserId == userId)
                .ToListAsync();
        }


    }


}
