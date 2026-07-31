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
                return StatusCode(403, new { message = "Your account has been blocked." });

            var passwordValid = await _userManager.CheckPasswordAsync(user, dto.Password!);

            if (!passwordValid)
            {
                return Unauthorized(new
                {
                    code = "INVALID_CREDENTIALS",
                    message = "The username/email or password is incorrect."
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

        [HttpPost("verify-two-factor")]
        public async Task<IActionResult> VerifyTwoFactor([FromBody] TwoFactorLoginDto dto)
        {
            var identifier = dto.Username.Trim();
            var user = identifier.Contains('@')
                ? await _userManager.FindByEmailAsync(identifier)
                : await _userManager.FindByNameAsync(identifier.ToLowerInvariant());

            if (user == null || !user.TwoFactorEnabled)
                return Unauthorized(new { message = "This sign-in verification is no longer valid." });

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
                    user.UserType
                }
            };
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
                _logger.LogError(exception, "Transactional email operation failed.");
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
