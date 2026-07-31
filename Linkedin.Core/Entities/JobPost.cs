using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Entities
{
    public class JobPost
    {
        public int Id { get; set; }

        public string EmployerId { get; set; } = null!;
        public ApplicationUser Employer { get; set; } = null!;

        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;

        public string? Location { get; set; }

        // On-site / Remote / Hybrid
        public string WorkplaceType { get; set; } = "On-site";

        // Full-time / Part-time / Internship / Contract
        public string EmploymentType { get; set; } = "Full-time";

        // Company career page / external apply link
        public string? ApplyUrl { get; set; }
        public string? RequiredSkills { get; set; }
        public int MinimumExperienceYears { get; set; }
        public bool IsBlocked { get; set; } = false;
        public string? BlockReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public DateTime? ExpiresAt { get; set; }

        // false => applications closed
        public bool IsActive { get; set; } = true;

        public ICollection<JobApplication> Applications { get; set; } = new List<JobApplication>();
        public ICollection<SavedJob> SavedJobs { get; set; } = new List<SavedJob>();
        public ICollection<JobInvitation> Invitations { get; set; } = new List<JobInvitation>();
    }
}
