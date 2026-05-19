using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Dtos.RegisterDtos
{
    public class EmployerRegisterDto : BaseRegisterDto
    {
        [Required(ErrorMessage = "Company name is required.")]
        [MaxLength(150, ErrorMessage = "Company name cannot exceed 150 characters.")]
        public string Name { get; set; } = null!;

        [MaxLength(100, ErrorMessage = "Industry cannot exceed 100 characters.")]
        public string? Industry { get; set; }

        [MaxLength(1000, ErrorMessage = "Bio cannot exceed 1000 characters.")]
        public string? Bio { get; set; }

        [MaxLength(300, ErrorMessage = "Website cannot exceed 300 characters.")]
        public string? Website { get; set; }

        [MaxLength(150, ErrorMessage = "Location cannot exceed 150 characters.")]
        public string? Location { get; set; }
    }
}
