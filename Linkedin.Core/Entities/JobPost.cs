using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinkedIn.Core.Entities
{
    public class JobPost
    {
        public int Id { get; set; }
        public string EmployerId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Location { get; set; }
        public decimal? Salary { get; set; }
        public string? ImageUrl { get; set; }
        public string? VideoUrl { get; set; }
        public ApplicationUser Employer { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
