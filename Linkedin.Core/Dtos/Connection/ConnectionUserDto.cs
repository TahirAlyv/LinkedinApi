using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Dtos.Connection
{
    public class ConnectionUserDto
    {
        public string Id { get; set; } = null!;
        public string? Username { get; set; }
        public string? FullName { get; set; }
        public string? CurrentPosition { get; set; }
        public string? ProfileImage { get; set; }
        public string? Location { get; set; }
        public DateTime? ConnectedAt { get; set; }
    }
}
