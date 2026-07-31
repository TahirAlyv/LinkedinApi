using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Dtos
{
    public class MessageDeleteResultDto
    {
        public int MessageId { get; set; }

        public int ChatId { get; set; }

        public string SenderId { get; set; } = null!;

        public string ReceiverId { get; set; } = null!;

        public DateTime DeletedAt { get; set; }
    }
}