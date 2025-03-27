using LinkedIn.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinkedIn.Core.Entities
{
    public class FollowRequest
    {
        public int Id { get; set; }
        public string? SenderId { get; set; }
        public string? ReceiverId { get; set; }
        public FollowRequestStatus Status { get; set; } 
        public DateTime RequestDate { get; set; } = DateTime.UtcNow;
        public ApplicationUser Sender { get; set; }
        public ApplicationUser Receiver { get; set; }

    }
}
