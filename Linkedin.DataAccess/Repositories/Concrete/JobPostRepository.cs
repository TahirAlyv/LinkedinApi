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
    public class JobPostRepository : Repository<JobPost>, IJobPostRepository
    {
        public JobPostRepository(AppDbContext dbContext) : base(dbContext) { }
 
    }
}
