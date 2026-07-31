 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Dtos
{
    public class PostDto
    {

        public int Id { get; set; }
        public string PostOwnerId { get; set; }
        public string Username { get; set; }
        public string? UserPhoto { get; set; }
        public string Role { get; set; }
        public string? ImageUrl { get; set; }
        public string? Content { get; set; }
        public string? VideoUrl { get; set; }
        public int? MentionedCompanyId { get; set; }
        public string? MentionedCompanyName { get; set; }
        public string? MentionedCompanyUsername { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? CommentCount { get; set; }
        public int? LikeCount { get; set; }
        public bool IsLikedByCurrentUser { get; set; }
        public bool IsSaved { get; set; }
        public string ModerationStatus { get; set; } = "Published";

        public bool IsAiFlagged { get; set; }

        public string? AiModerationReason { get; set; }



    }
}
