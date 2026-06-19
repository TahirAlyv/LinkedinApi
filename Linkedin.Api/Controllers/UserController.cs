using Linkedin.Business.Services.Interface;
using Linkedin.Core.Common;
using Linkedin.Core.Dtos;
using Linkedin.Core.Dtos.Profile.Create;
using Linkedin.Core.Dtos.Profile.Update;
using Linkedin.Core.Entities;
using Linkedin.DataAccess.Repositories.Interfaces;
 
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Linkedin.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly UserManager<ApplicationUser> _userManager; 
        public UserController(IUserService userService, UserManager<ApplicationUser> userManager)
        {
            _userService = userService;
            this._userManager = userManager;
        }



        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            var ownerUser = await _userService.GetAuthenticatedUserAsync(User);
            if (ownerUser == null)
                return Unauthorized(ServiceResult.Failure("User not found"));

            var result = await _userService.GetMyProfileDetailsAsync(ownerUser.Id);
 
            return Ok(result);
        }


        [HttpGet("users")]
        public async Task<IActionResult> SearchUser([FromQuery] string? query)
        {
            var ownerUser = await _userService.GetAuthenticatedUserAsync(User);

            if (ownerUser == null)
                return Unauthorized(ServiceResult.Failure("User not found"));

            var result = await _userService.GetSearchUser(query ?? string.Empty, ownerUser.Id);

            return Ok(result);
        }

        [HttpGet("{username}")]
        public async Task<IActionResult> GetUserByUsername(string username)
        {
            var currentUser = await _userService.GetAuthenticatedUserAsync(User);

            if (currentUser == null)
                return Unauthorized(ServiceResult.Failure("User not found"));

            var result = await _userService.GetUserByUserName(username, currentUser.Id);

            if (!result.Success)
                return NotFound(result);

            return Ok(result.Data);
        }


        [HttpPut("basic-info")]
        public async Task<IActionResult> UpdateBasicInfo([FromBody] UpdateBasicInfoDto dto)
        {
            var ownerUser = await _userService.GetAuthenticatedUserAsync(User);

            if (ownerUser == null)
                return Unauthorized(ServiceResult.Failure("User not found"));

            var result = await _userService.UpdateBasicInfoAsync(ownerUser.Id, dto);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPut("profile-image")]
        public async Task<IActionResult> UpdateProfileImage(IFormFile file)
        {
            var ownerUser = await _userService.GetAuthenticatedUserAsync(User);
            if (ownerUser == null)
                return Unauthorized(ServiceResult.Failure("User not found"));

            var result = await _userService.UpdateProfileImageAsync(ownerUser.Id, file);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpDelete("profile-image")]
        public async Task<IActionResult> DeleteProfileImage()
        {
            var ownerUser = await _userService.GetAuthenticatedUserAsync(User);
            if (ownerUser == null)
                return Unauthorized(ServiceResult.Failure("User not found"));

            var result = await _userService.DeleteProfileImageAsync(ownerUser.Id);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPut("background-image")]
        public async Task<IActionResult> UpdateBackgroundImage(IFormFile file)
        {
            var ownerUser = await _userService.GetAuthenticatedUserAsync(User);
            if (ownerUser == null)
                return Unauthorized(ServiceResult.Failure("User not found"));

            var result = await _userService.UpdateBackgroundImageAsync(ownerUser.Id, file);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpDelete("background-image")]
        public async Task<IActionResult> DeleteBackgroundImage()
        {
            var ownerUser = await _userService.GetAuthenticatedUserAsync(User);
            if (ownerUser == null)
                return Unauthorized(ServiceResult.Failure("User not found"));

            var result = await _userService.DeleteBackgroundImageAsync(ownerUser.Id);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }


        [HttpPost("experience")]
        public async Task<IActionResult> AddExperience([FromBody] CreateExperienceDto dto)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var result = await _userService.AddExperienceAsync(user.Id, dto);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }


        [HttpPut("experience/{experienceId}")]
        public async Task<IActionResult> UpdateExperience(int experienceId, [FromBody] UpdateExperienceDto dto)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var result = await _userService.UpdateExperienceAsync(user.Id, experienceId, dto);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }


        [HttpDelete("experience/{experienceId}")]
        public async Task<IActionResult> DeleteExperience(int experienceId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var result = await _userService.DeleteExperienceAsync(user.Id, experienceId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("education")]
        public async Task<IActionResult> AddEducation([FromBody] CreateEducationDto dto)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var result = await _userService.AddEducationAsync(user.Id, dto);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPut("education/{educationId}")]
        public async Task<IActionResult> UpdateEducation(int educationId, [FromBody] UpdateEducationDto dto)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var result = await _userService.UpdateEducationAsync(user.Id, educationId, dto);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpDelete("education/{educationId}")]
        public async Task<IActionResult> DeleteEducation(int educationId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var result = await _userService.DeleteEducationAsync(user.Id, educationId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("skill")]
        public async Task<IActionResult> AddSkill([FromBody] CreateSkillDto dto)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var result = await _userService.AddSkillAsync(user.Id, dto);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("skills")]
