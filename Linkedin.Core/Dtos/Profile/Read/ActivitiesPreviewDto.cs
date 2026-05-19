using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Dtos.Profile.Read
{
    public class ActivitiesPreviewDto
    {

        public int PostsCount { get; set; }
        public List<PostPreviewDto>? RecentPosts { get; set; }



    }
}
