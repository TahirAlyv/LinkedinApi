using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Dtos.Ai
{
    public class AiRateLimitResult
    {
        public bool Allowed { get; set; }

        public int RetryAfterSeconds { get; set; }

        public string Message { get; set; } = string.Empty;
    }
}