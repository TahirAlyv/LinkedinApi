using Linkedin.Business.Services.Interface;
using Linkedin.Core.Common;
using Linkedin.Core.Dtos.CompanyFolllow;
using Linkedin.Core.Entities;
using Linkedin.Core.Enums;
using Linkedin.DataAccess.Repositories.Interfaces;

namespace Linkedin.Business.Services.Concrete
{
    public class CompanyFollowService : ICompanyFollowService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotficationsService _notificationsService;

        public CompanyFollowService(
            IUnitOfWork unitOfWork,
            INotficationsService notificationsService)
        {
            _unitOfWork = unitOfWork;
            _notificationsService = notificationsService;
        }

        public async Task<ServiceResult> FollowCompanyAsync(string currentUserId, string employerUsername)
        {
            if (string.IsNullOrWhiteSpace(currentUserId))
                return ServiceResult.Failure("User not found.");

            if (string.IsNullOrWhiteSpace(employerUsername))
                return ServiceResult.Failure("Company username is required.");

            var follower = await _unitOfWork.Users.GetByIdAsync(currentUserId);

            if (follower == null)
                return ServiceResult.Failure("User not found.");

            var employer = await _unitOfWork.Users.GetUserByUsername(employerUsername);

            if (employer == null)
                return ServiceResult.Failure("Company not found.");

            if (employer.UserType != UserType.Employer)
                return ServiceResult.Failure("You can only follow company pages.");

            if (employer.Id == currentUserId)
                return ServiceResult.Failure("You cannot follow your own company.");

            var existingFollow = await _unitOfWork.CompanyFollows.GetFollowAsync(
                currentUserId,
                employer.Id
            );

            if (existingFollow != null)
                return ServiceResult.SuccessResult("Already following this company.", new
                {
                    isFollowing = true
                });

            var follow = new CompanyFollow
            {
                FollowerId = currentUserId,
                EmployerId = employer.Id,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.CompanyFollows.AddAsync(follow);
            await _unitOfWork.CompleteAsync();

            await _notificationsService.CreateOrUpdateAsync(
                senderId: follower.Id,
                receiverId: employer.Id,
                type: NotificationType.Follow,
                postId: null,
                contentPreview: "started following your company",
                senderUsername: follower.UserName ?? follower.FullName ?? "Member",
                senderProfilePhoto: follower.ProfileImage ?? "");

            var count = await _unitOfWork.CompanyFollows.GetFollowerCountAsync(employer.Id);

            return ServiceResult.SuccessResult("Company followed successfully.", new
            {
                isFollowing = true,
                followerCount = count
            });
        }

        public async Task<ServiceResult> UnfollowCompanyAsync(string currentUserId, string employerUsername)
        {
            if (string.IsNullOrWhiteSpace(currentUserId))
                return ServiceResult.Failure("User not found.");

            var follower = await _unitOfWork.Users.GetByIdAsync(currentUserId);

            if (follower == null)
                return ServiceResult.Failure("User not found.");

            var employer = await _unitOfWork.Users.GetUserByUsername(employerUsername);

            if (employer == null)
                return ServiceResult.Failure("Company not found.");

            var existingFollow = await _unitOfWork.CompanyFollows.GetFollowAsync(
                currentUserId,
                employer.Id
            );

            if (existingFollow == null)
                return ServiceResult.SuccessResult("You are not following this company.", new
                {
                    isFollowing = false
                });

            _unitOfWork.CompanyFollows.Remove(existingFollow);
            await _unitOfWork.CompleteAsync();

            var count = await _unitOfWork.CompanyFollows.GetFollowerCountAsync(employer.Id);

            return ServiceResult.SuccessResult("Company unfollowed successfully.", new
            {
                isFollowing = false,
                followerCount = count
            });
        }

        public async Task<ServiceResult> GetFollowStatusAsync(string currentUserId, string employerUsername)
        {
            if (string.IsNullOrWhiteSpace(currentUserId))
                return ServiceResult.SuccessResult("successful", new
                {
                    isFollowing = false
                });

            var currentUser = await _unitOfWork.Users.GetByIdAsync(currentUserId);

            if (currentUser == null)
                return ServiceResult.SuccessResult("successful", new
                {
                    isFollowing = false,
                    canFollow = false
                });

            var employer = await _unitOfWork.Users.GetUserByUsername(employerUsername);

            if (employer == null || employer.UserType != UserType.Employer)
                return ServiceResult.Failure("Company not found.");

            var follow = await _unitOfWork.CompanyFollows.GetFollowAsync(
                currentUserId,
                employer.Id
            );

            var count = await _unitOfWork.CompanyFollows.GetFollowerCountAsync(employer.Id);

            return ServiceResult.SuccessResult("successful", new
            {
                isFollowing = follow != null,
                canFollow = employer.Id != currentUserId,
                followerCount = count
            });
        }

        public async Task<ServiceResult> GetMyFollowedCompaniesAsync(string currentUserId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(currentUserId);

            if (user == null)
                return ServiceResult.Failure("User not found.");

            var follows = await _unitOfWork.CompanyFollows.GetFollowedCompaniesAsync(currentUserId);

            var dto = follows
                .Where(cf => cf.Employer.UserType == UserType.Employer)
                .Select(cf => new CompanyFollowDto
            {
                EmployerId = cf.EmployerId,
                Username = cf.Employer.UserName,
                CompanyName = cf.Employer.Company != null
                    ? cf.Employer.Company.Name
                    : cf.Employer.FullName,
                Industry = cf.Employer.Company != null
                    ? cf.Employer.Company.Industry
                    : cf.Employer.CurrentPosition,
                LogoUrl = cf.Employer.Company != null && cf.Employer.Company.LogoUrl != null
                    ? cf.Employer.Company.LogoUrl
                    : cf.Employer.ProfileImage,
                Location = cf.Employer.Company != null && cf.Employer.Company.Location != null
                    ? cf.Employer.Company.Location
                    : cf.Employer.Location,
                FollowedAt = cf.CreatedAt
            }).ToList();

            return ServiceResult.SuccessResult("successful", dto);
        }

        public async Task<ServiceResult> GetMyFollowingAsync(string currentUserId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(currentUserId);
            if (user == null)
                return ServiceResult.Failure("User not found.");

            var follows = await _unitOfWork.CompanyFollows.GetFollowedCompaniesAsync(currentUserId);
            var dto = follows.Select(cf => new
            {
                id = cf.EmployerId,
                username = cf.Employer.UserName,
                fullName = cf.Employer.UserType == UserType.Employer && cf.Employer.Company != null
                    ? cf.Employer.Company.Name
                    : cf.Employer.FullName,
                currentPosition = cf.Employer.UserType == UserType.Employer && cf.Employer.Company != null
                    ? cf.Employer.Company.Industry
                    : cf.Employer.CurrentPosition,
                profileImage = cf.Employer.UserType == UserType.Employer && cf.Employer.Company != null
                    ? cf.Employer.Company.LogoUrl ?? cf.Employer.ProfileImage
                    : cf.Employer.ProfileImage,
                location = cf.Employer.UserType == UserType.Employer && cf.Employer.Company != null
                    ? cf.Employer.Company.Location ?? cf.Employer.Location
                    : cf.Employer.Location,
                userType = cf.Employer.UserType.ToString(),
                followedAt = cf.CreatedAt
            }).ToList();

            return ServiceResult.SuccessResult("successful", dto);
        }

        public async Task<ServiceResult> FollowUserAsync(string currentUserId, string username)
        {
            var follower = await _unitOfWork.Users.GetByIdAsync(currentUserId);
            var target = await _unitOfWork.Users.GetUserByUsername(username);
            if (follower == null || target == null)
                return ServiceResult.Failure("User not found.");
            if (follower.UserType != UserType.Employer)
                return ServiceResult.Failure("Only company accounts can follow members from this action.");
            if (target.UserType != UserType.JobSeeker)
                return ServiceResult.Failure("This action is only for member profiles.");
            if (target.Id == currentUserId)
                return ServiceResult.Failure("You cannot follow your own account.");

            var existing = await _unitOfWork.CompanyFollows.GetFollowAsync(currentUserId, target.Id);
            if (existing == null)
            {
                await _unitOfWork.CompanyFollows.AddAsync(new CompanyFollow
                {
                    FollowerId = currentUserId,
                    EmployerId = target.Id,
                    CreatedAt = DateTime.UtcNow
                });
                await _unitOfWork.CompleteAsync();
                await _notificationsService.CreateOrUpdateAsync(
                    follower.Id,
                    target.Id,
                    NotificationType.Follow,
                    null,
                    "started following you",
                    follower.UserName ?? follower.FullName ?? "Company",
                    follower.Company?.LogoUrl ?? follower.ProfileImage ?? "");
            }

            return ServiceResult.SuccessResult("Member followed successfully.", new
            {
                isFollowing = true,
                followerCount = await _unitOfWork.CompanyFollows.GetFollowerCountAsync(target.Id)
            });
        }

        public async Task<ServiceResult> UnfollowUserAsync(string currentUserId, string username)
        {
            var target = await _unitOfWork.Users.GetUserByUsername(username);
            if (target == null) return ServiceResult.Failure("User not found.");
            var existing = await _unitOfWork.CompanyFollows.GetFollowAsync(currentUserId, target.Id);
            if (existing != null)
            {
                _unitOfWork.CompanyFollows.Remove(existing);
                await _unitOfWork.CompleteAsync();
            }
            return ServiceResult.SuccessResult("Member unfollowed.", new
            {
                isFollowing = false,
                followerCount = await _unitOfWork.CompanyFollows.GetFollowerCountAsync(target.Id)
            });
        }

        public async Task<ServiceResult> GetUserFollowStatusAsync(string currentUserId, string username)
        {
            var current = await _unitOfWork.Users.GetByIdAsync(currentUserId);
            var target = await _unitOfWork.Users.GetUserByUsername(username);
            if (current == null || target == null)
                return ServiceResult.Failure("User not found.");
            var follow = await _unitOfWork.CompanyFollows.GetFollowAsync(currentUserId, target.Id);
            return ServiceResult.SuccessResult("successful", new
            {
                isFollowing = follow != null,
                canFollow = current.UserType == UserType.Employer &&
                            target.UserType == UserType.JobSeeker &&
                            current.Id != target.Id,
                followerCount = await _unitOfWork.CompanyFollows.GetFollowerCountAsync(target.Id)
            });
        }

        public async Task<ServiceResult> GetMyCompanyFollowersAsync(string currentUserId)
        {
            var employer = await _unitOfWork.Users.GetByIdAsync(currentUserId);

            if (employer == null)
                return ServiceResult.Failure("User not found.");

            var followers = await _unitOfWork.CompanyFollows.GetCompanyFollowersAsync(currentUserId);

            var dto = followers.Select(cf => new CompanyFollowerDto
            {
                FollowerId = cf.FollowerId,
                Username = cf.Follower.UserName,
                FullName = cf.Follower.UserType == UserType.Employer && cf.Follower.Company != null
                    ? cf.Follower.Company.Name
                    : cf.Follower.FullName,
                CurrentPosition = cf.Follower.UserType == UserType.Employer && cf.Follower.Company != null
                    ? cf.Follower.Company.Industry
                    : cf.Follower.CurrentPosition,
                ProfileImage = cf.Follower.UserType == UserType.Employer && cf.Follower.Company != null
                    ? cf.Follower.Company.LogoUrl ?? cf.Follower.ProfileImage
                    : cf.Follower.ProfileImage,
                Location = cf.Follower.UserType == UserType.Employer && cf.Follower.Company != null
                    ? cf.Follower.Company.Location ?? cf.Follower.Location
                    : cf.Follower.Location,
                FollowedAt = cf.CreatedAt
            }).ToList();

            return ServiceResult.SuccessResult("successful", dto);
        }

        public async Task<ServiceResult> GetCompanyFollowerCountAsync(string employerUsername)
        {
            var employer = await _unitOfWork.Users.GetUserByUsername(employerUsername);

            if (employer == null || employer.UserType != UserType.Employer)
                return ServiceResult.Failure("Company not found.");

            var count = await _unitOfWork.CompanyFollows.GetFollowerCountAsync(employer.Id);

            return ServiceResult.SuccessResult("successful", new
            {
                followerCount = count
            });
        }
    }
}
