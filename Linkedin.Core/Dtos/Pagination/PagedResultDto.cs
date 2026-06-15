using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Dtos.Pagination
{
    public class PagedResultDto<T>
    {
        public List<T> Items { get; set; } = new();

        public int PageNumber { get; set; }

        public int PageSize { get; set; }

        public int TotalCount { get; set; }

        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}
