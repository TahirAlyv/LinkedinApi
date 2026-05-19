using Linkedin.Core.Entities;
using Linkedin.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Dtos
{
    public class NotificationReturnDto
    {
        public int Id { get; set; }

        public string SenderId { get; set; } = null!;
        public string ReceiverId { get; set; } = null!;
 
        public NotificationType Type { get; set; }
 
        public int? PostId { get; set; }
        public int? CommentId { get; set; }
 
        public string SenderUsername { get; set; } = null!;
        public string? SenderProfilePhoto { get; set; }
        public string? ContentPreview { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? LastTriggeredAt { get; set; }
        public bool IsRead { get; set; } = false;
 
    }
}
