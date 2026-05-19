using Linkedin.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Dtos
{
    public class SearchedUserDto
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string ProfileImage { get; set; }
        public string Bio {  get; set; }    
        public Visibility Visibility { get; set; }
        public string Role { get; set; }
        public bool IsFollowing { get; set; }
    }
}
