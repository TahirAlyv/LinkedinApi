 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Dtos
{
    public class ChatDto
    {
        public string Sender { get; set; }
        public string Receiver { get; set; }
        public string SenderProfilImage { get; set; }
        public string ReveiverProfilImage { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<MessageDto> Messages { get; set; }
        = new List<MessageDto>();
    }
}
