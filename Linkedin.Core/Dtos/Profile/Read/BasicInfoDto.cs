using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Dtos.Profile.Read
{
    public class BasicInfoDto
    {
        public string ? Id { get; set; }
        public string? FullName { get; set; }
        public string? CurrentPosition { get; set; }
        public string? ProfileImage { get; set; }
        public string? BackgroundImage { get; set; }
        public string? Username { get; set; }
        public string? Location { get; set; }
       
    }
}
