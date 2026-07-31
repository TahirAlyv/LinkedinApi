using System.ComponentModel.DataAnnotations;

namespace Linkedin.Core.Dtos.RegisterDtos
{
    public class BaseRegisterDto
    {
        [Required(ErrorMessage = "Username is required.")]
        [MinLength(3, ErrorMessage = "Username must be at least 3 characters.")]
        [MaxLength(30, ErrorMessage = "Username cannot exceed 30 characters.")]
        [RegularExpression(
            @"^(?![._])(?!.*[._]{2})[a-z0-9]+(?:[._][a-z0-9]+)*$",
            ErrorMessage = "Username can only contain lowercase letters, numbers, dots and underscores.")]
        public string Username { get; set; } = null!;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Email format is invalid.")]
        [MaxLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Please confirm your email.")]
        [EmailAddress(ErrorMessage = "Confirmation email format is invalid.")]
        [Compare(nameof(Email), ErrorMessage = "Email addresses do not match.")]
        public string ConfirmEmail { get; set; } = null!;

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
        public string Password { get; set; } = null!;
    }
}
