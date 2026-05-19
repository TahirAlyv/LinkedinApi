using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Entities
{

    public class Experience
    {
        public int Id { get; set; }

        public string Title { get; set; }
        public string? EmploymentType { get; set; }
        public string CompanyName { get; set; }

        public bool IsCurrent { get; set; }

        public int? StartMonth { get; set; }
        public int? StartYear { get; set; }
        public int? EndMonth { get; set; }
        public int? EndYear { get; set; }

        public string? Location { get; set; }
        public string? LocationType { get; set; }
        public string? Description { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
    }
}
