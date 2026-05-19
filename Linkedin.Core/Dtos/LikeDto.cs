using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Dtos
{
    public class LikeDto
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int PostId { get; set; }
        public string ProfileImage { get; set; }
        public string UserName { get; set; }
        public DateTime CreatedAt { get; set; }
}
}
