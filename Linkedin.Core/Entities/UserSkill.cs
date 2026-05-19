using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Entities
{
    public class UserSkill
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
    }
}
