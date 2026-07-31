using System.ComponentModel.DataAnnotations;

namespace Linkedin.Core.Dtos.Ai
{
    public class ImproveTextRequestDto
    {
        [Required(ErrorMessage = "Text is required.")]
        [StringLength(1000, MinimumLength = 2,
            ErrorMessage = "Text must be between 2 and 1000 characters.")]
        public string Text { get; set; } = string.Empty;
    }
}