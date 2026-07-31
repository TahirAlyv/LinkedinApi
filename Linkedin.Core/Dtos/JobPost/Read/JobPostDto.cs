using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Dtos.JobPost.Read
{
    public class JobPostDto
    {
        public int Id { get; set; }

        public string EmployerId { get; set; } = null!;

        public string? CompanyName { get; set; }
        public string? CompanyLogo { get; set; }
        public string? CompanyUsername { get; set; }
        public string? Industry { get; set; }

        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;

        public string? Location { get; set; }

        public string WorkplaceType { get; set; } = null!;
        public string EmploymentType { get; set; } = null!;

        public string? ApplyUrl { get; set; }
        public List<string> RequiredSkills { get; set; } = new();
        public int MinimumExperienceYears { get; set; }
        public int MatchingTalentCount { get; set; }
        public int ExternalApplyClicks { get; set; }
        public int SaveCount { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }

        public bool IsActive { get; set; }
        public bool IsExpired { get; set; }
        public bool CanApply { get; set; }
        public bool HasApplyUrl { get; set; }

        public bool IsOwner { get; set; }
        public bool IsSaved { get; set; }
        public bool IsApplied { get; set; }
        public DateTime? AppliedAt { get; set; }
    }

}
