using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Dtos.Profile.Read
{
    public class ExperienceDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? EmploymentType { get; set; }
        public string CompanyName { get; set; } = null!;
        public int? CompanyId { get; set; }
        public string? CompanyLogoUrl { get; set; }
        public string? CompanyUsername { get; set; }
        public bool? IsCurrent { get; set; }
        public int? StartMonth { get; set; }
        public int? StartYear { get; set; }
        public int? EndMonth { get; set; }
        public int? EndYear { get; set; }
        public string? Location { get; set; }
        public string? LocationType { get; set; }
        public string? Description { get; set; }
    }
}
