using Linkedin.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.DataAccess.Repositories.Interfaces
{
    public interface IUserSkillRepository:IRepository<UserSkill>
    {
        Task<UserSkill?> GetByIdAsync(int id);
        Task<UserSkill?> GetUserSkillByNameAsync(string userId, string name);
        Task<List<UserSkill>> GetUserSkillsAsync(string userId);
    }
}
