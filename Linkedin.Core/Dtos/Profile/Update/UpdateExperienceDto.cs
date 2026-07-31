using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Dtos.Profile.Update
{
    public class UpdateExperienceDto
    {
        public string Title { get; set; } = null!;
        public string? EmploymentType { get; set; }
        public string CompanyName { get; set; } = null!;
        public int? CompanyId { get; set; }
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
