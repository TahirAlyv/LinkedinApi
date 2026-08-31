using Linkedin.Business.Services.Interface;
using Linkedin.Core.Entities;
using Linkedin.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Security.Cryptography;

namespace Linkedin.Api.Controllers
{
    [ApiController]
    [Route("api/Admin/staff")]
    [Authorize(Roles = "Admin")]
    public class StaffController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public StaffController(UserManager<ApplicationUser> userManager, IEmailService emailService, IConfiguration configuration)
        {
            _userManager = userManager;
            _emailService = emailService;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> GetModerators()
        {
            var moderators = await _userManager.GetUsersInRoleAsync("Moderator");
            return Ok(moderators.OrderByDescending(user => user.CreatedAt).Select(user => new
            {
                id = user.Id, fullName = user.FullName, username = user.UserName,
                email = user.Email, profileImage = user.ProfileImage,
                createdAt = user.CreatedAt, isDisabled = user.IsBlocked,
                disableReason = user.BlockReason, twoFactorEnabled = user.TwoFactorEnabled,
                emailConfirmed = user.EmailConfirmed, lockoutEnd = user.LockoutEnd
            }));
        }

        [HttpPost("invite")]
        public async Task<IActionResult> InviteModerator([FromBody] InviteModeratorDto dto)
        {
            var email = dto.Email?.Trim();
            var username = dto.UserName?.Trim().ToLowerInvariant();
            var fullName = dto.FullName?.Trim();
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(fullName))
                return BadRequest(new { message = "Full name, username and email are required." });
            if (await _userManager.FindByEmailAsync(email) != null)
                return Conflict(new { message = "An account with this email already exists." });
            if (await _userManager.FindByNameAsync(username) != null)
                return Conflict(new { message = "This username is already in use." });

            var moderator = new ApplicationUser
            {
                FullName = fullName, UserName = username, Email = email,
                EmailConfirmed = true, UserType = UserType.Staff,
                TwoFactorEnabled = true, LockoutEnabled = true,
                CreatedAt = DateTime.UtcNow
            };
            var temporaryPassword = $"Nx!{Convert.ToHexString(RandomNumberGenerator.GetBytes(18))}a7";
            var createResult = await _userManager.CreateAsync(moderator, temporaryPassword);
            if (!createResult.Succeeded)
                return BadRequest(new { message = "Moderator could not be created.", errors = createResult.Errors.Select(error => error.Description) });

            var roleResult = await _userManager.AddToRoleAsync(moderator, "Moderator");
            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(moderator);
                return BadRequest(new { message = "Moderator role could not be assigned." });
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(moderator);
            var frontend = (_configuration["Frontend:BaseUrl"] ?? "https://lynq-app-two.vercel.app").TrimEnd('/');
            var link = $"{frontend}/reset-password?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
            try
            {
                await _emailService.SendAsync(email, fullName, "You were invited as a Nexora Moderator",
                    $"<div style='font-family:Arial,sans-serif;line-height:1.6;color:#172033'><h2>Nexora Moderator invitation</h2><p>Hello {WebUtility.HtmlEncode(fullName)},</p><p>An administrator invited you to the Nexora staff workspace.</p><p><a style='display:inline-block;padding:12px 18px;border-radius:8px;background:#3563e9;color:white;text-decoration:none' href='{WebUtility.HtmlEncode(link)}'>Create your password</a></p><p>This secure link expires in 10 minutes. After creating your password, sign in at <strong>/admin/login</strong>; a 2FA code will be sent to this email.</p></div>");
            }
            catch
            {
                await _userManager.DeleteAsync(moderator);
                return StatusCode(503, new { message = "Invitation email could not be sent, so the moderator account was not kept." });
            }

            return Ok(new { message = "Moderator invitation sent.", moderatorId = moderator.Id });
        }

        [HttpPost("{id}/disable")]
        public async Task<IActionResult> Disable(string id, [FromBody] StaffReasonDto dto)
        {
            var moderator = await FindModeratorAsync(id);
            if (moderator == null) return NotFound(new { message = "Moderator not found." });
            var reason = dto.Reason?.Trim();
            if (string.IsNullOrWhiteSpace(reason)) return BadRequest(new { message = "A disable reason is required." });
            moderator.IsBlocked = true; moderator.BlockReason = reason;
            await _userManager.UpdateSecurityStampAsync(moderator);
            await _userManager.UpdateAsync(moderator);
            return Ok(new { message = "Moderator disabled." });
        }

        [HttpPost("{id}/enable")]
        public async Task<IActionResult> Enable(string id)
        {
            var moderator = await FindModeratorAsync(id);
            if (moderator == null) return NotFound(new { message = "Moderator not found." });
            moderator.IsBlocked = false; moderator.BlockReason = null;
            await _userManager.SetLockoutEndDateAsync(moderator, null);
            await _userManager.ResetAccessFailedCountAsync(moderator);
            await _userManager.UpdateAsync(moderator);
            return Ok(new { message = "Moderator enabled." });
        }

        private async Task<ApplicationUser?> FindModeratorAsync(string id)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(item => item.Id == id && item.UserType == UserType.Staff);
            return user != null && await _userManager.IsInRoleAsync(user, "Moderator") ? user : null;
        }
    }

    public class InviteModeratorDto { public string FullName { get; set; } = ""; public string UserName { get; set; } = ""; public string Email { get; set; } = ""; }
    public class StaffReasonDto { public string? Reason { get; set; } }
}
