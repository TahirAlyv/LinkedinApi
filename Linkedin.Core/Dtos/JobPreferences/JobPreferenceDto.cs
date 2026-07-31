using System;
using System.Collections.Generic;

namespace Linkedin.Core.Dtos.JobPreferences
{
    public class JobPreferenceDto
    {
        public List<string> JobTitles { get; set; } = new();
        public List<string> Locations { get; set; } = new();
        public List<string> WorkplaceTypes { get; set; } = new();
        public List<string> EmploymentTypes { get; set; } = new();
        public bool IsOpenToWork { get; set; }
        public List<string> OnsiteLocations { get; set; } = new();
        public List<string> RemoteLocations { get; set; } = new();
        public string StartAvailability { get; set; } = "Immediately";
        public DateTime? UpdatedAt { get; set; }
    }

    public class RecommendedJobDto
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
        public int MatchScore { get; set; }
        public string RecommendationReason { get; set; } = "Recommended for you";
        public bool IsFromFollowedCompany { get; set; }
        public bool HasProfileMatch { get; set; }
    }
}
