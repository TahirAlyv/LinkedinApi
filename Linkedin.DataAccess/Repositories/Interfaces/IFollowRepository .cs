using LinkedIn.Core.Entities;
using Linkedin.DataAccess.Repositories.Concrete;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.DataAccess.Repositories.Interfaces
{
    public interface IFollowRepository: IRepository<Follow>
    {
        Task<bool> IsFollowingAsync(string followerId, string followingId);
    }
}
