using Linkedin.Core.Entities;
 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.DataAccess.Repositories.Interfaces
{
    public interface IRefreshToken : IRepository<RefreshToken>
    {
        Task<RefreshToken?> GetByTokenAsync(string token);

        // BackgroundService üçün:
        // vaxtı bitmiş tokenləri gətirir
        Task<List<RefreshToken>> GetExpiredTokensAsync(DateTime utcNow);

        // Köhnə revoke olunmuş token təkrar istifadə edilərsə,
        // yalnız həmin browser/app session-unun aktiv tokenlərini tapır
        Task<List<RefreshToken>> GetActiveTokensByFamilyAsync(
            string userId,
            string tokenFamilyId);
    }
}
