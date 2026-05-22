using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Entities
{
    public class Company
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Tagline { get; set; }
        public string? Industry { get; set; }
        public string? Bio { get; set; }
        public string? Website { get; set; }
        public string? Location { get; set; }
        public string? LogoUrl { get; set; }
        public string? CompanySize { get; set; }
        public int? FoundedYear { get; set; }
        public bool IsVerified { get; set; } = false;
        public string UserId { get; set; }
        public ApplicationUser? User { get; set; }
    }
}
