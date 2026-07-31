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

        public virtual ApplicationUser Sender { get; set; } = null!;

        public virtual ApplicationUser Receiver { get; set; } = null!;

        public virtual ICollection<Message> Messages { get; set; }
            = new List<Message>();
    }
}
