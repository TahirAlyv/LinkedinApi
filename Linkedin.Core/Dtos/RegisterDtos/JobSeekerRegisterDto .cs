using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Dtos.RegisterDtos
{
    public class JobSeekerRegisterDto : BaseRegisterDto
    {
        [Required(ErrorMessage = "Full name is required.")]
        [MaxLength(100, ErrorMessage = "Full name cannot exceed 100 characters.")]
        public string FullName { get; set; } = null!;

        [MaxLength(1000, ErrorMessage = "Bio cannot exceed 1000 characters.")]
        public string? Bio { get; set; }

        [MaxLength(150, ErrorMessage = "Location cannot exceed 150 characters.")]
        public string? Location { get; set; }
    }
}
