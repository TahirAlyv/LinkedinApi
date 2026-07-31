using Linkedin.Core.Common;
using Linkedin.Core.Dtos;
using Linkedin.Core.Dtos.Google;
using Linkedin.Core.Dtos.Auth;
using Linkedin.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Business.Services.Interface
{
    public interface IAuthService
    {
        Task<string> GenerateTokeen(ApplicationUser user);
        Task AssignRole(ApplicationUser user, string role);
        Task<ServiceResult> SaveRefreshTokenAsync(ApplicationUser user, string refreshToken);
        Task<ServiceResult> RefreshAccessTokenAsync(string refreshToken);
        string GenerateRefreshToken();
        Task<ServiceResult> GoogleLoginAsync(GoogleLoginDto dto);
        Task<AccountEmailResult> SendEmailConfirmationAsync(string email);
        Task<AccountEmailResult> ConfirmEmailAsync(string email, string token);
        Task<AccountEmailResult> ChangeUnconfirmedEmailAsync(
            string currentEmail,
            string newEmail,
            string password);
        Task<AccountEmailResult> SendPasswordResetAsync(string email);
        Task<AccountEmailResult> ValidatePasswordResetTokenAsync(string email, string token);
        Task<AccountEmailResult> ResetPasswordAsync(string email, string token, string newPassword);
        Task SendTwoFactorCodeAsync(ApplicationUser user, string code);
    }
}
