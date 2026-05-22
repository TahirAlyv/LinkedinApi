using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Dtos.JobPost.Create
{
    public class CreateJobPostDto
    {
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;

        public string? Location { get; set; }

        public string WorkplaceType { get; set; } = "On-site";
        public string EmploymentType { get; set; } = "Full-time";

        public string? ApplyUrl { get; set; }

        public DateTime? ExpiresAt { get; set; }
    }
}
