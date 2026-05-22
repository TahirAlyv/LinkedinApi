using Linkedin.Business.Services.Interface;
using Linkedin.Core.Dtos;
using Linkedin.Core.Dtos.RegisterDtos;
using Linkedin.Core.Entities;
using Linkedin.Core.Enums;
using LinkedIn.Core.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Linkedin.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;

        public AuthController(
            IAuthService authService,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager)
        {
            _authService = authService;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [HttpPost("jobseekers/register")]
        public async Task<ActionResult> RegisterJobSeeker([FromBody] JobSeekerRegisterDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existingUserByUsername = await _userManager.FindByNameAsync(dto.Username);
            if (existingUserByUsername != null)
                return BadRequest("This username is already taken.");

            var existingUserByEmail = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUserByEmail != null)
                return BadRequest("An account with this email already exists.");

            var user = new ApplicationUser
            {
                UserName = dto.Username,
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

            return Ok("User registered successfully.");
        }

        [HttpPost("employers/register")]
        public async Task<ActionResult> RegisterEmployer([FromBody] EmployerRegisterDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existingUserByUsername = await _userManager.FindByNameAsync(dto.Username);
            if (existingUserByUsername != null)
                return BadRequest("This username is already taken.");

            var existingUserByEmail = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUserByEmail != null)
                return BadRequest("An account with this email already exists.");

            var user = new ApplicationUser
            {
                UserName = dto.Username,
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

            return Ok("Employer registered successfully.");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _userManager.FindByNameAsync(dto.Username);
            if (user == null)
                return Unauthorized("Invalid username or password.");

            if (user.IsBlocked)
                return StatusCode(403, new { message = "Your account has been blocked." });

            var passwordValid = await _userManager.CheckPasswordAsync(user, dto.Password!);

            if (!passwordValid)
                return Unauthorized("Invalid username or password.");

            var accessToken = await _authService.GenerateTokeen(user);

            var refreshToken = _authService.GenerateRefreshToken();

            await _authService.SaveRefreshTokenAsync(user, refreshToken);

            return Ok(new
            {
                accessToken,
                refreshToken,
            });
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
    }
}