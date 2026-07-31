using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Dtos
{
    public class SendMessageDto
    {
        public string? Content { get; set; }

        public List<IFormFile>? Files { get; set; }
    }
}
