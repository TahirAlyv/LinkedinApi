using Google.Apis.Auth;
using Linkedin.Business.Services.Interface;
using Linkedin.Core.Dtos;
using Linkedin.Core.Dtos.Google;
using Linkedin.Core.Dtos.Auth;
using Linkedin.Core.Dtos.RegisterDtos;
using Linkedin.Core.Entities;
using Linkedin.Core.Enums;
using LinkedIn.Core.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;

namespace Linkedin.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthService authService,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        [HttpPost("jobseekers/register")]
        public async Task<ActionResult> RegisterJobSeeker([FromBody] JobSeekerRegisterDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var username = dto.Username.Trim().ToLowerInvariant();

            var existingUserByUsername = await _userManager.FindByNameAsync(username);
            if (existingUserByUsername != null)
                return BadRequest("This username is already taken.");

            var existingUserByEmail = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUserByEmail != null)
                return BadRequest("An account with this email already exists.");

            var user = new ApplicationUser
            {
                UserName = username,
                Email = dto.Email,
                FullName = dto.FullName,
                Bio = dto.Bio,
                Location = dto.Location,
                UserType = UserType.JobSeeker
            };

            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            await _authService.AssignRole(user, "JobSeeker");

            var emailSent = await TrySendConfirmationEmailAsync(user.Email!);

            return Ok(new
            {
                message = emailSent
                    ? "Registration successful. Please confirm your email."
                    : "Registration successful. Use resend verification to request a new email.",
                email = user.Email,
                emailSent
            });
        }

        [HttpPost("employers/register")]
        public async Task<ActionResult> RegisterEmployer([FromBody] EmployerRegisterDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var username = dto.Username.Trim().ToLowerInvariant();

            var existingUserByUsername = await _userManager.FindByNameAsync(username);
            if (existingUserByUsername != null)
                return BadRequest("This username is already taken.");

            var existingUserByEmail = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUserByEmail != null)
                return BadRequest("An account with this email already exists.");

            var user = new ApplicationUser
            {
                UserName = username,
                Email = dto.Email,
                FullName = dto.Name,
                Bio = dto.Bio,
                Website = dto.Website,
                Location = dto.Location,
                UserType = UserType.Employer,

                Company = new Company
                {
                    Name = dto.Name,
                    Industry = dto.Industry,
                    Bio = dto.Bio,
                    Website = dto.Website,
                    Location = dto.Location,
                    IsVerified = false
                }
            };

            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            await _authService.AssignRole(user, "Employer");

            var emailSent = await TrySendConfirmationEmailAsync(user.Email!);

            return Ok(new
            {
                message = emailSent
                    ? "Registration successful. Please confirm your email."
                    : "Registration successful. Use resend verification to request a new email.",
                email = user.Email,
                emailSent
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var identifier = dto?.Username?.Trim();
            if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(dto.Password))
            {
                return BadRequest(new
                {
                    code = "LOGIN_FIELDS_REQUIRED",
                    message = "Username or email and password are required."
                });
            }

            var user = identifier.Contains('@')
                ? await _userManager.FindByEmailAsync(identifier)
                : await _userManager.FindByNameAsync(identifier.ToLowerInvariant());

            if (user == null)
            {
                return Unauthorized(new
                {
                    code = "INVALID_CREDENTIALS",
                    message = "The username/email or password is incorrect."
                });
            }

            if (user.IsBlocked)
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    code = "ACCOUNT_RESTRICTED",
                    message = "Your account has been restricted.",
                    reason = string.IsNullOrWhiteSpace(user.BlockReason)
                        ? "No additional reason was provided."
                        : user.BlockReason
                });

            var passwordValid = await _userManager.CheckPasswordAsync(user, dto.Password!);

            if (!passwordValid)
            {
                return Unauthorized(new
                {
                    code = "INVALID_CREDENTIALS",
                    message = "The username/email or password is incorrect."
                });
            }

            if (await IsStaffAsync(user))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    code = "STAFF_PORTAL_REQUIRED",
                    message = "Staff accounts must sign in through the Admin Portal."
                });
            }

            if (!user.EmailConfirmed)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    code = "EMAIL_NOT_CONFIRMED",
                    message = "Please confirm your email before signing in.",
                    email = user.Email
                });
            }

            if (user.TwoFactorEnabled)
            {
                var code = await _userManager.GenerateTwoFactorTokenAsync(
                    user,
                    TokenOptions.DefaultEmailProvider);
                await _authService.SendTwoFactorCodeAsync(user, code);

                return Ok(new
                {
                    requiresTwoFactor = true,
                    email = user.Email,
                    message = "We sent a verification code to your email."
                });
            }

            return Ok(await BuildLoginResponseAsync(user));
        }

        [HttpPost("staff-login")]
        [EnableRateLimiting("StaffLogin")]
        public async Task<IActionResult> StaffLogin([FromBody] StaffLoginDto dto)
        {
            var identifier = dto.Identifier.Trim();
            var user = await FindUserAsync(identifier);

            if (user == null || !await IsStaffAsync(user))
            {
                return Unauthorized(new
                {
                    code = "INVALID_STAFF_CREDENTIALS",
                    message = "The email/username or password is incorrect."
                });
            }

            if (user.IsBlocked)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    code = "STAFF_ACCOUNT_BLOCKED",
                    message = "This staff account has been disabled."
                });
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                return StatusCode(StatusCodes.Status423Locked, new
                {
                    code = "STAFF_ACCOUNT_LOCKED",
                    message = "Too many failed attempts. Try again in 15 minutes."
                });
            }

            if (!await _userManager.CheckPasswordAsync(user, dto.Password))
            {
                await _userManager.AccessFailedAsync(user);

                return Unauthorized(new
                {
                    code = "INVALID_STAFF_CREDENTIALS",
                    message = "The email/username or password is incorrect."
                });
            }

            await _userManager.ResetAccessFailedCountAsync(user);

            if (!user.EmailConfirmed)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    code = "STAFF_EMAIL_NOT_CONFIRMED",
                    message = "The staff email address must be confirmed before sign-in."
                });
            }

            var code = await _userManager.GenerateTwoFactorTokenAsync(
                user,
                TokenOptions.DefaultEmailProvider);
            await _authService.SendTwoFactorCodeAsync(user, code);

            return Ok(new
            {
                requiresTwoFactor = true,
                email = MaskEmail(user.Email),
                message = "A verification code was sent to the staff email address."
            });
        }

        [HttpPost("staff-verify-two-factor")]
        [EnableRateLimiting("StaffLogin")]
        public async Task<IActionResult> VerifyStaffTwoFactor(
            [FromBody] StaffTwoFactorLoginDto dto)
        {
            var user = await FindUserAsync(dto.Identifier.Trim());

            if (user == null ||
                !await IsStaffAsync(user) ||
                user.IsBlocked ||
                await _userManager.IsLockedOutAsync(user) ||
                !await _userManager.CheckPasswordAsync(user, dto.Password))
            {
                return Unauthorized(new
                {
                    code = "INVALID_STAFF_VERIFICATION",
                    message = "This staff sign-in verification is no longer valid."
                });
            }

            var valid = await _userManager.VerifyTwoFactorTokenAsync(
                user,
                TokenOptions.DefaultEmailProvider,
                dto.Code.Trim());

            if (!valid)
            {
                return Unauthorized(new
                {
                    code = "INVALID_STAFF_VERIFICATION",
                    message = "The verification code is invalid or has expired."
                });
            }

            return Ok(await BuildLoginResponseAsync(user));
        }

        [HttpPost("verify-two-factor")]
        public async Task<IActionResult> VerifyTwoFactor([FromBody] TwoFactorLoginDto dto)
        {
            var identifier = dto.Username.Trim();
            var user = identifier.Contains('@')
                ? await _userManager.FindByEmailAsync(identifier)
                : await _userManager.FindByNameAsync(identifier.ToLowerInvariant());

            if (user == null || !user.TwoFactorEnabled)
                return Unauthorized(new { message = "This sign-in verification is no longer valid." });

            if (await IsStaffAsync(user))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    code = "STAFF_PORTAL_REQUIRED",
                    message = "Staff accounts must sign in through the Admin Portal."
                });
            }

            var valid = await _userManager.VerifyTwoFactorTokenAsync(
                user,
                TokenOptions.DefaultEmailProvider,
                dto.Code.Trim());
            if (!valid)
                return Unauthorized(new { message = "The verification code is invalid or has expired." });

            return Ok(await BuildLoginResponseAsync(user));
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.RefreshToken))
                return BadRequest("Refresh token is required");

            var result = await _authService.RefreshAccessTokenAsync(dto.RefreshToken);

            if (!result.Success)
                return Unauthorized(result.Message);

            return Ok(result.Data);
        }



        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDto dto)
        {
            var result = await _authService.GoogleLoginAsync(dto);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("resend-confirmation")]
        public async Task<IActionResult> ResendConfirmation([FromBody] EmailRequestDto dto)
        {
            var result = await ExecuteEmailOperationAsync(
                () => _authService.SendEmailConfirmationAsync(dto.Email));

            return ToEmailActionResult(result);
        }

        [HttpPost("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailDto dto)
        {
            var result = await _authService.ConfirmEmailAsync(dto.Email, dto.Token);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("change-unconfirmed-email")]
        public async Task<IActionResult> ChangeUnconfirmedEmail(
            [FromBody] ChangeUnconfirmedEmailDto dto)
        {
            var result = await ExecuteEmailOperationAsync(
                () => _authService.ChangeUnconfirmedEmailAsync(
                    dto.CurrentEmail,
                    dto.NewEmail,
                    dto.Password));

            return ToEmailActionResult(result);
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] EmailRequestDto dto)
        {
            var result = await ExecuteEmailOperationAsync(
                () => _authService.SendPasswordResetAsync(dto.Email));

            return ToEmailActionResult(result);
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var result = await _authService.ResetPasswordAsync(
                dto.Email,
                dto.Token,
                dto.NewPassword);

            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("validate-password-reset-token")]
        public async Task<IActionResult> ValidatePasswordResetToken(
            [FromBody] ConfirmEmailDto dto)
        {
            var result = await _authService.ValidatePasswordResetTokenAsync(
                dto.Email,
                dto.Token);

            return result.Success ? Ok(result) : BadRequest(result);
        }

        private async Task<bool> TrySendConfirmationEmailAsync(string email)
        {
            try
            {
                var result = await _authService.SendEmailConfirmationAsync(email);
                return result.Success;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Confirmation email could not be sent to {Email}.",
                    email);
                return false;
            }
        }

        private async Task<object> BuildLoginResponseAsync(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.Contains("Admin")
                ? "Admin"
                : roles.Contains("Moderator")
                    ? "Moderator"
                    : roles.FirstOrDefault() ?? user.UserType.ToString();
            var portal = role is "Admin" or "Moderator" ? "staff" : "platform";
            var accessToken = await _authService.GenerateTokeen(user);
            var refreshToken = _authService.GenerateRefreshToken();
            await _authService.SaveRefreshTokenAsync(user, refreshToken);

            return new
            {
                accessToken,
                refreshToken,
                user = new
                {
                    user.Id,
                    user.FullName,
                    user.Email,
                    user.UserName,
                    user.ProfileImage,
                    user.UserType,
                    role,
                    portal
                },
                portal
            };
        }

        private async Task<ApplicationUser?> FindUserAsync(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                return null;

            return identifier.Contains('@')
                ? await _userManager.FindByEmailAsync(identifier)
                : await _userManager.FindByNameAsync(identifier.ToLowerInvariant());
        }

        private async Task<bool> IsStaffAsync(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            return roles.Any(role => role is "Admin" or "Moderator");
        }

        private static string MaskEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
                return "your email";

            var parts = email.Split('@', 2);
            var localPart = parts[0];
            var visible = localPart.Length <= 2
                ? localPart[..1]
                : localPart[..2];

            return $"{visible}{new string('*', Math.Max(2, localPart.Length - visible.Length))}@{parts[1]}";
        }

        private async Task<AccountEmailResult> ExecuteEmailOperationAsync(
     Func<Task<AccountEmailResult>> operation)
        {
            try
            {
                return await operation();
            }
            catch (Exception exception)
            {
                Exception mainException = exception.GetBaseException();

                _logger.LogError(
                    exception,
                    """
            Transactional email operation failed.
            ExceptionType: {ExceptionType}
            ExceptionMessage: {ExceptionMessage}
            BaseExceptionType: {BaseExceptionType}
            BaseExceptionMessage: {BaseExceptionMessage}
            """,
                    exception.GetType().FullName,
                    exception.Message,
                    mainException.GetType().FullName,
                    mainException.Message
                );

                return AccountEmailResult.Fail(
                    "The email could not be sent right now. Please try again later.");
            }
        }

        private IActionResult ToEmailActionResult(AccountEmailResult result)
        {
            if (result.RetryAfterSeconds.HasValue)
            {
                Response.Headers["Retry-After"] = result.RetryAfterSeconds.Value.ToString();
                return StatusCode(StatusCodes.Status429TooManyRequests, result);
            }

            return result.Success ? Ok(result) : BadRequest(result);
        }

    }
}
