using Linkedin.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Dtos
{
    public class CommentDto
    {
        public int Id { get; set; }
        public string UserPhoto { get; set; }
        public string Username { get; set; }
        public string UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int PostId { get; set; }
        public string Text { get; set; }




    }
}
