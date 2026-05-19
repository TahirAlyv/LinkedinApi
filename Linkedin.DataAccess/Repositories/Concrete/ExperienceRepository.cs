using Linkedin.Core.Data;
using Linkedin.Core.Entities;
using Linkedin.DataAccess.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.DataAccess.Repositories.Concrete
{
    public class ExperienceRepository:Repository<Experience>, IExperienceRepository
    {
        public ExperienceRepository(AppDbContext context) : base(context) { }


      

    }
}
