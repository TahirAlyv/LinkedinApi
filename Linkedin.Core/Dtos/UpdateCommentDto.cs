using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Dtos
{
    using System.ComponentModel.DataAnnotations;

    public class UpdateCommentDto
    {
        [Required(ErrorMessage = "Comment text is required.")]
        [MinLength(1, ErrorMessage = "Comment text cannot be empty.")]
        [MaxLength(1000, ErrorMessage = "Comment text is too long.")]
        public string Text { get; set; }
    }

}
