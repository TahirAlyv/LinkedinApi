using Linkedin.Business.Services.Interface;
using Linkedin.Core.Common;
using Linkedin.Core.Dtos.Connection;
using Linkedin.Core.Entities;
using Linkedin.Core.Enums;
using Linkedin.DataAccess.Repositories.Interfaces;

namespace Linkedin.Business.Services.Concrete
{
    public class ConnectionService : IConnectionService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ConnectionService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ServiceResult> SendConnectionRequestAsync(
            string currentUserId,
            string receiverUsername)
        {
            if (string.IsNullOrWhiteSpace(receiverUsername))
                return new ServiceResult(false, "Username is required", null!);

            var sender = await _unitOfWork.Users.GetByIdAsync(currentUserId);

            if (sender == null)
                return new ServiceResult(false, "Current user not found", null!);

            var receiver = await _unitOfWork.Users.GetUserByUsername(receiverUsername);

            if (receiver == null)
                return new ServiceResult(false, "Receiver user not found", null!);

            if (sender.Id == receiver.Id)
                return new ServiceResult(false, "You cannot connect with yourself", null!);

            var alreadyConnected = await _unitOfWork.Connections
                .AreConnectedAsync(sender.Id, receiver.Id);

            if (alreadyConnected)
                return new ServiceResult(false, "You are already connected", null!);

            // Employer / business account varsa, request getmir, birbaşa connect olur.
            if (sender.UserType == UserType.Employer || receiver.UserType == UserType.Employer)
            {
                return await ConnectDirectlyAsync(currentUserId, receiverUsername);
            }

            var pendingRequest = await _unitOfWork.ConnectionRequests
                .GetPendingRequestBetweenUsersAsync(sender.Id, receiver.Id);

            if (pendingRequest != null)
            {
                if (pendingRequest.SenderId == sender.Id)
                    return new ServiceResult(false, "Connection request already sent", null!);

                return new ServiceResult(false, "This user already sent you a connection request", null!);
            }

            var request = new ConnectionRequest
            {
                SenderId = sender.Id,
                ReceiverId = receiver.Id,
                Status = ConnectionRequestStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.ConnectionRequests.AddAsync(request);

            var check = await _unitOfWork.CompleteAsync();

            if (check <= 0)
                return new ServiceResult(false, "Connection request could not be sent", null!);

            var dto = new ConnectionRequestDto
            {
                Id = request.Id,
                Sender = MapUser(sender),
                Receiver = MapUser(receiver),
                Status = request.Status,
                CreatedAt = request.CreatedAt,
                RespondedAt = request.RespondedAt
            };

            return new ServiceResult(true, "Connection request sent", dto);
        }

        public async Task<ServiceResult> ConnectDirectlyAsync(
            string currentUserId,
            string targetUsername)
        {
            if (string.IsNullOrWhiteSpace(targetUsername))
                return new ServiceResult(false, "Username is required", null!);

            var currentUser = await _unitOfWork.Users.GetByIdAsync(currentUserId);

            if (currentUser == null)
                return new ServiceResult(false, "Current user not found", null!);

            var targetUser = await _unitOfWork.Users.GetUserByUsername(targetUsername);

            if (targetUser == null)
                return new ServiceResult(false, "Target user not found", null!);

            if (currentUser.Id == targetUser.Id)
                return new ServiceResult(false, "You cannot connect with yourself", null!);

            var alreadyConnected = await _unitOfWork.Connections
                .AreConnectedAsync(currentUser.Id, targetUser.Id);

            if (alreadyConnected)
                return new ServiceResult(false, "You are already connected", null!);

            var connectedAt = DateTime.UtcNow;

            var connection1 = new Connection
            {
                UserId = currentUser.Id,
                ConnectedUserId = targetUser.Id,
                ConnectedAt = connectedAt
            };

            var connection2 = new Connection
            {
                UserId = targetUser.Id,
                ConnectedUserId = currentUser.Id,
                ConnectedAt = connectedAt
            };

            await _unitOfWork.Connections.AddAsync(connection1);
            await _unitOfWork.Connections.AddAsync(connection2);

            var check = await _unitOfWork.CompleteAsync();

            if (check <= 0)
                return new ServiceResult(false, "Connection could not be created", null!);

            var dto = new DirectConnectionDto
            {
                CurrentUser = new ConnectionUserDto
                {
                    Id = currentUser.Id,
                    Username = currentUser.UserName,
                    FullName = currentUser.FullName,
                    CurrentPosition = currentUser.CurrentPosition,
                    ProfileImage = currentUser.ProfileImage,
                    Location = currentUser.Location,
                    ConnectedAt = connectedAt
                },

                TargetUser = new ConnectionUserDto
                {
                    Id = targetUser.Id,
                    Username = targetUser.UserName,
                    FullName = targetUser.FullName,
                    CurrentPosition = targetUser.CurrentPosition,
                    ProfileImage = targetUser.ProfileImage,
                    Location = targetUser.Location,
                    ConnectedAt = connectedAt
                }
            };

            return new ServiceResult(true, "Connected successfully", dto);
        }

        public async Task<ServiceResult> AcceptRequestAsync(string currentUserId, int requestId)
        {
            var request = await _unitOfWork.ConnectionRequests
                .GetRequestWithUsersAsync(requestId);

            if (request == null)
                return new ServiceResult(false, "Connection request not found", null!);

            if (request.ReceiverId != currentUserId)
                return new ServiceResult(false, "You cannot accept this request", null!);

            if (request.Status != ConnectionRequestStatus.Pending)
                return new ServiceResult(false, "This request is not pending", null!);

            var alreadyConnected = await _unitOfWork.Connections
                .AreConnectedAsync(request.SenderId, request.ReceiverId);

            if (alreadyConnected)
                return new ServiceResult(false, "Users are already connected", null!);

            var connectedAt = DateTime.UtcNow;

            request.Status = ConnectionRequestStatus.Accepted;
            request.RespondedAt = connectedAt;

            var connection1 = new Connection
            {
                UserId = request.SenderId,
                ConnectedUserId = request.ReceiverId,
                ConnectedAt = connectedAt
            };

            var connection2 = new Connection
            {
                UserId = request.ReceiverId,
                ConnectedUserId = request.SenderId,
                ConnectedAt = connectedAt
            };

            await _unitOfWork.Connections.AddAsync(connection1);
            await _unitOfWork.Connections.AddAsync(connection2);

            var check = await _unitOfWork.CompleteAsync();

            if (check <= 0)
                return new ServiceResult(false, "Connection request could not be accepted", null!);

            var dto = MapRequest(request);

            dto.Sender.ConnectedAt = connectedAt;
            dto.Receiver.ConnectedAt = connectedAt;

            return new ServiceResult(true, "Connection request accepted", dto);
        }

        public async Task<ServiceResult> RejectRequestAsync(string currentUserId, int requestId)
        {
            var request = await _unitOfWork.ConnectionRequests
                .GetRequestWithUsersAsync(requestId);

            if (request == null)
                return new ServiceResult(false, "Connection request not found", null!);

            if (request.ReceiverId != currentUserId)
                return new ServiceResult(false, "You cannot reject this request", null!);

            if (request.Status != ConnectionRequestStatus.Pending)
                return new ServiceResult(false, "This request is not pending", null!);

            request.Status = ConnectionRequestStatus.Rejected;
            request.RespondedAt = DateTime.UtcNow;

            var check = await _unitOfWork.CompleteAsync();

            if (check <= 0)
                return new ServiceResult(false, "Connection request could not be rejected", null!);

            var dto = MapRequest(request);

            return new ServiceResult(true, "Connection request rejected", dto);
        }

        public async Task<ServiceResult> CancelRequestAsync(string currentUserId, int requestId)
        {
            var request = await _unitOfWork.ConnectionRequests
                .GetRequestWithUsersAsync(requestId);

            if (request == null)
                return new ServiceResult(false, "Connection request not found", null!);

            if (request.SenderId != currentUserId)
                return new ServiceResult(false, "You cannot cancel this request", null!);

            if (request.Status != ConnectionRequestStatus.Pending)
                return new ServiceResult(false, "This request is not pending", null!);

            request.Status = ConnectionRequestStatus.Cancelled;
            request.RespondedAt = DateTime.UtcNow;

            var check = await _unitOfWork.CompleteAsync();

            if (check <= 0)
                return new ServiceResult(false, "Connection request could not be cancelled", null!);

            var dto = MapRequest(request);

            return new ServiceResult(true, "Connection request cancelled", dto);
        }

        public async Task<ServiceResult> GetReceivedRequestsAsync(string currentUserId)
        {
            var requests = await _unitOfWork.ConnectionRequests
                .GetReceivedPendingRequestsAsync(currentUserId);

            var dtoList = requests
                .Select(MapRequest)
                .ToList();

            return new ServiceResult(true, "Received connection requests", dtoList);
        }

        public async Task<ServiceResult> GetSentRequestsAsync(string currentUserId)
        {
            var requests = await _unitOfWork.ConnectionRequests
                .GetSentPendingRequestsAsync(currentUserId);

            var dtoList = requests
                .Select(MapRequest)
                .ToList();

            return new ServiceResult(true, "Sent connection requests", dtoList);
        }

        public async Task<ServiceResult> GetMyConnectionsAsync(string currentUserId)
        {
            var connections = await _unitOfWork.Connections
                .GetUserConnectionsAsync(currentUserId);

            var dtoList = connections.Select(c => new ConnectionUserDto
            {
                Id = c.ConnectedUser.Id,
                Username = c.ConnectedUser.UserName,
                FullName = c.ConnectedUser.FullName,
                CurrentPosition = c.ConnectedUser.CurrentPosition,
                ProfileImage = c.ConnectedUser.ProfileImage,
                Location = c.ConnectedUser.Location,
                ConnectedAt = c.ConnectedAt
            }).ToList();

            return new ServiceResult(true, "My connections", dtoList);
        }

        public async Task<ServiceResult> GetConnectionStatusAsync(
            string currentUserId,
            string targetUsername)
        {
            if (string.IsNullOrWhiteSpace(targetUsername))
                return new ServiceResult(false, "Username is required", null!);

            var targetUser = await _unitOfWork.Users.GetUserByUsername(targetUsername);

            if (targetUser == null)
                return new ServiceResult(false, "Target user not found", null!);

            if (targetUser.Id == currentUserId)
            {
                return new ServiceResult(true, "Connection status", new ConnectionStatusDto
                {
                    Status = "self",
                    RequestId = null
                });
            }

            var connected = await _unitOfWork.Connections
                .AreConnectedAsync(currentUserId, targetUser.Id);

            if (connected)
            {
                return new ServiceResult(true, "Connection status", new ConnectionStatusDto
                {
                    Status = "connected",
                    RequestId = null
                });
            }

            var pendingRequest = await _unitOfWork.ConnectionRequests
                .GetPendingRequestBetweenUsersAsync(currentUserId, targetUser.Id);

            if (pendingRequest != null)
            {
                var status = pendingRequest.SenderId == currentUserId
                    ? "pending_sent"
                    : "pending_received";

                return new ServiceResult(true, "Connection status", new ConnectionStatusDto
                {
                    Status = status,
                    RequestId = pendingRequest.Id
                });
            }

            return new ServiceResult(true, "Connection status", new ConnectionStatusDto
            {
                Status = "none",
                RequestId = null
            });
        }

        private static ConnectionRequestDto MapRequest(ConnectionRequest request)
        {
            return new ConnectionRequestDto
            {
                Id = request.Id,
                Sender = MapUser(request.Sender),
                Receiver = MapUser(request.Receiver),
                Status = request.Status,
                CreatedAt = request.CreatedAt,
                RespondedAt = request.RespondedAt
            };
        }

        public async Task<ServiceResult> RemoveConnectionAsync(string currentUserId, string targetUsername)
        {
            if (string.IsNullOrWhiteSpace(targetUsername))
                return new ServiceResult(false, "Username is required", null);

            var currentUser = await _unitOfWork.Users.GetByIdAsync(currentUserId);

            if (currentUser == null)
                return new ServiceResult(false, "Current user not found", null);

            var targetUser = await _unitOfWork.Users.GetUserByUsername(targetUsername);

            if (targetUser == null)
                return new ServiceResult(false, "Target user not found", null);

            if (currentUser.Id == targetUser.Id)
                return new ServiceResult(false, "You cannot remove yourself", null);

            var isConnected = await _unitOfWork.Connections.ExistsAsync(
                currentUser.Id,
                targetUser.Id
            );

            if (!isConnected)
                return new ServiceResult(false, "Connection not found", null);

            await _unitOfWork.Connections.RemoveConnectionAsync(
                currentUser.Id,
                targetUser.Id
            );

            await _unitOfWork.CompleteAsync();

            var removedUserDto = new ConnectionUserDto
            {
                Id = targetUser.Id,
                Username = targetUser.UserName,
                FullName = targetUser.FullName,
                CurrentPosition = targetUser.CurrentPosition,
                ProfileImage = targetUser.ProfileImage,
                Location = targetUser.Location
            };

            return new ServiceResult(true, "Connection removed successfully", removedUserDto);
        }

 
        private static ConnectionUserDto MapUser(ApplicationUser user)
        {
            return new ConnectionUserDto
            {
                Id = user.Id,
                Username = user.UserName,
                FullName = user.FullName,
                CurrentPosition = user.CurrentPosition,
                ProfileImage = user.ProfileImage,
                Location = user.Location
            };
        }
    }
}