using LinkedIn.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Business.Services.Interface
{
    public interface IUserService
    {
        Task<ApplicationUser?> GetAuthenticatedUserAsync(ClaimsPrincipal user);
    }
}
