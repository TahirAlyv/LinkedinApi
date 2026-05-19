using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Dtos
{
    public class CommentNotificationDto
    {
        public int CommentId { get; set; }
        public int PostId { get; set; }
        public string UserId { get; set; }
        public string Content { get; set; }
        public string Username { get; set; }
        public string UserPhoto { get; set; }
        public DateTime CreatedAt { get; set; }
        public string PostOwnerId { get; set; }


    }
}
