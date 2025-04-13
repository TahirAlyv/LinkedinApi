using LinkedIn.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Dtos
{
    public class UserDto
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string PhotoUrl { get; set; }
        public string Bio {  get; set; }
        public Visibility Visibility { get; set; }
        public string Skills { get; set; }
        public string Experience { get; set; }
    }
}
