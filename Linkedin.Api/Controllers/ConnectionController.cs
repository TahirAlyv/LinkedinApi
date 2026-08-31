using Linkedin.Api.Hubs;
using Linkedin.Business.Services.Interface;
using Linkedin.Core.Dtos.Connection;
using Linkedin.Core.Data;
using Linkedin.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Linkedin.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ConnectionController : ControllerBase
    {
        private readonly IConnectionService _connectionService;
        private readonly IHubContext<ConnectionHub> _connectionHub;
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ConnectionController(
            IConnectionService connectionService,
            IHubContext<ConnectionHub> connectionHub,
            AppDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _connectionService = connectionService;
            _connectionHub = connectionHub;
            _context = context;
            _userManager = userManager;
        }

        [HttpPost("send/{username}")]
        public async Task<IActionResult> SendConnectionRequest(string username)
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId == null)
                return Unauthorized();

            if (await IsBlockedEitherWayAsync(currentUserId, username))
                return StatusCode(403, new { message = "Connection is unavailable between blocked accounts." });

            var result = await _connectionService.SendConnectionRequestAsync(currentUserId, username);

            if (!result.Success)
                return BadRequest(result);

            var requestDto = result.Data as ConnectionRequestDto;

            if (requestDto != null)
            {
                await _connectionHub.Clients
                    .Group($"user-{requestDto.Receiver.Id}")
                    .SendAsync("ReceiveConnectionRequest", requestDto);

                await _connectionHub.Clients
                    .Group($"user-{requestDto.Sender.Id}")
                    .SendAsync("ConnectionRequestSent", requestDto);

                return Ok(result);
            }

            var directDto = result.Data as DirectConnectionDto;

            if (directDto != null)
            {
                await _connectionHub.Clients
                    .Group($"user-{currentUserId}")
                    .SendAsync("ConnectedDirectlyByMe", directDto.TargetUser);

                await _connectionHub.Clients
                    .Group($"user-{directDto.TargetUser.Id}")
                    .SendAsync("ReceiveDirectConnection", directDto.CurrentUser);
            }

            return Ok(result);
        }

        [HttpPost("connect-directly/{username}")]
        public async Task<IActionResult> ConnectDirectly(string username)
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId == null)
                return Unauthorized();

            if (await IsBlockedEitherWayAsync(currentUserId, username))
                return StatusCode(403, new { message = "Connection is unavailable between blocked accounts." });

            var result = await _connectionService.ConnectDirectlyAsync(currentUserId, username);

            if (!result.Success)
                return BadRequest(result);

            var directDto = result.Data as DirectConnectionDto;

            if (directDto != null)
            {
                await _connectionHub.Clients
                    .Group($"user-{currentUserId}")
                    .SendAsync("ConnectedDirectlyByMe", directDto.TargetUser);

                await _connectionHub.Clients
                    .Group($"user-{directDto.TargetUser.Id}")
                    .SendAsync("ReceiveDirectConnection", directDto.CurrentUser);
            }

            return Ok(result);
        }

        private async Task<bool> IsBlockedEitherWayAsync(string currentUserId, string username)
        {
            var target = await _userManager.FindByNameAsync(username.Trim().ToLowerInvariant());
            if (target == null) return false;
            return await _context.UserBlocks.AsNoTracking().AnyAsync(item =>
                (item.BlockerId == currentUserId && item.BlockedUserId == target.Id) ||
                (item.BlockerId == target.Id && item.BlockedUserId == currentUserId));
        }

        [HttpPost("accept/{requestId}")]
        public async Task<IActionResult> AcceptRequest(int requestId)
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId == null)
                return Unauthorized();

            var result = await _connectionService.AcceptRequestAsync(currentUserId, requestId);

            if (!result.Success)
                return BadRequest(result);

            var requestDto = result.Data as ConnectionRequestDto;

            if (requestDto != null)
            {
                await _connectionHub.Clients
                    .Group($"user-{requestDto.Sender.Id}")
                    .SendAsync("ReceiveConnectionAccepted", requestDto);

                await _connectionHub.Clients
                    .Group($"user-{requestDto.Receiver.Id}")
                    .SendAsync("ConnectionRequestAcceptedByMe", requestDto);
            }

            return Ok(result);
        }

        [HttpPost("reject/{requestId}")]
        public async Task<IActionResult> RejectRequest(int requestId)
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId == null)
                return Unauthorized();

            var result = await _connectionService.RejectRequestAsync(currentUserId, requestId);

            if (!result.Success)
                return BadRequest(result);

            var requestDto = result.Data as ConnectionRequestDto;

            if (requestDto != null)
            {
                await _connectionHub.Clients
                    .Group($"user-{requestDto.Sender.Id}")
                    .SendAsync("ReceiveConnectionRejected", requestDto);

                await _connectionHub.Clients
                    .Group($"user-{requestDto.Receiver.Id}")
                    .SendAsync("ConnectionRequestRejectedByMe", requestDto);
            }

            return Ok(result);
        }

        [HttpPost("cancel/{requestId}")]
        public async Task<IActionResult> CancelRequest(int requestId)
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId == null)
                return Unauthorized();

            var result = await _connectionService.CancelRequestAsync(currentUserId, requestId);

            if (!result.Success)
                return BadRequest(result);

            var requestDto = result.Data as ConnectionRequestDto;

            if (requestDto != null)
            {
                await _connectionHub.Clients
                    .Group($"user-{requestDto.Receiver.Id}")
                    .SendAsync("ReceiveConnectionCancelled", requestDto);

                await _connectionHub.Clients
                    .Group($"user-{requestDto.Sender.Id}")
                    .SendAsync("ConnectionRequestCancelledByMe", requestDto);
            }

            return Ok(result);
        }

        [HttpGet("received")]
        public async Task<IActionResult> GetReceivedRequests()
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId == null)
                return Unauthorized();

            var result = await _connectionService.GetReceivedRequestsAsync(currentUserId);

            return Ok(result);
        }

        [HttpGet("sent")]
        public async Task<IActionResult> GetSentRequests()
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId == null)
                return Unauthorized();

            var result = await _connectionService.GetSentRequestsAsync(currentUserId);

            return Ok(result);
        }

        [HttpGet("my-connections")]
        public async Task<IActionResult> GetMyConnections()
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId == null)
                return Unauthorized();

            var result = await _connectionService.GetMyConnectionsAsync(currentUserId);

            return Ok(result);
        }

        [HttpGet("status/{username}")]
        public async Task<IActionResult> GetConnectionStatus(string username)
        {
            var currentUserId = GetCurrentUserId();

            if (currentUserId == null)
                return Unauthorized();

            var result = await _connectionService.GetConnectionStatusAsync(currentUserId, username);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        private string? GetCurrentUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        [HttpPost("remove/{username}")]
        public async Task<IActionResult> RemoveConnection(string username)
        {
            var currentUserId = GetCurrentUserId();

            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized();

            var result = await _connectionService.RemoveConnectionAsync(currentUserId, username);

            if (!result.Success)
                return BadRequest(result);

            var removedUser = result.Data as ConnectionUserDto;

            if (removedUser != null)
            {
                await _connectionHub.Clients
                    .Group($"user-{currentUserId}")
                    .SendAsync("ConnectionRemovedByMe", removedUser);

                await _connectionHub.Clients
                    .Group($"user-{removedUser.Id}")
                    .SendAsync("ReceiveConnectionRemoved", new
                    {
                        RemovedByUserId = currentUserId
                    });
            }

            return Ok(result);
        }
    }
}
