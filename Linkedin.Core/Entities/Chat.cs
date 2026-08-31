using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Entities
{
    public class Chat
    {
        public int Id { get; set; }

        public string SenderId { get; set; } = null!;

        public string ReceiverId { get; set; } = null!;

        public DateTime CreatedAt { get; set; }

        // Each participant can clear the conversation only from their own view.
        public DateTime? SenderHiddenAt { get; set; }
        public DateTime? ReceiverHiddenAt { get; set; }

        // Employer-to-member conversations begin as a single invitation.
        // The employer cannot send another message until the member accepts.
        public bool RequiresAcceptance { get; set; }
        public Linkedin.Core.Enums.ChatInvitationStatus InvitationStatus { get; set; }
            = Linkedin.Core.Enums.ChatInvitationStatus.None;
        public string? InvitedByUserId { get; set; }
        public DateTime? InvitationRespondedAt { get; set; }

        public virtual ApplicationUser Sender { get; set; } = null!;

        public virtual ApplicationUser Receiver { get; set; } = null!;

        public virtual ICollection<Message> Messages { get; set; }
            = new List<Message>();
    }
}
