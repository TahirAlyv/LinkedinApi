using Linkedin.Business.Services.Interface;
using Linkedin.Core.Common;
using Linkedin.Core.Entities;
using Linkedin.DataAccess.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Linkedin.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RefreshTokenController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuthService _authService;

        public RefreshTokenController(IUnitOfWork unitOfWork,IAuthService authService)
        {
            _unitOfWork = unitOfWork;
            _authService = authService;
        }

        [HttpGet("refresh-token")]
        
        public async Task<IActionResult> RefreshToken(string tokenRequest)
        {
           
            var result = await _authService.RefreshAccessTokenAsync(tokenRequest);
            if(!result.Success)
                return Unauthorized(result.Message);

            return Ok(result.Data);


        }

    }
}
