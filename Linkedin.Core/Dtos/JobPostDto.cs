 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Dtos
{
    public class JobPostDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Location { get; set; }
        public decimal? Salary { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? EmployerName { get; set; }
        public object EmployerPhoto { get; set; }
        public string Role { get; set; }
        public string? Skills { get; set; }
        public string? ImageUrl { get; set; }
        public string? VideoUrl { get; set; }
    }
}
