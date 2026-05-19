using Linkedin.Core.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Dtos
{
    public class UpdateJobPostDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Location { get; set; }
        public decimal? Salary { get; set; }
        public string? ImageUrl { get; set; }
        public string? VideoUrl { get; set; }
        public string? Skills { get; set; }
        public bool DeleteMedia { get; set; } = false;
        public IFormFile? File { get; set; }

    }
}
