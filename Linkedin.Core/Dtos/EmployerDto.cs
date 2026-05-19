using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Dtos
{
    public class EmployerDto
    {
        public string? Id { get; set; }
        public string Email { get; set; }
        public string UserName {  get; set; }
        public string CompanyName { get; set; }
        public string? Industry { get; set; }
        public string? Bio { get; set; }
        public string? Website { get; set; }
        public string? Location { get; set; }
        public string? LogoUrl { get; set; }
        public bool IsVerified { get; set; } = false;
        public string Role { get; set; }

    }
}
