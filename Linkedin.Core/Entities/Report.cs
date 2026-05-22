using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Entities
{
    public class Report
    {
        public int Id { get; set; }

        public string ReporterId { get; set; } = null!;
        public ApplicationUser Reporter { get; set; } = null!;

        public int PostId { get; set; }
        public Post Post { get; set; } = null!;

        public string Reason { get; set; } = null!;
        public bool IsReviewed { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
