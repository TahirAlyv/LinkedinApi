using Linkedin.Business.Services.Interface;
using Linkedin.Core.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Linkedin.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CompanyFollowController : ControllerBase
    {
        private readonly ICompanyFollowService _companyFollowService;

        public CompanyFollowController(ICompanyFollowService companyFollowService)
        {
            _companyFollowService = companyFollowService;
        }

        private string? GetCurrentUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        [HttpPost("follow/{employerUsername}")]
        public async Task<IActionResult> FollowCompany(string employerUsername)
        {
            var currentUserId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(currentUserId))
                return Unauthorized(ServiceResult.Failure("User not found."));

            var result = await _companyFollowService.FollowCompanyAsync(
                currentUserId,
                employerUsername
            );

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpDelete("unfollow/{employerUsername}")]
        public async Task<IActionResult> UnfollowCompany(string employerUsername)
        {
            var currentUserId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(currentUserId))
                return Unauthorized(ServiceResult.Failure("User not found."));

            var result = await _companyFollowService.UnfollowCompanyAsync(
                currentUserId,
                employerUsername
            );

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("status/{employerUsername}")]
        public async Task<IActionResult> GetFollowStatus(string employerUsername)
        {
            var currentUserId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(currentUserId))
                return Unauthorized(ServiceResult.Failure("User not found."));

            var result = await _companyFollowService.GetFollowStatusAsync(
                currentUserId,
                employerUsername
            );

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("my-followed-companies")]
        public async Task<IActionResult> GetMyFollowedCompanies()
        {
            var currentUserId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(currentUserId))
                return Unauthorized(ServiceResult.Failure("User not found."));

            var result = await _companyFollowService.GetMyFollowedCompaniesAsync(currentUserId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("my-followers")]
        public async Task<IActionResult> GetMyCompanyFollowers()
        {
            var currentUserId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(currentUserId))
                return Unauthorized(ServiceResult.Failure("User not found."));

            var result = await _companyFollowService.GetMyCompanyFollowersAsync(currentUserId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("followers-count/{employerUsername}")]
        public async Task<IActionResult> GetCompanyFollowerCount(string employerUsername)
        {
            var result = await _companyFollowService.GetCompanyFollowerCountAsync(employerUsername);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
    }
}