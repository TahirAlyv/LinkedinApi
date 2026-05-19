using Linkedin.Core.Data;
using Linkedin.Core.Entities;
using Linkedin.DataAccess.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.DataAccess.Repositories.Concrete
{
    public class EducationRepository: Repository<Education>, IEducationRepository 
    {
        public EducationRepository (AppDbContext context) : base(context) { }

    }
}
