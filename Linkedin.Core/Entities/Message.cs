using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinkedIn.Core.Entities
{
    public class Message
    {
        public int Id { get; set; }
        public string SenderId { get; set; }
        public string Content { get; set; }
        public bool IsImage { get; set; }
        public int ChatId { get; set; }
        public DateTime DateTime { get; set; }
        public ApplicationUser Sender { get; set; }
        public virtual Chat Chat { get; set; }
        public bool HasSeen { get; set; }
    }
}
