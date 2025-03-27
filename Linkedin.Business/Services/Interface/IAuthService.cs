using Linkedin.Core.Dtos;
using LinkedIn.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Business.Services.Interface
{
    public interface IAuthService
    {
        Task<string> GenerateTokeen(ApplicationUser user);
        Task AssignRole(ApplicationUser user, string role);
    }
}
