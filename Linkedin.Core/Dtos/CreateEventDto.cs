using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Linkedin.Core.Dtos
{
    public class CreateEventDto
    {
        [Required, StringLength(120)]
        public string Title { get; set; } = string.Empty;
        [StringLength(1000)]
        public string? Description { get; set; }
        [StringLength(300)]
        public string? Topics { get; set; }
        [StringLength(180)]
        public string? Location { get; set; }
        public DateTime StartsAt { get; set; }
        public IFormFile? Image { get; set; }
    }
}
