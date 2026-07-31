using Linkedin.Business.Services.Interface;
using Linkedin.Core.Common;
using Linkedin.Core.Dtos;
using Linkedin.Core.Dtos.Google;
using Linkedin.Core.Dtos.Auth;
using Linkedin.Core.Entities;
using Linkedin.Core.Enums;
using Linkedin.DataAccess.Repositories.Interfaces;
 
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.Threading.Tasks;
using Google.Apis.Auth;

namespace Linkedin.Business.Services.Concrete
{
    public class AuthService : IAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly IEmailCooldownService _emailCooldown;

        private const int RefreshTokenLifetimeDays = 7;
        private const int SessionLifetimeDays = 30;

        public AuthService(
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IUnitOfWork unitOfWork,
            IEmailService emailService,
            IEmailCooldownService emailCooldown)
        {
            _configuration = configuration;
            _userManager = userManager;
            _roleManager = roleManager;
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _emailCooldown = emailCooldown;
        }

        public async Task AssignRole(ApplicationUser user, string role)
        {

            var normalizedRole = role == "Employer" ? "Employer" : "JobSeeker";

            if (!await _roleManager.RoleExistsAsync(normalizedRole))
            {
                await _roleManager.CreateAsync(new ApplicationRole { Name = normalizedRole });
            }

            await _userManager.AddToRoleAsync(user, normalizedRole);

        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }


        public async Task<ServiceResult> SaveRefreshTokenAsync(
        ApplicationUser user,
        string refreshToken)
        {
            var now = DateTime.UtcNow;

            // Hər yeni login üçün ayrı session family yaranır.
            // Chrome login-i, Android login-i və iPhone login-i fərqli family olacaq.
            var tokenFamilyId = Guid.NewGuid().ToString("N");

            // Bu login session-u maksimum 30 gün yaşaya bilər.
            var sessionExpiresAt = now.AddDays(SessionLifetimeDays);

            var newRefresh = CreateRefreshToken(
                user,
                refreshToken,
                tokenFamilyId,
                sessionExpiresAt,
                now);

            await _unitOfWork.RefreshTokens.AddAsync(newRefresh);

            var check = await _unitOfWork.CompleteAsync();

            if (check <= 0)
            {
                return new ServiceResult(
                    message: "An error occurred while recording the refresh token.",
                    success: false,
                    data: null);
            }

            return new ServiceResult(
                message: "Refresh token created successfully.",
                success: true,
                data: null);
        }

        private RefreshToken CreateRefreshToken(
            ApplicationUser user,
            string refreshToken,
            string tokenFamilyId,
            DateTime sessionExpiresAt,
            DateTime now)
        {
            // Normalda tokenə 7 gün veririk.
            var tokenExpiresAt = now.AddDays(RefreshTokenLifetimeDays);

            // Amma 30 günlük session limiti keçilə bilməz.
            if (tokenExpiresAt > sessionExpiresAt)
            {
                tokenExpiresAt = sessionExpiresAt;
            }

            return new RefreshToken
            {
                Token = refreshToken,
                UserId = user.Id,
                TokenFamilyId = tokenFamilyId,
                ExpiresAt = tokenExpiresAt,
                SessionExpiresAt = sessionExpiresAt,
                IsRevoked = false
            };
        }

        public async Task<string> GenerateTokeen(ApplicationUser user)
        {
 
            var roles = await _userManager.GetRolesAsync(user);
 
            var role = roles.FirstOrDefault() ?? "JobSeeker";

 
            var claims = new List<Claim>
            {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, role)
            };

            var key = Encoding.ASCII.GetBytes(_configuration["AppSettings:Token"]);


