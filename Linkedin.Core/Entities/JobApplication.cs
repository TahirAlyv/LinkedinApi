using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Entities
{
    public class JobApplication
    {
        public int Id { get; set; }

        public string ApplicantId { get; set; } = null!;
        public ApplicationUser Applicant { get; set; } = null!;

        public int JobPostId { get; set; }
        public JobPost JobPost { get; set; } = null!;

        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
    }
}
