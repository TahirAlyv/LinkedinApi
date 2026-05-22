using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
 

namespace Linkedin.Core.Entities
{
    public class Post
    {
        public int Id { get; set; }
        public string UserID { get; set; }
        public ApplicationUser User { get; set; }
        public string? ImageUrl { get; set; }
        public string? Content { get; set; }
        public string? VideoUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? CommentCount { get; set; }
        public int? LikeCount { get; set; } 
        public ICollection<Like>? Likes { get; set; }
        public ICollection<Comment>? Comments { get; set; }
        public bool IsBlocked { get; set; } = false;
        public string? BlockReason { get; set; }

    }
}
