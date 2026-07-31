using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Dtos
{
    public class UpdatePostDto
    {
        public int PostId { get; set; }
        public IFormFile? File { get; set; }
        public string? Content { get; set; }
        public bool DeleteMedia { get; set; } = false;
        public int? MentionedCompanyId { get; set; }
        public bool ClearCompanyMention { get; set; } = false;
    }
}
