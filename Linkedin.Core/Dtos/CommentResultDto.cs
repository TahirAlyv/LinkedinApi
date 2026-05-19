using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Dtos
{
    public class CommentResultDto
    {
        public CommentDto Comment { get; set; }
        public NotificationDto? Notification { get; set; }
    }
}
