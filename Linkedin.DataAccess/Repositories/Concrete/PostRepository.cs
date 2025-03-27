using Linkedin.Core.Data;
using Linkedin.DataAccess.Repositories.Interfaces;
using LinkedIn.Core.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.DataAccess.Repositories.Concrete
{
    public class PostRepository: Repository<Post>, IPostRepository
    {
        public PostRepository(AppDbContext context) : base(context) { }

         
 

    }
    
}
