using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Entities
{
    public class Like
    {
        public int Id { get; set; }
        public int PostId { get; set; }
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
        public Post Post { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool isLiked { get; set; } = true;

    }
}
