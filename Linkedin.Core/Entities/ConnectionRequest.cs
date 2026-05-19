
using Linkedin.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Entities
{
    public class ConnectionRequest
    {
        public int Id { get; set; }

        public string SenderId { get; set; } = null!;
        public string ReceiverId { get; set; } = null!;

        public ConnectionRequestStatus Status { get; set; } = ConnectionRequestStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RespondedAt { get; set; }

        public ApplicationUser Sender { get; set; } = null!;
        public ApplicationUser Receiver { get; set; } = null!;
    }
}