            var tokenHandler = new JwtSecurityTokenHandler();

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(15),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };
 
            var token = tokenHandler.CreateToken(tokenDescriptor);
 
            return tokenHandler.WriteToken(token);
        }

        public async Task<ServiceResult> RefreshAccessTokenAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return new ServiceResult(
                    false,
                    "Refresh token is required.",
                    null);
            }

            var now = DateTime.UtcNow;

            var token = await _unitOfWork.RefreshTokens
                .GetByTokenAsync(refreshToken);

            if (token == null)
            {
                return new ServiceResult(
                    false,
                    "Refresh token not found. Please login again.",
                    null);
            }

            if (token.SessionExpiresAt <= now)
            {
                token.IsRevoked = true;
                await _unitOfWork.CompleteAsync();

                return new ServiceResult(
                    false,
                    "Your session has expired. Please login again.",
                    null);
            }
            if (token.ExpiresAt <= now)
            {
                token.IsRevoked = true;
                await _unitOfWork.CompleteAsync();

                return new ServiceResult(
                    false,
                    "Refresh token has expired. Please login again.",
                    null);
            }
            if (token.IsRevoked)
            {
                var activeTokensInSameFamily = await _unitOfWork.RefreshTokens
                    .GetActiveTokensByFamilyAsync(
                        token.UserId,
                        token.TokenFamilyId);
                foreach (var activeToken in activeTokensInSameFamily)
                {
                    activeToken.IsRevoked = true;
                }

                if (activeTokensInSameFamily.Any())
                {
                    await _unitOfWork.CompleteAsync();
                }

                return new ServiceResult(
                    false,
                    "This session was closed because an old refresh token was reused. Please login again.",
                    null);
            }

            var user = token.User;

            if (user == null)
            {
                token.IsRevoked = true;
                await _unitOfWork.CompleteAsync();

                return new ServiceResult(
                    false,
                    "User not found.",
                    null);
            }

            if (user.IsBlocked)
            {
                return new ServiceResult(
                    false,
                    "Your account has been blocked.",
                    null);
            }


            token.IsRevoked = true;

            var newAccessToken = await GenerateTokeen(user);
            var newRefreshTokenValue = GenerateRefreshToken();


            var newRefreshToken = CreateRefreshToken(
                user,
                newRefreshTokenValue,
                token.TokenFamilyId,
                token.SessionExpiresAt,
                now);

            await _unitOfWork.RefreshTokens.AddAsync(newRefreshToken);


            var check = await _unitOfWork.CompleteAsync();

            if (check <= 0)
            {
                return new ServiceResult(
                    false,
                    "New tokens could not be created.",
                    null);
            }

            return new ServiceResult(
                true,
                "New tokens created successfully.",
                new
                {
                    accessToken = newAccessToken,
                    refreshToken = newRefreshTokenValue
                });
        }

        public async Task<ServiceResult> GoogleLoginAsync(GoogleLoginDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.IdToken))
                return new ServiceResult(false, "Google token is required", null);

            GoogleJsonWebSignature.Payload payload;

            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(dto.IdToken);
            }
            catch
            {
                return new ServiceResult(false, "Invalid Google token", null);
            }

            if (!payload.EmailVerified)
                return new ServiceResult(false, "Google email is not verified", null);

            var user = await _userManager.FindByEmailAsync(payload.Email);

            if (user == null)
            {
                var generatedUsername = await GenerateUniqueUsernameAsync(payload.Name);
                var isEmployer = string.Equals(
                    dto.AccountType,
                    "company",
                    StringComparison.OrdinalIgnoreCase);

                user = new ApplicationUser
                {
                    UserName = generatedUsername,
                    Email = payload.Email,
                    FullName = payload.Name,
                    ProfileImage = payload.Picture,
                    EmailConfirmed = true,
                    UserType = isEmployer ? UserType.Employer : UserType.JobSeeker,
                    Company = isEmployer
                        ? new Company
                        {
                            Name = !string.IsNullOrWhiteSpace(dto.CompanyName)
                                ? dto.CompanyName.Trim()
                                : string.IsNullOrWhiteSpace(payload.Name)
                                    ? "Company"
                                    : payload.Name,
                            LogoUrl = payload.Picture
                        }
                        : null
                };

                if (user.Company != null)
                {
                    user.Company.UserId = user.Id;
                    user.Company.User = user;
                }

                var result = await _userManager.CreateAsync(user);

                if (!result.Succeeded)
                    return new ServiceResult(false, "User could not be created", result.Errors);

                await AssignRole(user, isEmployer ? "Employer" : "JobSeeker");
            }
            else if (string.Equals(user.UserName, user.Email, StringComparison.OrdinalIgnoreCase))
            {
                var generatedUsername = await GenerateUniqueUsernameAsync(payload.Name);
                var usernameResult = await _userManager.SetUserNameAsync(user, generatedUsername);

                if (!usernameResult.Succeeded)
                    return new ServiceResult(false, "A username could not be created for this Google account", usernameResult.Errors);
            }

            if (user.IsBlocked)
                return new ServiceResult(false, "Your account has been blocked.", null);

            var accessToken = await GenerateTokeen(user);
            var refreshToken = GenerateRefreshToken();

            await SaveRefreshTokenAsync(user, refreshToken);

            return new ServiceResult(true, "Google login successful", new
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
            });
        }

        public async Task SendTwoFactorCodeAsync(ApplicationUser user, string code)
        {
            var safeName = WebUtility.HtmlEncode(user.FullName ?? user.UserName ?? "there");
            var safeCode = WebUtility.HtmlEncode(code);
            await _emailService.SendAsync(
                user.Email!,
                user.FullName ?? user.UserName ?? user.Email!,
                "Your Nexora sign-in code",
                $"<p>Hello {safeName},</p><p>Use this code to complete your Nexora sign-in:</p><p style=\"font-size:28px;font-weight:700;letter-spacing:6px\">{safeCode}</p><p>This code expires shortly. Do not share it with anyone.</p>");
        }

        private async Task<string> GenerateUniqueUsernameAsync(string? fullName)
        {
            var baseUsername = (fullName ?? "user")
                .Trim()
                .ToLowerInvariant()
                .Replace("ə", "e")
                .Replace("ı", "i")
                .Replace("ö", "o")
                .Replace("ü", "u")
                .Replace("ş", "s")
                .Replace("ç", "c")
                .Replace("ğ", "g");

            baseUsername = Regex.Replace(baseUsername, @"\s+", ".");
            baseUsername = Regex.Replace(baseUsername, @"[^a-z0-9.]", string.Empty);
            baseUsername = Regex.Replace(baseUsername, @"\.{2,}", ".").Trim('.');

            if (string.IsNullOrWhiteSpace(baseUsername))
                baseUsername = "user";

            if (baseUsername.Length > 23)
                baseUsername = baseUsername[..23].TrimEnd('.');

            string candidate;
            do
            {
                var suffix = RandomNumberGenerator.GetInt32(100000, 1000000);
                candidate = $"{baseUsername}.{suffix}";
            }
            while (await _userManager.FindByNameAsync(candidate) != null);

            return candidate;
        }

        public async Task<AccountEmailResult> SendEmailConfirmationAsync(string email)
        {
            var normalizedEmail = email?.Trim();
            if (string.IsNullOrWhiteSpace(normalizedEmail))
                return AccountEmailResult.Fail("A valid email address is required.");

            if (!_emailCooldown.TryAcquire("confirm", normalizedEmail, out var retryAfter))
                return AccountEmailResult.Cooldown(retryAfter);

            var user = await _userManager.FindByEmailAsync(normalizedEmail);
            if (user == null || user.EmailConfirmed)
            {
                return AccountEmailResult.Ok(
                    "If this account requires verification, a confirmation email has been sent.");
            }

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var link = BuildFrontendLink("confirm-email", normalizedEmail, token);
            var displayName = string.IsNullOrWhiteSpace(user.FullName) ? user.UserName! : user.FullName;

            try
            {
                await _emailService.SendAsync(
                    normalizedEmail,
                    displayName,
                    "Confirm your Nexora email",
                    BuildEmailTemplate(
                        "Confirm your email",
                        "Verify your email address to activate your account and sign in. This link expires in 15 minutes.",
                        "Confirm email",
                        link,
                        "If you did not create this account, you can safely ignore this email."));
            }
            catch
            {
                _emailCooldown.Release("confirm", normalizedEmail);
                throw;
            }

            return AccountEmailResult.Ok(
                "A 15-minute confirmation link was sent. Please check your inbox.");
        }

        public async Task<AccountEmailResult> ConfirmEmailAsync(string email, string token)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
                return AccountEmailResult.Fail(
                    "The confirmation link is invalid or has expired.",
                    "LINK_INVALID_OR_EXPIRED");

            var user = await _userManager.FindByEmailAsync(email.Trim());
            if (user == null)
                return AccountEmailResult.Fail(
                    "The confirmation link is invalid or has expired.",
                    "LINK_INVALID_OR_EXPIRED");

            if (user.EmailConfirmed)
                return AccountEmailResult.Fail(
                    "This confirmation link has already been used. You can sign in.",
                    "LINK_ALREADY_USED");

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (!result.Succeeded)
            {
                return AccountEmailResult.Fail(
                    "The confirmation link is invalid or has expired.",
                    "LINK_INVALID_OR_EXPIRED");
            }

            // Invalidate this confirmation token immediately after successful use.
            await _userManager.UpdateSecurityStampAsync(user);

            return AccountEmailResult.Ok(
                "Email confirmed successfully. You can now sign in.",
                "EMAIL_CONFIRMED");
        }

        public async Task<AccountEmailResult> ChangeUnconfirmedEmailAsync(
            string currentEmail,
            string newEmail,
            string password)
        {
            var normalizedCurrentEmail = currentEmail?.Trim();
            var normalizedNewEmail = newEmail?.Trim();

            if (string.IsNullOrWhiteSpace(normalizedCurrentEmail) ||
                string.IsNullOrWhiteSpace(normalizedNewEmail) ||
                string.IsNullOrWhiteSpace(password))
            {
                return AccountEmailResult.Fail(
                    "Current email, new email and password are required.",
                    "CHANGE_EMAIL_FIELDS_REQUIRED");
            }

            // This anonymous endpoint verifies a password, so each account gets
            // only one attempt per cooldown window to reduce brute-force risk.
            if (!_emailCooldown.TryAcquire(
                "change-unconfirmed-email",
                normalizedCurrentEmail,
                out var retryAfter))
            {
                return AccountEmailResult.Cooldown(retryAfter);
            }

            var user = await _userManager.FindByEmailAsync(normalizedCurrentEmail);
            if (user == null || user.EmailConfirmed)
            {
                return AccountEmailResult.Fail(
                    "The email address could not be changed for this account.",
                    "EMAIL_CHANGE_NOT_ALLOWED");
            }

            if (!await _userManager.CheckPasswordAsync(user, password))
            {
                return AccountEmailResult.Fail(
                    "The password is incorrect.",
                    "INVALID_PASSWORD");
            }

            if (string.Equals(
                normalizedCurrentEmail,
                normalizedNewEmail,
                StringComparison.OrdinalIgnoreCase))
            {
                return AccountEmailResult.Fail(
                    "Enter a different email address.",
                    "EMAIL_UNCHANGED");
            }

            var emailOwner = await _userManager.FindByEmailAsync(normalizedNewEmail);
            if (emailOwner != null && emailOwner.Id != user.Id)
            {
                return AccountEmailResult.Fail(
                    "An account with this email already exists.",
                    "EMAIL_ALREADY_IN_USE");
            }

            var setEmailResult = await _userManager.SetEmailAsync(user, normalizedNewEmail);
            if (!setEmailResult.Succeeded)
            {
                return AccountEmailResult.Fail(
                    setEmailResult.Errors.FirstOrDefault()?.Description ??
                    "The email address could not be changed.",
                    "EMAIL_CHANGE_FAILED");
            }

            user.EmailConfirmed = false;
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return AccountEmailResult.Fail(
                    updateResult.Errors.FirstOrDefault()?.Description ??
                    "The email address could not be changed.",
                    "EMAIL_CHANGE_FAILED");
            }

            // Rotating the stamp invalidates every confirmation token created
            // for the previous email address before a token for the new one is issued.
            await _userManager.UpdateSecurityStampAsync(user);

            try
            {
                var sendResult = await SendEmailConfirmationAsync(normalizedNewEmail);
                if (!sendResult.Success)
                {
                    return AccountEmailResult.Ok(
                        "Email changed. Please request a new confirmation link.",
                        "EMAIL_CHANGED_LINK_NOT_SENT");
                }
            }
            catch
            {
                return AccountEmailResult.Ok(
                    "Email changed, but the confirmation message could not be sent. Please resend it.",
                    "EMAIL_CHANGED_LINK_NOT_SENT");
            }

            return AccountEmailResult.Ok(
                "Email changed. A new 15-minute confirmation link was sent.",
                "EMAIL_CHANGED");
        }

        public async Task<AccountEmailResult> SendPasswordResetAsync(string email)
        {
            var normalizedEmail = email?.Trim();
            if (string.IsNullOrWhiteSpace(normalizedEmail))
                return AccountEmailResult.Fail("A valid email address is required.");

            if (!_emailCooldown.TryAcquire("password-reset", normalizedEmail, out var retryAfter))
                return AccountEmailResult.Cooldown(retryAfter);

            var user = await _userManager.FindByEmailAsync(normalizedEmail);
            if (user == null || !user.EmailConfirmed)
            {
                return AccountEmailResult.Ok(
                    "If an eligible account exists, a password reset email has been sent.");
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var link = BuildFrontendLink("reset-password", normalizedEmail, token);
            var displayName = string.IsNullOrWhiteSpace(user.FullName) ? user.UserName! : user.FullName;

            try
            {
                await _emailService.SendAsync(
                    normalizedEmail,
                    displayName,
                    "Reset your Nexora password",
                    BuildEmailTemplate(
                        "Reset your password",
                        "We received a request to reset your password. This link expires in 10 minutes.",
                        "Reset password",
                        link,
                        "If you did not request this change, ignore this email. Your password will remain unchanged."));
            }
            catch
            {
                _emailCooldown.Release("password-reset", normalizedEmail);
                throw;
            }

            return AccountEmailResult.Ok(
                "If an eligible account exists, a 10-minute password reset link has been sent.");
        }

        public async Task<AccountEmailResult> ValidatePasswordResetTokenAsync(
            string email,
            string token)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
            {
                return AccountEmailResult.Fail(
                    "The password reset link is invalid, expired, or has already been used.",
                    "LINK_INVALID_OR_USED");
            }

            var user = await _userManager.FindByEmailAsync(email.Trim());
            if (user == null || !user.EmailConfirmed)
            {
                return AccountEmailResult.Fail(
                    "The password reset link is invalid, expired, or has already been used.",
                    "LINK_INVALID_OR_USED");
            }

            var isValid = await _userManager.VerifyUserTokenAsync(
                user,
                _userManager.Options.Tokens.PasswordResetTokenProvider,
                UserManager<ApplicationUser>.ResetPasswordTokenPurpose,
                token);

            return isValid
                ? AccountEmailResult.Ok("The password reset link is valid.", "LINK_VALID")
                : AccountEmailResult.Fail(
                    "The password reset link is invalid, expired, or has already been used.",
                    "LINK_INVALID_OR_USED");
        }

        public async Task<AccountEmailResult> ResetPasswordAsync(
            string email,
            string token,
            string newPassword)
        {
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(token) ||
                string.IsNullOrWhiteSpace(newPassword))
            {
                return AccountEmailResult.Fail(
                    "The password reset link is invalid, expired, or has already been used.",
                    "LINK_INVALID_OR_USED");
            }

            var user = await _userManager.FindByEmailAsync(email.Trim());
            if (user == null || !user.EmailConfirmed)
                return AccountEmailResult.Fail(
                    "The password reset link is invalid, expired, or has already been used.",
                    "LINK_INVALID_OR_USED");

            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            if (!result.Succeeded)
            {
                var identityMessage = result.Errors.FirstOrDefault()?.Description;
                var invalidToken = result.Errors.Any(error => error.Code == "InvalidToken");

                return AccountEmailResult.Fail(
                    invalidToken
                        ? "The password reset link is invalid, expired, or has already been used."
                        : identityMessage ?? "The password could not be reset.",
                    invalidToken ? "LINK_INVALID_OR_USED" : "PASSWORD_VALIDATION_FAILED");
            }

            // ResetPasswordAsync normally rotates the stamp. Rotating it explicitly
            // guarantees that the successful link cannot be submitted again.
            await _userManager.UpdateSecurityStampAsync(user);

            return AccountEmailResult.Ok(
                "Your password has been reset successfully. This link can no longer be used.",
                "PASSWORD_RESET");
        }

        private string BuildFrontendLink(string path, string email, string token)
        {
            var baseUrl = (_configuration["Frontend:BaseUrl"] ?? "http://localhost:5173")
                .TrimEnd('/');

            return $"{baseUrl}/{path}?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
        }

        private static string BuildEmailTemplate(
            string title,
            string description,
            string buttonText,
            string buttonUrl,
            string footer)
        {
            var safeTitle = WebUtility.HtmlEncode(title);
            var safeDescription = WebUtility.HtmlEncode(description);
            var safeButtonText = WebUtility.HtmlEncode(buttonText);
            var safeButtonUrl = WebUtility.HtmlEncode(buttonUrl);
            var safeFooter = WebUtility.HtmlEncode(footer);

            return $"""
                <!doctype html>
                <html lang="en">
                <body style="margin:0;background:#f5f7fb;font-family:Arial,sans-serif;color:#172033">
                  <div style="max-width:560px;margin:32px auto;padding:0 16px">
                    <div style="background:#ffffff;border:1px solid #e2e8f0;border-radius:18px;padding:36px;box-shadow:0 10px 30px rgba(15,23,42,.06)">
                      <div style="font-size:25px;font-weight:800;color:#4f46e5;margin-bottom:24px">nexora.</div>
                      <h1 style="font-size:24px;margin:0 0 12px">{safeTitle}</h1>
                      <p style="font-size:15px;line-height:1.65;color:#64748b;margin:0 0 26px">{safeDescription}</p>
                      <a href="{safeButtonUrl}" style="display:inline-block;padding:12px 22px;border-radius:999px;background:#4f46e5;color:#ffffff;text-decoration:none;font-weight:700">{safeButtonText}</a>
                      <p style="font-size:12px;line-height:1.6;color:#94a3b8;margin:28px 0 0">{safeFooter}</p>
                    </div>
                  </div>
                </body>
                </html>
                """;
        }

    }
}
