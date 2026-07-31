using System.ComponentModel.DataAnnotations;

namespace Linkedin.Core.Dtos.Auth
{
    public class EmailRequestDto
    {
        [Required, EmailAddress, MaxLength(150)]
        public string Email { get; set; } = string.Empty;
    }

    public class ConfirmEmailDto : EmailRequestDto
    {
        [Required]
        public string Token { get; set; } = string.Empty;
    }

    public sealed class ResetPasswordDto : ConfirmEmailDto
    {
        [Required, MinLength(8), MaxLength(100)]
        public string NewPassword { get; set; } = string.Empty;

        [Required, Compare(nameof(NewPassword))]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public sealed class ChangeUnconfirmedEmailDto
    {
        [Required, EmailAddress, MaxLength(150)]
        public string CurrentEmail { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(150)]
        public string NewEmail { get; set; } = string.Empty;

        [Required, Compare(nameof(NewEmail))]
        public string ConfirmNewEmail { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public sealed class AccountEmailResult
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
        public string? Code { get; init; }
        public int? RetryAfterSeconds { get; init; }

        public static AccountEmailResult Ok(string message, string? code = null) =>
            new() { Success = true, Message = message, Code = code };

        public static AccountEmailResult Fail(string message, string? code = null) =>
            new() { Success = false, Message = message, Code = code };

        public static AccountEmailResult Cooldown(int seconds) =>
            new()
            {
                Success = false,
                Message = $"Please wait {seconds} seconds before requesting another email.",
                RetryAfterSeconds = seconds
            };
    }
}
