using LinkedIn.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Dtos
{
    public class OtherUserDto
    {
        public string Username { get; set; }
        public string PhotoUrl { get; set; }
        public string Bio { get; set; }
        public int Followers {  get; set; }
        public int Following {  get; set; }
        public bool IsFollowing { get; set; }
        public Visibility Visibility { get; set; }
    }
}
