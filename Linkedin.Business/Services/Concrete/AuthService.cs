using Linkedin.Business.Services.Interface;
using Linkedin.Core.Common;
using Linkedin.Core.Dtos;
using Linkedin.Core.Dtos.Google;
using Linkedin.Core.Entities;
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

        private const int RefreshTokenLifetimeDays = 7;
        private const int SessionLifetimeDays = 30;

        public AuthService(IConfiguration configuration, UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager,IUnitOfWork unitOfWork)
        {
            _configuration = configuration;
            _userManager = userManager;
            _roleManager = roleManager;
            _unitOfWork = unitOfWork;
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
                user = new ApplicationUser
                {
                    UserName = payload.Email,
                    Email = payload.Email,
                    FullName = payload.Name,
                    ProfileImage = payload.Picture,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user);

                if (!result.Succeeded)
                    return new ServiceResult(false, "User could not be created", result.Errors);

                await AssignRole(user, "JobSeeker");
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
                    user.ProfileImage
                }
            });
        }

    }
}
