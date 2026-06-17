using Linkedin.Core.Common;
using Linkedin.Core.Dtos;
using Linkedin.Core.Dtos.Pagination;
using Linkedin.Core.Dtos.Profile.Create;
using Linkedin.Core.Dtos.Profile.Read;
using Linkedin.Core.Dtos.Profile.Update;
using Linkedin.Core.Entities;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Business.Services.Interface
{
    public interface IUserService
    {
        Task<ApplicationUser?> GetAuthenticatedUserAsync(ClaimsPrincipal user);
        Task<ServiceResult> GetSearchUser(string query, string ownerId);
        Task<ServiceResult> GetUserByUserName(string username, string currentUserId);
        Task<ProfileDetailsDto> GetMyProfileDetailsAsync(string userId);
        Task<ServiceResult> UpdateBasicInfoAsync(string userId, UpdateBasicInfoDto dto);
        Task<ServiceResult> UpdateEmployerCompanyInfoAsync(string userId, UpdateEmployerCompanyInfoDto dto);
        Task<ServiceResult> UpdateEmployerContactInfoAsync(string userId, UpdateEmployerContactInfoDto dto);
        Task<ServiceResult> UpdateProfileImageAsync(string userId, IFormFile file);
        Task<ServiceResult> DeleteProfileImageAsync(string userId);

        Task<ServiceResult> UpdateBackgroundImageAsync(string userId, IFormFile file);
        Task<ServiceResult> DeleteBackgroundImageAsync(string userId);

        Task<ServiceResult> AddExperienceAsync(string userId, CreateExperienceDto dto);
        Task<ServiceResult> UpdateExperienceAsync(string userId, int experienceId, UpdateExperienceDto dto);
        Task<ServiceResult> DeleteExperienceAsync(string userId, int experienceId);

        Task<ServiceResult> AddEducationAsync(string userId, CreateEducationDto dto);
        Task<ServiceResult> UpdateEducationAsync(string userId, int educationId, UpdateEducationDto dto);
        Task<ServiceResult> DeleteEducationAsync(string userId, int educationId);

        Task<ServiceResult> AddSkillAsync(string userId, CreateSkillDto dto);
        Task<ServiceResult> AddSkillsAsync(string userId, BulkCreateSkillDto dto);
        Task<ServiceResult> UpdateSkillAsync(string userId, int skillId, UpdateSkillDto dto);
        Task<ServiceResult> DeleteSkillAsync(string userId, int skillId);
        Task<UserLookupDto> GetUserEntityByUsernameAsync(string username);

        Task<PagedResultDto<SearchedUserDto>> GetEmployersPagedAsync(
            string currentUserId,
            int pageNumber,
            int pageSize);
        Task<PagedResultDto<SearchedUserDto>> GetJobSeekersPagedAsync(
            string currentUserId,
            int pageNumber,
            int pageSize);

        Task<ServiceResult> GetRecommendedUsersAsync(
            string currentUserId,
            int pageNumber,
            int pageSize);

        Task<ServiceResult> GetSearchHistoryAsync(
            string userId,
            int take);



    }
}
