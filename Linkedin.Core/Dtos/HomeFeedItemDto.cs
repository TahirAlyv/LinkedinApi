using Linkedin.Core.Dtos.JobPost.Read;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Dtos
{
    public class HomeFeedItemDto
    {
        public string ItemType { get; set; } = null!; // "post" | "job"
        public DateTime CreatedAt { get; set; }

        public PostDto? Post { get; set; }
        public JobPostDto? JobPost { get; set; }
    }
}
