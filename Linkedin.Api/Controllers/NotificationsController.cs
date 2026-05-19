using Linkedin.Business.Services.Concrete;
using Linkedin.Business.Services.Interface;
using Linkedin.DataAccess.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Linkedin.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserService _userService;
        private readonly INotficationsService _notificationService;

        public NotificationsController(IUnitOfWork unitOfWork, IUserService userService, INotficationsService notificationService)
        {
            _unitOfWork = unitOfWork;
            _userService = userService;
            _notificationService = notificationService;
        }

        [HttpGet("notifications")]

        public async Task<IActionResult> GetNotifications()
        {
            var user = await _userService.GetAuthenticatedUserAsync(User);
            if (user == null)
                return Unauthorized("User not found!");

            var notifications = await _notificationService.GetNotificationsForUserAsync(user.Id);

            return Ok(notifications);
        }




    }
}

