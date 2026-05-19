using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Dtos.Profile.Update
{
    public class UpdateBasicInfoDto
    {
        public string? FullName { get; set; }
        public string? CurrentPosition { get; set; }
        public string? Username { get; set; }
        public string? Location { get; set; }
        public string? Email { get; set; }
        public string? CurrentPassword { get; set; }
    }
}
