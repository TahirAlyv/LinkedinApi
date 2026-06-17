using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Dtos.Search
{
    public class SearchHistoryDto
    {
        public int Id { get; set; }

        public string Query { get; set; } = null!;

        public string NormalizedQuery { get; set; } = null!;

        public DateTime CreatedAt { get; set; }
    }
}
