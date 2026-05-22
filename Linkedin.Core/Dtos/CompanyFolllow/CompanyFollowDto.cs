using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Dtos.CompanyFolllow
{
    public class CompanyFollowDto
    {
        public string EmployerId { get; set; } = null!;
        public string? Username { get; set; }
        public string? CompanyName { get; set; }
        public string? Industry { get; set; }
        public string? LogoUrl { get; set; }
        public string? Location { get; set; }
        public DateTime FollowedAt { get; set; }
    }
}
