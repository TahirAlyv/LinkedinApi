using Linkedin.Core.Common;
using Linkedin.Core.Dtos;
using Linkedin.Core.Entities;
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
        Task<ServiceResult> SaveRefreshTokenAsync(ApplicationUser user, string refreshToken);
        Task<ServiceResult> RefreshAccessTokenAsync(string refreshToken);
        string GenerateRefreshToken(); 
    }
}
