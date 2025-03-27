using Linkedin.Business.Services.Interface;
using Linkedin.Core.Dtos.RegisterDtos;
using LinkedIn.Core.DTOs;
using LinkedIn.Core.Entities;
using Microsoft.AspNetCore.Http;
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

        public AuthController(IAuthService authService, UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
        {
            _authService = authService;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [HttpPost("jobseekers/register")]

        public async Task<ActionResult> RegisterJobSeeker([FromBody] JobSeekerRegisterDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existingUserByUsername = await _userManager.FindByNameAsync(dto.Username);
            if (existingUserByUsername != null)
            {
                return BadRequest("This username is already taken.");
            }

            var existingUserByEmail = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUserByEmail != null)
            {
                return BadRequest("An account with this email already exists.");
            }

            var user = new ApplicationUser
            {
                UserName = dto.Username,
                Email = dto.Email,
                FullName = dto.FullName,
                Bio= dto.Bio
            };

            var result = await _userManager.CreateAsync(user, dto.Password);

            if (result.Succeeded)
            {
                await _authService.AssignRole(user, "JobSeeker");
                return Ok("Employer registered successfully.");
            }
         
            return Ok("User registered successfully.");


        }



        [HttpPost("employers/register")]

        public async Task<ActionResult> RegisterEmployer([FromBody] EmployerRegisterDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existingUserByUsername = await _userManager.FindByNameAsync(dto.Username);
            if (existingUserByUsername != null)
            {
                return BadRequest("This username is already taken.");
            }

            var existingUserByEmail = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUserByEmail != null)
            {
                return BadRequest("An account with this email already exists.");
            }

            var user = new ApplicationUser
            {
                UserName = dto.Username,
                Email = dto.Email,
                CompanyName = dto.CompanyName,
                Industry = dto.Industry,
                Bio=dto.Bio,
                
            };

            var result = await _userManager.CreateAsync(user, dto.Password);

            if (result.Succeeded)
            {
                await _authService.AssignRole(user,"Employer");
                return Ok("Employer registered successfully.");
            }

            return Ok("User registered successfully.");


        }


        [HttpPost("login")]
        public async Task<ActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _userManager.FindByNameAsync(dto.Username!);
            if (user == null)
            {
                return Unauthorized("Invalid username or password.");
            }

            var passwordValid = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!passwordValid)
            {
                return Unauthorized("Invalid username or password.");
            }

            var token = await _authService.GenerateTokeen(user);
            return Ok(new { token });
        }

         
    }
}
