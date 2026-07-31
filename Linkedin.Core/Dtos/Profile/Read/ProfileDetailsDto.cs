using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Dtos.Profile.Read
{
    public class ProfileDetailsDto
    {
        public string UserType { get; set; } = null!;
        public string? Role { get; set; }

        public BasicInfoDto? BasicInfo { get; set; }
        public ContactInfoDto? ContactInfo { get; set; }
        public AboutDto? About { get; set; }

        public CompanyInfoDto? CompanyInfo { get; set; }

        public List<ExperienceDto>? Experiences { get; set; }
        public List<EducationDto>? Educations { get; set; }
        public List<SkillDto>? Skills { get; set; }
        public ActivitiesPreviewDto? ActivitiesPreview { get; set; }
        public OpenToWorkProfileDto? OpenToWork { get; set; }
    }

    public class OpenToWorkProfileDto
    {
        public bool IsOpenToWork { get; set; }
        public List<string> JobTitles { get; set; } = new();
        public List<string> WorkplaceTypes { get; set; } = new();
        public List<string> OnsiteLocations { get; set; } = new();
        public List<string> RemoteLocations { get; set; } = new();
        public List<string> EmploymentTypes { get; set; } = new();
        public string StartAvailability { get; set; } = "Immediately";
        public DateTime? UpdatedAt { get; set; }
    }

    public class CompanyInfoDto
    {
        public string? Name { get; set; }
        public string? Industry { get; set; }
        public string? Bio { get; set; }
        public string? Website { get; set; }
        public string? Location { get; set; }
        public string? LogoUrl { get; set; }
        public bool IsVerified { get; set; }
        public string? CompanySize { get; set; }
        public int? FoundedYear { get; set; }
        public string? Tagline { get; set; }
    }
}
