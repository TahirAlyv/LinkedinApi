using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Entities
{
    public class Connection
    {
        public int Id { get; set; }

        public string UserId { get; set; } = null!;
        public string ConnectedUserId { get; set; } = null!;

        public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;

        public ApplicationUser User { get; set; } = null!;
        public ApplicationUser ConnectedUser { get; set; } = null!;
    }
}
