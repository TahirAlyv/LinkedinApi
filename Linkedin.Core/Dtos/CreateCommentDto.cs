using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Dtos
{
    public class CreateCommentDto
    {
        [Required]
        public int PostId { get; set; }
        [Required]
        public string Text { get; set; }
    }
}
