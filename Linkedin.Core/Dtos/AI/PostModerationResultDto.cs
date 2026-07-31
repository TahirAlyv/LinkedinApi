using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Dtos.AI
{
    public class PostModerationResultDto
    {
        public bool IsFlagged { get; set; }

        public string RiskLevel { get; set; } = "none";

        public List<string> Categories { get; set; } = new();

        public string Reason { get; set; } = string.Empty;

        public string SuggestedAction { get; set; } = "Published";
    }
}
