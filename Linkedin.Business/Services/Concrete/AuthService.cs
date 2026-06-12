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


        public async Task<ServiceResult> SaveRefreshTokenAsync(ApplicationUser user, string refreshToken)
        {
            var newRefresh = new RefreshToken
            {
                Token = refreshToken,
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false
            };

           await _unitOfWork.RefreshTokens.AddAsync(newRefresh);
           var check= await _unitOfWork.CompleteAsync();

            if (check != 1)
            {
                return new ServiceResult(message: "An error occurred while recording!", success: false, data: null);
            }
            return new ServiceResult(message: "Successfully registered token!", success: true, data: null);
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
            var token = await _unitOfWork.RefreshTokens.GetByTokenAsync(refreshToken);

            if (token == null)
                return new ServiceResult(false, "Token not found!", null);

            if (token.IsRevoked)
                return new ServiceResult(false, "Token iptal edilmiş", null);

            if (token.ExpiresAt < DateTime.UtcNow)
                return new ServiceResult(false, "Token süresi dolmuş", null);

            token.IsRevoked = true;

            var user = token.User;

            if (user == null)
                return new ServiceResult(false, "User not found!", null);

            if (user.IsBlocked)
                return new ServiceResult(false, "Your account has been blocked.", null);
            var newAccessToken = await GenerateTokeen(user);
            var newRefreshToken = GenerateRefreshToken();

            await SaveRefreshTokenAsync(user, newRefreshToken);

            return new ServiceResult(true, "Yeni tokenlar üretildi", new
            {
                accessToken = newAccessToken,
                refreshToken = newRefreshToken
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
