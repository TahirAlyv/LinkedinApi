using Linkedin.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Dtos.Profile.Read
{
    public class ContactInfoDto
    {
        public string? Email { get; set; }
        public PhoneType? PhoneType { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? Website { get; set; }
        public int? BirthMonth { get; set; }
        public int? BirthDay { get; set; }
    }
}
