using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Entities
{
    public class SearchHistory
    {
        public int Id { get; set; }

        public string UserId { get; set; } = null!;

        public ApplicationUser User { get; set; } = null!;

        public string Query { get; set; } = null!;

        public string NormalizedQuery { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Null means visible in the user's recent searches. Setting this keeps
        // the row for analytics/audit while hiding it only from that user.
        public DateTime? HiddenAt { get; set; }
    }
}
