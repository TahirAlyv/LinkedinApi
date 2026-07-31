using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Dtos.Profile.Create
{
    public class CreateEducationDto
    {
        public string? School { get; set; }
        public int? InstitutionCompanyId { get; set; }
        public string? Degree { get; set; }
        public string? Field { get; set; }
        public int? StartMonth { get; set; }
        public int? StartYear { get; set; }
        public int? EndMonth { get; set; }
        public int? EndYear { get; set; }
        public string? Note { get; set; }
    }
}
