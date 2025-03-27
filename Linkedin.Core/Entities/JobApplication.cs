using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LinkedIn.Core.Enums;

namespace LinkedIn.Core.Entities
{
    public class JobApplication
    {
        public int Id { get; set; }
        public int JobPostId { get; set; }
        public string UserId { get; set; }
        public DateTime ApplicationDate { get; set; } = DateTime.UtcNow;
        public string ResumeUrl { get; set; }
        public ApplicationStatus Status { get; set; }
        public ApplicationUser User { get; set; }
        public JobPost JobPost { get; set; }

    }
}
