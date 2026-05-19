 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Dtos
{
    public class MessageDto
    {
        public string Sender { get; set; }
        public string Content { get; set; }
        public bool IsImage { get; set; }
        public DateTime DateTime { get; set; }
        public bool HasSeen { get; set; }
    }
}
