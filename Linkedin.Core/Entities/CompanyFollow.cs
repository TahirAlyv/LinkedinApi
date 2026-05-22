using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Entities
{
    public class CompanyFollow
    {
        public int Id { get; set; }

        public string FollowerId { get; set; } = null!;
        public ApplicationUser Follower { get; set; } = null!;

        public string EmployerId { get; set; } = null!;
        public ApplicationUser Employer { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
