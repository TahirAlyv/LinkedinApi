 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Dtos
{
    public class MessageDto
    {
        public int Id { get; set; }

        public int ChatId { get; set; }

        public string Sender { get; set; } = null!;

        public string SenderId { get; set; } = null!;

        public string? SenderProfileImage { get; set; }

        public string? Receiver { get; set; }

        public string? ReceiverId { get; set; }

        public string? Content { get; set; }

        // Köhnə frontend ilə uyğunluq üçün hələlik qalır
        public bool IsImage { get; set; }

        public DateTime DateTime { get; set; }

        public bool HasSeen { get; set; }

        public List<ChatAttachmentDto> Attachments { get; set; }
            = new List<ChatAttachmentDto>();
    }
}
