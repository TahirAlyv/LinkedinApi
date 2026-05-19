using Linkedin.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Dtos.Connection
{
    public class ConnectionRequestDto
    {
        public int Id { get; set; }

        public ConnectionUserDto Sender { get; set; } = null!;
        public ConnectionUserDto Receiver { get; set; } = null!;

        public ConnectionRequestStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? RespondedAt { get; set; }
    }
}
