using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Dtos.Profile.Update
{
    public class UpdateEmployerCompanyInfoDto
    {
        public string Name { get; set; } = null!;
        public string? Username { get; set; }
        public string? Tagline { get; set; }
        public string? Industry { get; set; }
        public string? Location { get; set; }
        public string? Bio { get; set; }
        public string? CompanySize { get; set; }
        public int? FoundedYear { get; set; }
    }
}
