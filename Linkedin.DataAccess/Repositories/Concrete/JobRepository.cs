using Linkedin.Core.Data;
using Linkedin.DataAccess.Repositories.Interfaces;
using LinkedIn.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.DataAccess.Repositories.Concrete
{
    public class JobRepository:Repository<JobApplication>,IJobRepository
    {
        public JobRepository(AppDbContext context): base(context) {}

        public Task AddJobApplicationAsync(int JobPostId, string userId)
        {
            throw new NotImplementedException();
        }
    }
}
