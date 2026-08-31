using Linkedin.Business.Services.Interface;
using Linkedin.Core.Common;
using Linkedin.Core.Data;
using Linkedin.Core.Dtos;
using Linkedin.Core.Dtos.Pagination;
using Linkedin.Core.Dtos.Profile.Create;
using Linkedin.Core.Dtos.Profile.Read;
using Linkedin.Core.Dtos.Profile.Update;
using Linkedin.Core.Entities;
using Linkedin.Core.Enums;
using Linkedin.DataAccess.Repositories.Concrete;
using Linkedin.DataAccess.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Linkedin.Business.Services.Concrete
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUploadImage _uploadImage;
        private readonly AppDbContext _dbContext;

        public UserService(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor, UserManager<ApplicationUser> userManager, IUploadImage uploadImage, AppDbContext dbContext)
        {
            _unitOfWork = unitOfWork;
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
            _uploadImage = uploadImage;
            _dbContext = dbContext;
        }


        public async Task<ApplicationUser?> GetAuthenticatedUserAsync(ClaimsPrincipal user)
        {

            var userId = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return null;
            }

            var userData = await _unitOfWork.Users.GetByIdAsync(userId);
            userData = await _userManager.Users
           .Include(u => u.Company)
           .FirstOrDefaultAsync(u => u.Id == userId);

            return userData;
        }

        public async Task<ServiceResult> GetSearchUser(string query, string ownerId)
        {
            if (string.IsNullOrWhiteSpace(query))
                return ServiceResult.SuccessResult("successful", new List<SearchedUserDto>());

            await _unitOfWork.Users.AddSearchHistoryAsync(ownerId, query);
            await _unitOfWork.CompleteAsync();

            var users = await _unitOfWork.Users.GetSearchUsers(query);

            if (users == null || !users.Any())
                return ServiceResult.SuccessResult("successful", new List<SearchedUserDto>());

            var blockedUserIds = await _dbContext.UserBlocks
                .AsNoTracking()
                .Where(item => item.BlockerId == ownerId || item.BlockedUserId == ownerId)
                .Select(item => item.BlockerId == ownerId ? item.BlockedUserId : item.BlockerId)
                .Distinct()
                .ToListAsync();

            users = users
                .Where(item => !blockedUserIds.Contains(item.Id))
                .ToList();

            if (!users.Any())
                return ServiceResult.SuccessResult("successful", new List<SearchedUserDto>());

            foreach (var userDto in users)
            {
                if (string.IsNullOrWhiteSpace(userDto.Username))
                {
                    userDto.Role = "User";
                    continue;
                }

                var appUser = await _userManager.FindByNameAsync(userDto.Username);

                if (appUser == null)
                {
                    userDto.Role = "User";
                    continue;
                }

                var roles = await _userManager.GetRolesAsync(appUser);
                userDto.Role = roles.FirstOrDefault() ?? "User";
            }

            return ServiceResult.SuccessResult("successful", users);
        }

        public async Task<ServiceResult> SearchProfileOptionsAsync(
            string userId,
            string type,
            string? query,
            int page,
            int pageSize)
        {
            if (!Enum.TryParse<ProfileOptionType>(type, true, out var optionType) ||
                !Enum.IsDefined(typeof(ProfileOptionType), optionType))
            {
                return ServiceResult.Failure("Profile option type is invalid");
            }

            var normalizedQuery = query?.Trim().ToUpperInvariant() ?? string.Empty;
            var safePage = Math.Max(page, 1);
            var safePageSize = Math.Clamp(pageSize, 1, 20);

            var options = await _dbContext.ProfileOptions
                .AsNoTracking()
                .Where(option =>
                    option.Type == optionType &&
                    (option.IsApproved ||
                     (optionType != ProfileOptionType.Industry &&
                      option.CreatedByUserId == userId)) &&
                    (normalizedQuery == string.Empty ||
                     option.NormalizedName.Contains(normalizedQuery)))
                .OrderByDescending(option =>
                    normalizedQuery != string.Empty &&
                    option.NormalizedName.StartsWith(normalizedQuery))
                .ThenBy(option => option.Name)
                .ThenBy(option => option.Id)
                .Skip((safePage - 1) * safePageSize)
                .Take(safePageSize + 1)
                .Select(option => new ProfileOptionDto
                {
                    Id = option.Id,
                    Name = option.Name,
                    Type = option.Type.ToString()
                })
                .ToListAsync();

            var hasMore = options.Count > safePageSize;

            return ServiceResult.SuccessResult(
                "Profile options retrieved successfully",
                new
                {
                    items = options.Take(safePageSize).ToList(),
                    page = safePage,
                    pageSize = safePageSize,
                    hasMore
                });
        }

        public async Task<ServiceResult> SearchOrganizationsAsync(
            string userId,
            string? query,
            string? purpose,
            int page,
            int pageSize)
        {
            var cleanQuery = query?.Trim() ?? string.Empty;
            var cleanPurpose = purpose?.Trim().ToLowerInvariant();
            var safePage = Math.Max(page, 1);
            var safePageSize = Math.Clamp(pageSize, 1, 20);

            var organizations = await _dbContext.Set<Company>()
                .AsNoTracking()
                .Where(company =>
                    (cleanPurpose != "education" || company.Industry == "Education") &&
                    (cleanQuery == string.Empty || company.Name.Contains(cleanQuery)))
                .OrderByDescending(company =>
                    cleanQuery != string.Empty && company.Name.StartsWith(cleanQuery))
                .ThenBy(company => company.Name)
                .ThenBy(company => company.Id)
                .Skip((safePage - 1) * safePageSize)
                .Take(safePageSize + 1)
                .Select(company => new OrganizationOptionDto
                {
                    Id = company.Id,
                    Name = company.Name,
                    Username = company.User != null ? company.User.UserName : null,
                    LogoUrl = company.LogoUrl ??
                        (company.User != null ? company.User.ProfileImage : null),
                    Industry = company.Industry
                })
                .ToListAsync();

            var hasMore = organizations.Count > safePageSize;

            return ServiceResult.SuccessResult(
                "Organizations retrieved successfully",
                new
                {
                    items = organizations.Take(safePageSize).ToList(),
                    page = safePage,
                    pageSize = safePageSize,
                    hasMore
                });
        }

        private async Task TrackCustomProfileOptionAsync(
            ProfileOptionType type,
            string? name,
            string userId)
        {
            var cleanName = name?.Trim();
            if (string.IsNullOrWhiteSpace(cleanName) || type == ProfileOptionType.Industry)
                return;

            var normalizedName = cleanName.ToUpperInvariant();
            var exists = await _dbContext.ProfileOptions
                .AnyAsync(option =>
                    option.Type == type &&
                    option.NormalizedName == normalizedName);

            if (exists)
                return;

            _dbContext.ProfileOptions.Add(new ProfileOption
            {
                Type = type,
                Name = cleanName,
                NormalizedName = normalizedName,
                IsApproved = false,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            });

            await _dbContext.SaveChangesAsync();
        }

        private async Task<Company?> FindOrganizationAsync(int? companyId)
        {
            if (!companyId.HasValue)
                return null;

            return await _dbContext.Set<Company>()
                .Include(company => company.User)
                .FirstOrDefaultAsync(company => company.Id == companyId.Value);
        }

        public async Task<ServiceResult> GetUserByUserName(string username, string currentUserId)
        {
            var targetUser = await _unitOfWork.Users.GetUserByUsername(username);

            if (targetUser == null)
                return new ServiceResult(false, "user not found!", null!);

            var targetUserRole = (await _userManager.GetRolesAsync(targetUser)).FirstOrDefault() ?? "User";

            var profile = await _unitOfWork.Users
                .GetProfileDetailsByUsernameAsync(username, currentUserId, targetUserRole);

            if (profile == null)
                return new ServiceResult(false, "user not found!", null!);

            if (targetUser.UserType == UserType.JobSeeker)
                profile.OpenToWork = await LoadOpenToWorkAsync(
                    targetUser.Id,
                    includeInactive: targetUser.Id == currentUserId);

            return new ServiceResult(true, "successful", profile);
        }

        public async Task<ProfileDetailsDto?> GetMyProfileDetailsAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return null;

            var roles = await _userManager.GetRolesAsync(user);
            string currentUserRole = roles.FirstOrDefault() ?? "None";

            var profile = await _unitOfWork.Users.GetMyProfileDetailsAsync(userId, currentUserRole);

            if (profile != null && user.UserType == UserType.JobSeeker)
                profile.OpenToWork = await LoadOpenToWorkAsync(
                    userId,
                    includeInactive: true);

            return profile;
        }

        private async Task<OpenToWorkProfileDto?> LoadOpenToWorkAsync(
            string userId,
            bool includeInactive)
        {
            var preference = await _dbContext.JobPreferences
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.UserId == userId);

            if (preference == null)
            {
                return includeInactive
                    ? new OpenToWorkProfileDto()
                    : null;
            }

            if (!preference.IsOpenToWork && !includeInactive)
                return null;

            return new OpenToWorkProfileDto
            {
                IsOpenToWork = preference.IsOpenToWork,
                JobTitles = SplitPreference(preference.JobTitles),
                WorkplaceTypes = SplitPreference(preference.WorkplaceTypes),
                OnsiteLocations = SplitPreference(preference.OnsiteLocations),
                RemoteLocations = SplitPreference(preference.RemoteLocations),
                EmploymentTypes = SplitPreference(preference.EmploymentTypes),
                StartAvailability = string.IsNullOrWhiteSpace(
                    preference.StartAvailability)
                        ? "Immediately"
                        : preference.StartAvailability,
                UpdatedAt = preference.UpdatedAt
            };
        }

        private static List<string> SplitPreference(string? value) =>
            string.IsNullOrWhiteSpace(value)
                ? new List<string>()
                : value.Split(
                        '|',
                        StringSplitOptions.RemoveEmptyEntries |
                        StringSplitOptions.TrimEntries)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();


        public async Task<ServiceResult> UpdateBasicInfoAsync(string userId, UpdateBasicInfoDto dto)
        {
            if (dto == null)
                return ServiceResult.Failure("Invalid request");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return ServiceResult.Failure("User not found");

            var fullName = dto.FullName?.Trim();
            var currentPosition = dto.CurrentPosition?.Trim();
            var username = dto.Username?.Trim().ToLowerInvariant();
            var location = dto.Location?.Trim();
            var newEmail = dto.Email?.Trim();

            if (string.IsNullOrWhiteSpace(fullName))
                return ServiceResult.Failure("Full name is required");

            if (string.IsNullOrWhiteSpace(username))
                return ServiceResult.Failure("Username is required");

            if (username.Length < 3)
                return ServiceResult.Failure("Username must be at least 3 characters");

            if (username.Length > 30)
                return ServiceResult.Failure("Username can be maximum 30 characters");

            var usernameRegex = new System.Text.RegularExpressions.Regex(@"^(?![._])(?!.*[._]{2})[a-z0-9]+(?:[._][a-z0-9]+)*$");
            if (!usernameRegex.IsMatch(username))
                return ServiceResult.Failure("Username can only contain lowercase letters, numbers, dots and underscores");

            var isUsernameTaken = await _unitOfWork.Users.IsUsernameTakenAsync(username, userId);
            if (isUsernameTaken)
                return ServiceResult.Failure("Username is already taken");

            user.FullName = fullName;
            user.CurrentPosition = string.IsNullOrWhiteSpace(currentPosition) ? null : currentPosition;
            user.Location = string.IsNullOrWhiteSpace(location) ? null : location;

            if (!string.Equals(user.UserName, username, StringComparison.OrdinalIgnoreCase))
            {
                user.UserName = username;
                user.NormalizedUserName = username.ToUpper();
            }

            if (!string.IsNullOrWhiteSpace(newEmail) &&
                !string.Equals(user.Email, newEmail, StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(dto.CurrentPassword))
                    return ServiceResult.Failure("Password is required to change email");

                var passwordCorrect = await _userManager.CheckPasswordAsync(user, dto.CurrentPassword);
                if (!passwordCorrect)
                    return ServiceResult.Failure("Password is incorrect");

                var emailRegex = new System.Text.RegularExpressions.Regex(@"^[^\s@]+@[^\s@]+\.[^\s@]+$");
                if (!emailRegex.IsMatch(newEmail))
                    return ServiceResult.Failure("Email format is invalid");

                var isEmailTaken = await _unitOfWork.Users.IsEmailTakenAsync(newEmail, userId);
                if (isEmailTaken)
                    return ServiceResult.Failure("Email is already taken");

                user.Email = newEmail;
                user.NormalizedEmail = newEmail.ToUpper();
                user.EmailConfirmed = false;
            }

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return ServiceResult.Failure(errors);
            }

            await TrackCustomProfileOptionAsync(
                ProfileOptionType.Position,
                user.CurrentPosition,
                userId);
            await TrackCustomProfileOptionAsync(
                ProfileOptionType.Location,
                user.Location,
                userId);

            var response = new BasicInfoDto
            {
                FullName = user.FullName,
                CurrentPosition = user.CurrentPosition,
                ProfileImage = user.ProfileImage,
                BackgroundImage = user.BackgroundImage,
                Username = user.UserName,
                Location = user.Location
            };

            return ServiceResult.SuccessResult("Basic info updated successfully", new
            {
                basicInfo = response,
                email = user.Email
            });
        }

        public async Task<ServiceResult> UpdateContactInfoAsync(string userId, UpdateContactInfoDto dto)
        {
            if (dto == null)
                return ServiceResult.Failure("Invalid request");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return ServiceResult.Failure("User not found");

            var phone = dto.Phone?.Trim();
            var address = dto.Address?.Trim();
            var website = dto.Website?.Trim();

            if (!string.IsNullOrWhiteSpace(phone) && phone.Length > 30)
                return ServiceResult.Failure("Phone number can be maximum 30 characters");

            if (!string.IsNullOrWhiteSpace(address) && address.Length > 220)
                return ServiceResult.Failure("Address can be maximum 220 characters");

            if (!string.IsNullOrWhiteSpace(website) && website.Length > 300)
                return ServiceResult.Failure("Website can be maximum 300 characters");

            PhoneType? phoneType = null;
            if (!string.IsNullOrWhiteSpace(dto.PhoneType))
            {
                if (!Enum.TryParse<PhoneType>(dto.PhoneType, true, out var parsedPhoneType) ||
                    !Enum.IsDefined(typeof(PhoneType), parsedPhoneType))
                {
                    return ServiceResult.Failure("Phone type is invalid");
                }

                phoneType = parsedPhoneType;
            }

            if (dto.BirthMonth.HasValue &&
                (dto.BirthMonth.Value < 1 || dto.BirthMonth.Value > 12))
            {
                return ServiceResult.Failure("Birth month is invalid");
            }

            if (dto.BirthDay.HasValue)
            {
                if (!dto.BirthMonth.HasValue)
                    return ServiceResult.Failure("Birth month is required when birth day is selected");

                var maxDay = DateTime.DaysInMonth(2000, dto.BirthMonth.Value);
                if (dto.BirthDay.Value < 1 || dto.BirthDay.Value > maxDay)
                    return ServiceResult.Failure("Birth day is invalid for the selected month");
            }

            user.PhoneNumber = string.IsNullOrWhiteSpace(phone) ? null : phone;
            user.PhoneType = phoneType;
            user.Address = string.IsNullOrWhiteSpace(address) ? null : address;
            user.Website = string.IsNullOrWhiteSpace(website) ? null : website;
            user.BirthMonth = dto.BirthMonth;
            user.BirthDay = dto.BirthDay;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(x => x.Description));
                return ServiceResult.Failure(errors);
            }

            var response = new ContactInfoDto
            {
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                PhoneType = user.PhoneType,
                Address = user.Address,
                Website = user.Website,
                BirthMonth = user.BirthMonth,
                BirthDay = user.BirthDay
            };

            return ServiceResult.SuccessResult("Contact information updated successfully", response);
        }

        public async Task<ServiceResult> UpdateAboutAsync(string userId, UpdateAboutDto dto)
        {
            if (dto == null)
                return ServiceResult.Failure("Invalid request");

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return ServiceResult.Failure("User not found");

            var bio = dto.Bio?.Trim() ?? string.Empty;

            // Boş buraxmaq olarsa, istifadəçi bio-nu silə də biləcək.
            if (bio.Length > 1000)
                return ServiceResult.Failure("Bio can be maximum 1000 characters");

            user.Bio = string.IsNullOrWhiteSpace(bio)
                ? null
                : bio;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return ServiceResult.Failure(errors);
            }

            return ServiceResult.SuccessResult("About section updated successfully", new
            {
                bio = user.Bio
            });
        }


        public async Task<ServiceResult> UpdateProfileImageAsync(string userId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return ServiceResult.Failure("Profile image is required");

            var user = await _dbContext.Users
                .Include(item => item.Company)
                .FirstOrDefaultAsync(item => item.Id == userId);
            if (user == null)
                return ServiceResult.Failure("User not found");

            // Köhnə şəkli yadda saxlayırıq.
            // Yeni upload və database update uğurlu olandan sonra siləcəyik.
            var oldProfileImage = user.ProfileImage;
            var oldCompanyLogo = user.Company?.LogoUrl;

            var uploadedPath = await _uploadImage.UploadFile(file, "profile");

            if (string.IsNullOrWhiteSpace(uploadedPath))
                return ServiceResult.Failure("Image upload failed");

            user.ProfileImage = uploadedPath;
            if (user.UserType == UserType.Employer && user.Company != null)
                user.Company.LogoUrl = uploadedPath;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                // Database update alınmadısa, boşuna Cloudinary-yə yüklənmiş
                // yeni şəkli silirik.
                await _uploadImage.DeletePhysicalFileIfExists(uploadedPath);

                var errors = string.Join(", ", result.Errors.Select(x => x.Description));
                return ServiceResult.Failure(errors);
            }

            // Yeni şəkil artıq database-dədir. İndi köhnəni silmək təhlükəsizdir.
            if (!string.IsNullOrWhiteSpace(oldProfileImage))
            {
                await _uploadImage.DeletePhysicalFileIfExists(oldProfileImage);
            }

            if (!string.IsNullOrWhiteSpace(oldCompanyLogo) &&
                !string.Equals(
                    oldCompanyLogo,
                    oldProfileImage,
                    StringComparison.OrdinalIgnoreCase))
            {
                await _uploadImage.DeletePhysicalFileIfExists(oldCompanyLogo);
            }

            return ServiceResult.SuccessResult("Profile image updated successfully", new
            {
                profileImage = user.ProfileImage,
                logoUrl = user.Company?.LogoUrl
            });
        }


        public async Task<ServiceResult> DeleteProfileImageAsync(string userId)
        {
            var user = await _dbContext.Users
                .Include(item => item.Company)
                .FirstOrDefaultAsync(item => item.Id == userId);
            if (user == null)
                return ServiceResult.Failure("User not found");

            var oldProfileImage = user.ProfileImage;
            var oldCompanyLogo = user.Company?.LogoUrl;

            user.ProfileImage = null;
            if (user.UserType == UserType.Employer && user.Company != null)
                user.Company.LogoUrl = null;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(x => x.Description));
                return ServiceResult.Failure(errors);
            }

            if (!string.IsNullOrWhiteSpace(oldProfileImage))
                await _uploadImage.DeletePhysicalFileIfExists(oldProfileImage);

            if (!string.IsNullOrWhiteSpace(oldCompanyLogo) &&
                !string.Equals(
                    oldCompanyLogo,
                    oldProfileImage,
                    StringComparison.OrdinalIgnoreCase))
            {
                await _uploadImage.DeletePhysicalFileIfExists(oldCompanyLogo);
            }

            return ServiceResult.SuccessResult("Profile image deleted successfully", new
            {
                profileImage = (string?)null,
                logoUrl = (string?)null
            });
        }


        public async Task<ServiceResult> UpdateBackgroundImageAsync(string userId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return ServiceResult.Failure("Background image is required");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return ServiceResult.Failure("User not found");

            var oldBackgroundImage = user.BackgroundImage;

            var uploadedPath = await _uploadImage.UploadFile(file, "background");

            if (string.IsNullOrWhiteSpace(uploadedPath))
                return ServiceResult.Failure("Image upload failed");

            user.BackgroundImage = uploadedPath;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                await _uploadImage.DeletePhysicalFileIfExists(uploadedPath);

                var errors = string.Join(", ", result.Errors.Select(x => x.Description));
                return ServiceResult.Failure(errors);
            }

            if (!string.IsNullOrWhiteSpace(oldBackgroundImage))
            {
                await _uploadImage.DeletePhysicalFileIfExists(oldBackgroundImage);
            }

            return ServiceResult.SuccessResult("Background image updated successfully", new
            {
                backgroundImage = user.BackgroundImage
            });
        }


        public async Task<ServiceResult> DeleteBackgroundImageAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return ServiceResult.Failure("User not found");

            if (!string.IsNullOrWhiteSpace(user.BackgroundImage))
                await _uploadImage.DeletePhysicalFileIfExists(user.BackgroundImage);

            user.BackgroundImage = null;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(x => x.Description));
                return ServiceResult.Failure(errors);
            }

            return ServiceResult.SuccessResult("Background image deleted successfully", new
            {
                backgroundImage = (string?)null
            });
        }


        public async Task<ServiceResult> AddExperienceAsync(string userId, CreateExperienceDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
                return ServiceResult.Failure("Title is required");

            if (string.IsNullOrWhiteSpace(dto.CompanyName))
                return ServiceResult.Failure("Company name is required");

            var company = await FindOrganizationAsync(dto.CompanyId);
            if (dto.CompanyId.HasValue && company == null)
                return ServiceResult.Failure("The selected company was not found");

            var experience = new Experience
            {
                Title = dto.Title.Trim(),
                EmploymentType = dto.EmploymentType,
                CompanyName = company?.Name ?? dto.CompanyName.Trim(),
                CompanyId = company?.Id,
                IsCurrent = dto.IsCurrent,
                StartMonth = dto.StartMonth,
                StartYear = dto.StartYear,
                EndMonth = dto.IsCurrent ? null : dto.EndMonth,
                EndYear = dto.IsCurrent ? null : dto.EndYear,
                Location = dto.Location,
                LocationType = dto.LocationType,
                Description = dto.Description,
                UserId = userId
            };

            await _unitOfWork.Experiences.AddAsync(experience);
            await _unitOfWork.CompleteAsync();
            await TrackCustomProfileOptionAsync(ProfileOptionType.Position, experience.Title, userId);
            await TrackCustomProfileOptionAsync(ProfileOptionType.Location, experience.Location, userId);

            var responseDto = new ExperienceDto
            {
                Id = experience.Id,
                Title = experience.Title,
                EmploymentType = experience.EmploymentType,
                CompanyName = experience.CompanyName,
                CompanyId = experience.CompanyId,
                CompanyLogoUrl = company?.LogoUrl ?? company?.User?.ProfileImage,
                CompanyUsername = company?.User?.UserName,
                IsCurrent = experience.IsCurrent,
                StartMonth = experience.StartMonth,
                StartYear = experience.StartYear,
                EndMonth = experience.EndMonth,
                EndYear = experience.EndYear,
                Location = experience.Location,
                LocationType = experience.LocationType,
                Description = experience.Description
            };

            return ServiceResult.SuccessResult("Experience added successfully", responseDto);
        }

        public async Task<ServiceResult> UpdateExperienceAsync(string userId, int experienceId, UpdateExperienceDto dto)
        {
            var experience = await _unitOfWork.Experiences.GetByIdAsync(experienceId);

            if (experience == null)
                return ServiceResult.Failure("Experience not found");

            if (experience.UserId != userId)
                return ServiceResult.Failure("Unauthorized");

            var company = await FindOrganizationAsync(dto.CompanyId);
            if (dto.CompanyId.HasValue && company == null)
                return ServiceResult.Failure("The selected company was not found");

            if (dto.Title != null)
                experience.Title = dto.Title;

            if (dto.EmploymentType != null)
                experience.EmploymentType = dto.EmploymentType;

            if (dto.CompanyName != null)
                experience.CompanyName = company?.Name ?? dto.CompanyName.Trim();

            experience.CompanyId = company?.Id;

            if (dto.IsCurrent.HasValue)
                experience.IsCurrent = dto.IsCurrent.Value;

            if (dto.StartMonth.HasValue)
                experience.StartMonth = dto.StartMonth;

            if (dto.StartYear.HasValue)
                experience.StartYear = dto.StartYear;

            if (dto.Location != null)
                experience.Location = dto.Location;

            if (dto.LocationType != null)
                experience.LocationType = dto.LocationType;

            if (dto.Description != null)
                experience.Description = dto.Description;

            // 🔥 Current logic
            if (experience.IsCurrent)
            {
                experience.EndMonth = null;
                experience.EndYear = null;
            }
            else
            {
                if (dto.EndMonth.HasValue)
                    experience.EndMonth = dto.EndMonth;

                if (dto.EndYear.HasValue)
                    experience.EndYear = dto.EndYear;
            }

            await _unitOfWork.CompleteAsync();
            await TrackCustomProfileOptionAsync(ProfileOptionType.Position, experience.Title, userId);
            await TrackCustomProfileOptionAsync(ProfileOptionType.Location, experience.Location, userId);

            var returnDto = new ExperienceDto
            {
                Id = experience.Id,
                Title = experience.Title,
                EmploymentType = experience.EmploymentType,
                CompanyName = experience.CompanyName,
                CompanyId = experience.CompanyId,
                CompanyLogoUrl = company?.LogoUrl ?? company?.User?.ProfileImage,
                CompanyUsername = company?.User?.UserName,
                IsCurrent = experience.IsCurrent,
                StartMonth = experience.StartMonth,
                StartYear = experience.StartYear,
                EndMonth = experience.EndMonth,
                EndYear = experience.EndYear,
                Location = experience.Location,
                LocationType = experience.LocationType,
                Description = experience.Description
            };

            return ServiceResult.SuccessResult("Experience updated successfully", returnDto);
        }

        public async Task<ServiceResult> DeleteExperienceAsync(string userId, int experienceId)
        {
            var experience = await _unitOfWork.Experiences.GetByIdAsync(experienceId);

            if (experience == null)
                return ServiceResult.Failure("Experience not found");

            if (experience.UserId != userId)
                return ServiceResult.Failure("Unauthorized");

            _unitOfWork.Experiences.Remove(experience);
            await _unitOfWork.CompleteAsync();

            return ServiceResult.SuccessResult("Experience deleted successfully");
        }

        public async Task<ServiceResult> AddEducationAsync(string userId, CreateEducationDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.School))
                return ServiceResult.Failure("School is required");

            var institution = await FindOrganizationAsync(dto.InstitutionCompanyId);
            if (dto.InstitutionCompanyId.HasValue && institution == null)
                return ServiceResult.Failure("The selected institution was not found");

            var education = new Education
            {
                School = institution?.Name ?? dto.School.Trim(),
                InstitutionCompanyId = institution?.Id,
                Degree = dto.Degree,
                Field = dto.Field,
                StartMonth = dto.StartMonth,
                StartYear = dto.StartYear,
                EndMonth = dto.EndMonth,
                EndYear = dto.EndYear,
                Note = dto.Note,
                UserId = userId
            };

            await _unitOfWork.Educations.AddAsync(education);
            await _unitOfWork.CompleteAsync();

            var responseDto = new EducationDto
            {
                Id = education.Id,
                School = education.School,
                InstitutionCompanyId = education.InstitutionCompanyId,
                InstitutionLogoUrl = institution?.LogoUrl ?? institution?.User?.ProfileImage,
                InstitutionUsername = institution?.User?.UserName,
                Degree = education.Degree,
                Field = education.Field,
                StartMonth = education.StartMonth,
                StartYear = education.StartYear,
                EndMonth = education.EndMonth,
                EndYear = education.EndYear,
                Note = education.Note
            };

            return ServiceResult.SuccessResult("Education added successfully", responseDto);
        }




        public async Task<ServiceResult> UpdateEducationAsync(string userId, int educationId, UpdateEducationDto dto)
        {
            var education = await _unitOfWork.Educations.GetByIdAsync(educationId);

            if (education == null)
                return ServiceResult.Failure("Education not found");

            if (education.UserId != userId)
                return ServiceResult.Failure("Unauthorized");

            var institution = await FindOrganizationAsync(dto.InstitutionCompanyId);
            if (dto.InstitutionCompanyId.HasValue && institution == null)
                return ServiceResult.Failure("The selected institution was not found");

            if (dto.School != null)
                education.School = institution?.Name ?? dto.School.Trim();

            education.InstitutionCompanyId = institution?.Id;

            if (dto.Degree != null)
                education.Degree = dto.Degree;

            if (dto.Field != null)
                education.Field = dto.Field;

            if (dto.StartMonth.HasValue)
                education.StartMonth = dto.StartMonth;

            if (dto.StartYear.HasValue)
                education.StartYear = dto.StartYear;

            // The education form sends both fields on every save. Assigning them
            // directly lets a user clear the end date when their education is ongoing.
            education.EndMonth = dto.EndMonth;
            education.EndYear = dto.EndYear;

            if (dto.Note != null)
                education.Note = dto.Note;

            await _unitOfWork.CompleteAsync();

            var responseDto = new EducationDto
            {
                Id = education.Id,
                School = education.School,
                InstitutionCompanyId = education.InstitutionCompanyId,
                InstitutionLogoUrl = institution?.LogoUrl ?? institution?.User?.ProfileImage,
                InstitutionUsername = institution?.User?.UserName,
                Degree = education.Degree,
                Field = education.Field,
                StartMonth = education.StartMonth,
                StartYear = education.StartYear,
                EndMonth = education.EndMonth,
                EndYear = education.EndYear,
                Note = education.Note
            };

            return ServiceResult.SuccessResult("Education updated successfully", responseDto);
        }

        public async Task<ServiceResult> DeleteEducationAsync(string userId, int educationId)
        {
            var education = await _unitOfWork.Educations.GetByIdAsync(educationId);

            if (education == null)
                return ServiceResult.Failure("Education not found");

            if (education.UserId != userId)
                return ServiceResult.Failure("Unauthorized");

            var responseDto = new EducationDto
            {
                Id = education.Id,
                School = education.School,
                Degree = education.Degree,
                Field = education.Field,
                StartMonth = education.StartMonth,
                StartYear = education.StartYear,
                EndMonth = education.EndMonth,
                EndYear = education.EndYear,
                Note = education.Note
            };

            _unitOfWork.Educations.Remove(education);
            await _unitOfWork.CompleteAsync();

            return ServiceResult.SuccessResult("Education deleted successfully", responseDto);
        }


        //Skills

        public async Task<ServiceResult> AddSkillAsync(string userId, CreateSkillDto dto)
        {
            if (dto == null)
                return ServiceResult.Failure("Invalid request");

            var skillName = dto.Name?.Trim();

            if (string.IsNullOrWhiteSpace(skillName))
                return ServiceResult.Failure("Skill name is required");

            var existingSkill = await _unitOfWork.Skills.GetUserSkillByNameAsync(userId, skillName);

            if (existingSkill != null)
                return ServiceResult.Failure("This skill already exists");

            var skill = new UserSkill
            {
                Name = skillName,
                UserId = userId
            };

            await _unitOfWork.Skills.AddAsync(skill);
            await _unitOfWork.CompleteAsync();
            await TrackCustomProfileOptionAsync(ProfileOptionType.Skill, skill.Name, userId);

            var responseDto = new SkillDto
            {
                Id = skill.Id,
                Name = skill.Name
            };

            return ServiceResult.SuccessResult("Skill added successfully", responseDto);
        }

        public async Task<ServiceResult> UpdateSkillAsync(string userId, int skillId, UpdateSkillDto dto)
        {
            if (dto == null)
                return ServiceResult.Failure("Invalid request");

            var skill = await _unitOfWork.Skills.GetByIdAsync(skillId);

            if (skill == null)
                return ServiceResult.Failure("Skill not found");

            if (skill.UserId != userId)
                return ServiceResult.Failure("Unauthorized");

            var skillName = dto.Name?.Trim();

            if (string.IsNullOrWhiteSpace(skillName))
                return ServiceResult.Failure("Skill name is required");

            var duplicateSkill = await _unitOfWork.Skills.GetUserSkillByNameAsync(userId, skillName);

            if (duplicateSkill != null && duplicateSkill.Id != skillId)
                return ServiceResult.Failure("This skill already exists");

            skill.Name = skillName;

            await _unitOfWork.CompleteAsync();
            await TrackCustomProfileOptionAsync(ProfileOptionType.Skill, skill.Name, userId);

            var responseDto = new SkillDto
            {
                Id = skill.Id,
                Name = skill.Name
            };

            return ServiceResult.SuccessResult("Skill updated successfully", responseDto);
        }

        public async Task<ServiceResult> DeleteSkillAsync(string userId, int skillId)
        {
            var skill = await _unitOfWork.Skills.GetByIdAsync(skillId);

            if (skill == null)
                return ServiceResult.Failure("Skill not found");

            if (skill.UserId != userId)
                return ServiceResult.Failure("Unauthorized");

            var responseDto = new SkillDto
            {
                Id = skill.Id,
                Name = skill.Name
            };

            _unitOfWork.Skills.Remove(skill);
            await _unitOfWork.CompleteAsync();

            return ServiceResult.SuccessResult("Skill deleted successfully", responseDto);
        }



        public async Task<ServiceResult> AddSkillsAsync(string userId, BulkCreateSkillDto dto)
        {
            if (dto == null || dto.Skills == null || !dto.Skills.Any())
                return ServiceResult.Failure("At least one skill is required");

            var existingSkills = await _unitOfWork.Skills.GetUserSkillsAsync(userId);
            var existingSkillNames = existingSkills
                .Select(x => x.Name.Trim().ToLower())
                .ToHashSet();

            var addedSkills = new List<UserSkill>();
            var seenNames = new HashSet<string>();

            foreach (var item in dto.Skills)
            {
                var skillName = item?.Name?.Trim();

                if (string.IsNullOrWhiteSpace(skillName))
                    continue;

                var lowerName = skillName.ToLower();

                if (existingSkillNames.Contains(lowerName))
                    continue;

                if (seenNames.Contains(lowerName))
                    continue;

                var skill = new UserSkill
                {
                    Name = skillName,
                    UserId = userId
                };

                addedSkills.Add(skill);
                seenNames.Add(lowerName);
            }

            if (!addedSkills.Any())
                return ServiceResult.Failure("No new valid skills to add");

            foreach (var skill in addedSkills)
            {
                await _unitOfWork.Skills.AddAsync(skill);
            }

            await _unitOfWork.CompleteAsync();

            foreach (var skill in addedSkills)
            {
                await TrackCustomProfileOptionAsync(
                    ProfileOptionType.Skill,
                    skill.Name,
                    userId);
            }

            var response = addedSkills.Select(skill => new SkillDto
            {
                Id = skill.Id,
                Name = skill.Name
            }).ToList();

            return ServiceResult.SuccessResult("Skills added successfully", response);
        }

        public async Task<UserLookupDto?> GetUserEntityByUsernameAsync(string username)
        {
            var user = await _unitOfWork.Users.GetUserByUsernameAsync(username);

            return user;
        }




        // Employer CRUD
        public async Task<ServiceResult> UpdateEmployerCompanyInfoAsync(
        string userId,
        UpdateEmployerCompanyInfoDto dto)
        {
            if (dto == null)
                return ServiceResult.Failure("Invalid request");

            var user = await _userManager.Users
                .Include(u => u.Company)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return ServiceResult.Failure("User not found");

            if (user.UserType != UserType.Employer)
                return ServiceResult.Failure("Only employer accounts can update company information");

            var name = dto.Name?.Trim();
            var username = dto.Username?.Trim().ToLowerInvariant();
            var tagline = dto.Tagline?.Trim();
            var industry = dto.Industry?.Trim();
            var location = dto.Location?.Trim();
            var bio = dto.Bio?.Trim();
            var companySize = dto.CompanySize?.Trim();
            var foundedYear = dto.FoundedYear;

            if (!string.IsNullOrWhiteSpace(companySize) && companySize.Length > 50)
                return ServiceResult.Failure("Company size can be maximum 50 characters");

            if (foundedYear.HasValue)
            {
                var currentYear = DateTime.UtcNow.Year;

                if (foundedYear.Value < 1800 || foundedYear.Value > currentYear)
                    return ServiceResult.Failure($"Founded year must be between 1800 and {currentYear}");
            }
            if (!string.IsNullOrWhiteSpace(tagline) && tagline.Length > 120)
                return ServiceResult.Failure("Tagline can be maximum 120 characters");

            if (!string.IsNullOrWhiteSpace(industry))
            {
                var normalizedIndustry = industry.ToUpperInvariant();
                var approvedIndustry = await _dbContext.ProfileOptions
                    .AsNoTracking()
                    .AnyAsync(option =>
                        option.Type == ProfileOptionType.Industry &&
                        option.IsApproved &&
                        option.NormalizedName == normalizedIndustry);

                if (!approvedIndustry)
                    return ServiceResult.Failure("Select an industry from the official list");
            }

            if (string.IsNullOrWhiteSpace(name))
                return ServiceResult.Failure("Company name is required");

            if (name.Length > 150)
                return ServiceResult.Failure("Company name can be maximum 150 characters");

            if (string.IsNullOrWhiteSpace(username))
                return ServiceResult.Failure("Username is required");

            if (username.Length < 3)
                return ServiceResult.Failure("Username must be at least 3 characters");

            if (username.Length > 30)
                return ServiceResult.Failure("Username can be maximum 30 characters");

            var usernameRegex = new System.Text.RegularExpressions.Regex(@"^(?![._])(?!.*[._]{2})[a-z0-9]+(?:[._][a-z0-9]+)*$");
            if (!usernameRegex.IsMatch(username))
                return ServiceResult.Failure("Username can only contain lowercase letters, numbers, dots and underscores");

            var isUsernameTaken = await _unitOfWork.Users.IsUsernameTakenAsync(username, userId);
            if (isUsernameTaken)
                return ServiceResult.Failure("Username is already taken");

            if (user.Company == null)
            {
                user.Company = new Company
                {
                    UserId = user.Id,
                    IsVerified = false
                };
            }
            user.Company.CompanySize = string.IsNullOrWhiteSpace(companySize) ? null : companySize;
            user.Company.FoundedYear = foundedYear;

            user.FullName = name;
            user.Location = string.IsNullOrWhiteSpace(location) ? null : location;
            user.Bio = string.IsNullOrWhiteSpace(bio) ? null : bio;

            if (!string.Equals(user.UserName, username, StringComparison.OrdinalIgnoreCase))
            {
                user.UserName = username;
                user.NormalizedUserName = username.ToUpper();
            }

            user.Company.Name = name;
            user.Company.Tagline = string.IsNullOrWhiteSpace(tagline) ? null : tagline;
            user.Company.Industry = string.IsNullOrWhiteSpace(industry) ? null : industry;
            user.Company.Location = string.IsNullOrWhiteSpace(location) ? null : location;
            user.Company.Bio = string.IsNullOrWhiteSpace(bio) ? null : bio;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return ServiceResult.Failure(errors);
            }

            await TrackCustomProfileOptionAsync(
                ProfileOptionType.Location,
                user.Company.Location,
                userId);

            return ServiceResult.SuccessResult("Company information updated successfully", new
            {
                basicInfo = new
                {
                    id = user.Id,
                    fullName = user.FullName,
                    username = user.UserName,
                    currentPosition = user.CurrentPosition,
                    profileImage = user.ProfileImage,
                    backgroundImage = user.BackgroundImage,
                    location = user.Location
                },
                about = new
                {
                    bio = user.Bio
                },
                companyInfo = new
                {
                    name = user.Company.Name,
                    industry = user.Company.Industry,
                    bio = user.Company.Bio,
                    website = user.Company.Website,
                    location = user.Company.Location,
                    logoUrl = user.Company.LogoUrl,
                    isVerified = user.Company.IsVerified,
                    companySize = user.Company.CompanySize,
                    foundedYear = user.Company.FoundedYear,
                    tagline = user.Company.Tagline,
                }
            });
        }

        public async Task<ServiceResult> UpdateEmployerContactInfoAsync(
            string userId,
            UpdateEmployerContactInfoDto dto)
        {
            if (dto == null)
                return ServiceResult.Failure("Invalid request");

            var user = await _userManager.Users
                .Include(u => u.Company)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return ServiceResult.Failure("User not found");

            if (user.UserType != UserType.Employer)
                return ServiceResult.Failure("Only employer accounts can update contact information");

            var website = dto.Website?.Trim();
            var email = dto.Email?.Trim();
            var phoneNumber = dto.PhoneNumber?.Trim();

            if (!string.IsNullOrWhiteSpace(website) && website.Length > 300)
                return ServiceResult.Failure("Website can be maximum 300 characters");

            if (!string.IsNullOrWhiteSpace(phoneNumber) && phoneNumber.Length > 30)
                return ServiceResult.Failure("Phone number can be maximum 30 characters");

            if (dto.ChangeEmail)
            {
                if (string.IsNullOrWhiteSpace(email))
                    return ServiceResult.Failure("Email is required");

                var emailRegex = new System.Text.RegularExpressions.Regex(@"^[^\s@]+@[^\s@]+\.[^\s@]+$");

                if (!emailRegex.IsMatch(email))
                    return ServiceResult.Failure("Email format is invalid");

                if (string.IsNullOrWhiteSpace(dto.CurrentPassword))
                    return ServiceResult.Failure("Password is required to change email");

                var passwordCorrect = await _userManager.CheckPasswordAsync(user, dto.CurrentPassword);

                if (!passwordCorrect)
                    return ServiceResult.Failure("Password is incorrect");

                if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
                {
                    var isEmailTaken = await _unitOfWork.Users.IsEmailTakenAsync(email, userId);

                    if (isEmailTaken)
                        return ServiceResult.Failure("Email is already taken");

                    user.Email = email;
                    user.NormalizedEmail = email.ToUpper();
                    user.EmailConfirmed = false;
                }
            }

            if (user.Company == null)
            {
                user.Company = new Company
                {
                    UserId = user.Id,
                    Name = user.FullName ?? user.UserName!,
                    IsVerified = false
                };
            }

            user.Website = string.IsNullOrWhiteSpace(website) ? null : website;
            user.PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber;

            user.Company.Website = string.IsNullOrWhiteSpace(website) ? null : website;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return ServiceResult.Failure(errors);
            }

            return ServiceResult.SuccessResult("Company contact information updated successfully", new
            {
                contactInfo = new
                {
                    email = user.Email,
                    phoneNumber = user.PhoneNumber,
                    website = user.Website,
                    address = user.Address,
                    birthDay = user.BirthDay,
                    birthMonth = user.BirthMonth,
                    phoneType = user.PhoneType
                },
                companyInfo = new
                {
                    name = user.Company.Name,
                    industry = user.Company.Industry,
                    bio = user.Company.Bio,
                    website = user.Company.Website,
                    location = user.Company.Location,
                    logoUrl = user.Company.LogoUrl,
                    isVerified = user.Company.IsVerified
                }
            });
        }


        public async Task<PagedResultDto<SearchedUserDto>> GetEmployersPagedAsync(
        string currentUserId,
        int pageNumber,
        int pageSize)
        {
            if (pageNumber < 1)
                pageNumber = 1;

            if (pageSize < 1)
                pageSize = 10;

            return await _unitOfWork.Users.GetEmployersPagedAsync(
                currentUserId,
                pageNumber,
                pageSize
            );
        }

        public async Task<PagedResultDto<SearchedUserDto>> GetJobSeekersPagedAsync(
            string currentUserId,
            int pageNumber,
            int pageSize)
        {
            if (pageNumber < 1)
                pageNumber = 1;

            if (pageSize < 1)
                pageSize = 10;

            return await _unitOfWork.Users.GetJobSeekersPagedAsync(
                currentUserId,
                pageNumber,
                pageSize
            );
        }


        public async Task<ServiceResult> GetRecommendedUsersAsync(
            string currentUserId,
            int pageNumber,
            int pageSize)
        {
            if (string.IsNullOrWhiteSpace(currentUserId))
                return ServiceResult.Failure("User not found");

            if (pageNumber < 1)
                pageNumber = 1;

            if (pageSize < 1)
                pageSize = 10;

            if (pageSize > 50)
                pageSize = 50;

            var result = await _unitOfWork.Users.GetRecommendedUsersAsync(
                currentUserId,
                pageNumber,
                pageSize);

            return ServiceResult.SuccessResult(
                "Recommended users loaded successfully",
                result);
        }


        public async Task<ServiceResult> GetSearchHistoryAsync(
            string userId,
            int take)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return ServiceResult.Failure("User not found");

            var result = await _unitOfWork.Users.GetSearchHistoryAsync(
                userId,
                take);

            return ServiceResult.SuccessResult(
                "Search history loaded successfully",
                result);
        }

        public async Task<ServiceResult> HideSearchHistoryAsync(
            string userId,
            int historyId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return ServiceResult.Failure("User not found");

            var found = await _unitOfWork.Users
                .HideSearchHistoryAsync(userId, historyId);

            if (!found)
                return ServiceResult.Failure("Search history item not found");

            await _unitOfWork.CompleteAsync();

            return ServiceResult.SuccessResult(
                "Search history item hidden successfully",
                historyId);
        }

        public async Task<ServiceResult> HideAllSearchHistoryAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return ServiceResult.Failure("User not found");

            var count = await _unitOfWork.Users
                .HideAllSearchHistoryAsync(userId);

            if (count > 0)
                await _unitOfWork.CompleteAsync();

            return ServiceResult.SuccessResult(
                "Search history hidden successfully",
                count);
        }
    }




}
