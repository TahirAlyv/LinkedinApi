using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Dtos.Profile.Update
{
    public class UpdateEmployerContactInfoDto
    {
        public string? Website { get; set; }
        public string? Email { get; set; }
        public string? CurrentPassword { get; set; }
        public string? PhoneNumber { get; set; }

        public bool ChangeEmail { get; set; }
    }
}
