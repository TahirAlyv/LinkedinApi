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

        public UserService(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor, UserManager<ApplicationUser> userManager, IUploadImage uploadImage)
        {
            _unitOfWork = unitOfWork;
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
            _uploadImage = uploadImage;
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

            return profile;
        }


        public async Task<ServiceResult> UpdateBasicInfoAsync(string userId, UpdateBasicInfoDto dto)
        {
            if (dto == null)
                return ServiceResult.Failure("Invalid request");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return ServiceResult.Failure("User not found");

            var fullName = dto.FullName?.Trim();
            var currentPosition = dto.CurrentPosition?.Trim();
            var username = dto.Username?.Trim();
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

            var usernameRegex = new System.Text.RegularExpressions.Regex(@"^[a-zA-Z0-9._]+$");
            if (!usernameRegex.IsMatch(username))
                return ServiceResult.Failure("Username can only contain letters, numbers, dots and underscores");

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

        public async Task<ServiceResult> UpdateProfileImageAsync(string userId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return ServiceResult.Failure("Profile image is required");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return ServiceResult.Failure("User not found");

            if (!string.IsNullOrWhiteSpace(user.ProfileImage))
                await _uploadImage.DeletePhysicalFileIfExists(user.ProfileImage);

            var uploadedPath = await _uploadImage.UploadFile(file, "profile");

            if (string.IsNullOrWhiteSpace(uploadedPath))
                return ServiceResult.Failure("Image upload failed");

            user.ProfileImage = uploadedPath;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(x => x.Description));
                return ServiceResult.Failure(errors);
            }

            return ServiceResult.SuccessResult("Profile image updated successfully", new
            {
                profileImage = user.ProfileImage
            });
        }


        public async Task<ServiceResult> DeleteProfileImageAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return ServiceResult.Failure("User not found");

            if (!string.IsNullOrWhiteSpace(user.ProfileImage))
                await _uploadImage.DeletePhysicalFileIfExists(user.ProfileImage);

            user.ProfileImage = null;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(x => x.Description));
                return ServiceResult.Failure(errors);
            }

            return ServiceResult.SuccessResult("Profile image deleted successfully", new
            {
                profileImage = (string?)null
            });
        }


        public async Task<ServiceResult> UpdateBackgroundImageAsync(string userId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return ServiceResult.Failure("Background image is required");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return ServiceResult.Failure("User not found");

            if (!string.IsNullOrWhiteSpace(user.BackgroundImage))
                await _uploadImage.DeletePhysicalFileIfExists(user.BackgroundImage);

            var uploadedPath = await _uploadImage.UploadFile(file, "background");

            if (string.IsNullOrWhiteSpace(uploadedPath))
                return ServiceResult.Failure("Image upload failed");

            user.BackgroundImage = uploadedPath;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(x => x.Description));
                return ServiceResult.Failure(errors);
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

            var experience = new Experience
            {
                Title = dto.Title,
                EmploymentType = dto.EmploymentType,
                CompanyName = dto.CompanyName,
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

            var responseDto = new ExperienceDto
            {
                Id = experience.Id,
                Title = experience.Title,
                EmploymentType = experience.EmploymentType,
                CompanyName = experience.CompanyName,
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

            if (dto.Title != null)
                experience.Title = dto.Title;

            if (dto.EmploymentType != null)
                experience.EmploymentType = dto.EmploymentType;

            if (dto.CompanyName != null)
                experience.CompanyName = dto.CompanyName;

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

            var returnDto = new UpdateExperienceDto
            {
                Title = experience.Title,
                EmploymentType = experience.EmploymentType,
                CompanyName = experience.CompanyName,
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

            var education = new Education
            {
                School = dto.School,
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

            if (dto.School != null)
                education.School = dto.School;

            if (dto.Degree != null)
                education.Degree = dto.Degree;

            if (dto.Field != null)
                education.Field = dto.Field;

            if (dto.StartMonth.HasValue)
                education.StartMonth = dto.StartMonth;

            if (dto.StartYear.HasValue)
                education.StartYear = dto.StartYear;

            if (dto.EndMonth.HasValue)
                education.EndMonth = dto.EndMonth;

            if (dto.EndYear.HasValue)
                education.EndYear = dto.EndYear;

            if (dto.Note != null)
                education.Note = dto.Note;

            await _unitOfWork.CompleteAsync();

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
            var username = dto.Username?.Trim();
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

            var usernameRegex = new System.Text.RegularExpressions.Regex(@"^[a-zA-Z0-9._]+$");
            if (!usernameRegex.IsMatch(username))
                return ServiceResult.Failure("Username can only contain letters, numbers, dots and underscores");

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
    }




}