public async Task<IActionResult> AddSkills([FromBody] BulkCreateSkillDto dto)
{
    var user = await _userManager.GetUserAsync(User);

    if (user == null)
        return Unauthorized();

    var result = await _userService.AddSkillsAsync(user.Id, dto);

    if (!result.Success)
        return BadRequest(result);

    return Ok(result);
}

        [HttpPut("skill/{skillId}")]
        public async Task<IActionResult> UpdateSkill(int skillId, [FromBody] UpdateSkillDto dto)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var result = await _userService.UpdateSkillAsync(user.Id, skillId, dto);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpDelete("skill/{skillId}")]
        public async Task<IActionResult> DeleteSkill(int skillId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var result = await _userService.DeleteSkillAsync(user.Id, skillId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }


        [HttpPut("employer/company-info")]
        public async Task<IActionResult> UpdateEmployerCompanyInfo([FromBody] UpdateEmployerCompanyInfoDto dto)
        {
            var ownerUser = await _userService.GetAuthenticatedUserAsync(User);

            if (ownerUser == null)
                return Unauthorized(ServiceResult.Failure("User not found"));

            var result = await _userService.UpdateEmployerCompanyInfoAsync(ownerUser.Id, dto);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPut("employer/contact-info")]
        public async Task<IActionResult> UpdateEmployerContactInfo([FromBody] UpdateEmployerContactInfoDto dto)
        {
            var ownerUser = await _userService.GetAuthenticatedUserAsync(User);

            if (ownerUser == null)
                return Unauthorized(ServiceResult.Failure("User not found"));

            var result = await _userService.UpdateEmployerContactInfoAsync(ownerUser.Id, dto);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }


        [HttpGet("employers")]
        public async Task<IActionResult> GetEmployers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
        {
            var currentUser = await _userService.GetAuthenticatedUserAsync(User);

            if (currentUser == null)
                return Unauthorized();

            var result = await _userService.GetEmployersPagedAsync(
                currentUser.Id,
                pageNumber,
                pageSize
            );

            return Ok(result);
        }

        [HttpGet("jobseekers")]
        public async Task<IActionResult> GetJobSeekers(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var currentUser = await _userService.GetAuthenticatedUserAsync(User);

            if (currentUser == null)
                return Unauthorized();

            var result = await _userService.GetJobSeekersPagedAsync(
                currentUser.Id,
                pageNumber,
                pageSize
            );

            return Ok(result);
        }



        [HttpGet("recommended")]
        public async Task<IActionResult> GetRecommendedUsers(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var currentUser = await _userService.GetAuthenticatedUserAsync(User);

            if (currentUser == null)
                return Unauthorized(ServiceResult.Failure("User not found"));

            var result = await _userService.GetRecommendedUsersAsync(
                currentUser.Id,
                pageNumber,
                pageSize);

            return Ok(result);
        }

        [HttpGet("search-history")]
        public async Task<IActionResult> GetSearchHistory(
            [FromQuery] int take = 10)
        {
            var currentUser = await _userService.GetAuthenticatedUserAsync(User);

            if (currentUser == null)
                return Unauthorized(ServiceResult.Failure("User not found"));

            var result = await _userService.GetSearchHistoryAsync(
                currentUser.Id,
                take);

            return Ok(result);
        }



        [HttpGet("check-profile-image")]
        public IActionResult CheckProfileImage()
        {
            var fileName = "cb8f32fe-1b3f-47a4-85f6-cfc0e524e978.jpg";

            var path = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "images",
                "profiles",
                fileName
            );

            return Ok(new
            {
                exists = System.IO.File.Exists(path),
                path = path
            });
        }






    }
}
