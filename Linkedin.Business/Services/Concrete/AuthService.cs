using Linkedin.Business.Services.Interface;
using Linkedin.Core.Common;
using Linkedin.Core.Dtos;
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

    }
}
