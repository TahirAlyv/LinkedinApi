using System.ComponentModel.DataAnnotations;

namespace Linkedin.Core.Dtos.Auth
{
    public sealed class SetTwoFactorDto
    {
        public bool Enabled { get; set; }

        [Required]
        public string CurrentPassword { get; set; } = string.Empty;
    }

    public sealed class TwoFactorLoginDto
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required, StringLength(20, MinimumLength = 4)]
        public string Code { get; set; } = string.Empty;
    }

    public sealed class StaffLoginDto
    {
        [Required]
        public string Identifier { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public sealed class StaffTwoFactorLoginDto
    {
        [Required]
        public string Identifier { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [Required, StringLength(20, MinimumLength = 4)]
        public string Code { get; set; } = string.Empty;
    }
}
