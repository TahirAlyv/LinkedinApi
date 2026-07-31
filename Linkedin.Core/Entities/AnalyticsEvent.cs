using System;
using Linkedin.Core.Enums;

namespace Linkedin.Core.Entities
{
    public class AnalyticsEvent
    {
        public long Id { get; set; }
        public AnalyticsEventType EventType { get; set; }
        public string ViewerUserId { get; set; } = null!;
        public ApplicationUser ViewerUser { get; set; } = null!;
        public string TargetUserId { get; set; } = null!;
        public ApplicationUser TargetUser { get; set; } = null!;
        public int? PostId { get; set; }
        public Post? Post { get; set; }
        public int? JobPostId { get; set; }
        public JobPost? JobPost { get; set; }
        public string? SearchQuery { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
