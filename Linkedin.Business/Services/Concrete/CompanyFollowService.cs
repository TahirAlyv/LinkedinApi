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

        public CompanyFollowService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
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

            if (follower.UserType == UserType.Employer)
                return ServiceResult.Failure("Employer accounts cannot follow companies.");

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

            if (follower.UserType == UserType.Employer)
                return ServiceResult.Failure("Employer accounts cannot unfollow companies.");

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

            if (currentUser == null || currentUser.UserType == UserType.Employer)
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

            if (user.UserType == UserType.Employer)
                return ServiceResult.SuccessResult("Employer accounts do not follow companies.", new List<CompanyFollowDto>());

            var follows = await _unitOfWork.CompanyFollows.GetFollowedCompaniesAsync(currentUserId);

            var dto = follows.Select(cf => new CompanyFollowDto
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

        public async Task<ServiceResult> GetMyCompanyFollowersAsync(string currentUserId)
        {
            var employer = await _unitOfWork.Users.GetByIdAsync(currentUserId);

            if (employer == null)
                return ServiceResult.Failure("User not found.");

            if (employer.UserType != UserType.Employer)
                return ServiceResult.Failure("Only employer accounts can view company followers.");

            var followers = await _unitOfWork.CompanyFollows.GetCompanyFollowersAsync(currentUserId);

            var dto = followers.Select(cf => new CompanyFollowerDto
            {
                FollowerId = cf.FollowerId,
                Username = cf.Follower.UserName,
                FullName = cf.Follower.FullName,
                CurrentPosition = cf.Follower.CurrentPosition,
                ProfileImage = cf.Follower.ProfileImage,
                Location = cf.Follower.Location,
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