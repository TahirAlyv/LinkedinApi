using Linkedin.DataAccess.Repositories.Concrete;
using LinkedIn.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.DataAccess.Repositories.Interfaces
{
    public interface IJobRepository: IRepository<JobApplication>
    {
        Task AddJobApplicationAsync(int JobPostId, string userId);
    }
}
