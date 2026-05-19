 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Linkedin.Core.Enums;

namespace Linkedin.Core.Entities
{
    public class Notification
    {
        public int Id { get; set; }

        /* Kim -> Kimə */
        public string SenderId { get; set; } = null!;
        public string ReceiverId { get; set; } = null!;

        /* Tip */
        public NotificationType Type { get; set; }

        /* Context (hansı obyektlə bağlıdır) */
        public int? PostId { get; set; }
        public int? CommentId { get; set; }

        /* UI üçün SNAPSHOT data */
        public string SenderUsername { get; set; } = null!;
        public string? SenderProfilePhoto { get; set; }
        public string? ContentPreview { get; set; } // comment text / empty

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;       // ilk dəfə
        public DateTime? LastTriggeredAt { get; set; }
        public bool IsRead { get; set; } = false;

        public ApplicationUser? Sender;
        public ApplicationUser? Receiver;
    }



}
