using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Dtos.Profile.Update
{
    public class UpdateContactInfoDto
    {
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? PhoneType { get; set; }
        public string? Address { get; set; }
        public int? BirthMonth { get; set; }
        public int? BirthDay { get; set; }
    }
}
