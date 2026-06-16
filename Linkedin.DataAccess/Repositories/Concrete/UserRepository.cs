using Linkedin.Core.Data;
using Linkedin.Core.Dtos;
using Linkedin.Core.Dtos.Pagination;
using Linkedin.Core.Dtos.Profile.Read;
using Linkedin.Core.Entities;
using Linkedin.DataAccess.Repositories.Interfaces;
 
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.DataAccess.Repositories.Concrete
{
    public class UserRepository : Repository<ApplicationUser>, IUserRepository
    {

        private readonly UserManager<ApplicationUser> _userManager;
        public UserRepository(AppDbContext context) : base(context) { }

        public async Task<List<SearchedUserDto>> GetSearchUsers(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<SearchedUserDto>();

            var search = query.Trim().ToLower();

            var adminRoleIds = _context.Roles
                .Where(r => r.Name == "Admin")
                .Select(r => r.Id);

            var adminUserIds = _context.UserRoles
                .Where(ur => adminRoleIds.Contains(ur.RoleId))
                .Select(ur => ur.UserId);

            return await _context.Users
                .AsNoTracking()
                .Include(u => u.Company)
                .Where(u =>
                    !u.IsBlocked &&
                    !adminUserIds.Contains(u.Id) &&
                    (
                        (u.UserName != null && u.UserName.ToLower().Contains(search)) ||
                        (u.FullName != null && u.FullName.ToLower().Contains(search)) ||
                        (u.CurrentPosition != null && u.CurrentPosition.ToLower().Contains(search)) ||
                        (u.Location != null && u.Location.ToLower().Contains(search)) ||
                        (u.Bio != null && u.Bio.ToLower().Contains(search)) ||
                        (u.Company != null && u.Company.Name != null && u.Company.Name.ToLower().Contains(search)) ||
                        (u.Company != null && u.Company.Industry != null && u.Company.Industry.ToLower().Contains(search))
                    )
                )
                .OrderBy(u => u.UserName)
                .Select(u => new SearchedUserDto
                {
                    Id = u.Id,
                    Username = u.UserName,

                    FullName = u.UserType == Linkedin.Core.Enums.UserType.Employer && u.Company != null
                        ? u.Company.Name
                        : u.FullName,

                    CurrentPosition = u.UserType == Linkedin.Core.Enums.UserType.Employer && u.Company != null
                        ? u.Company.Industry
                        : u.CurrentPosition,

                    ProfileImage = u.UserType == Linkedin.Core.Enums.UserType.Employer && u.Company != null && u.Company.LogoUrl != null
                        ? u.Company.LogoUrl
                        : u.ProfileImage,

                    Bio = u.UserType == Linkedin.Core.Enums.UserType.Employer && u.Company != null
                        ? u.Company.Bio
                        : u.Bio,

                    Location = u.UserType == Linkedin.Core.Enums.UserType.Employer && u.Company != null && u.Company.Location != null
                        ? u.Company.Location
                        : u.Location,

                    Visibility = u.Visibility.ToString(),
                    UserType = u.UserType.ToString(),

                    CompanyName = u.Company != null ? u.Company.Name : null,
                    CompanyLogo = u.Company != null ? u.Company.LogoUrl : null,
                    CompanyIndustry = u.Company != null ? u.Company.Industry : null
                })
                .Take(20)
                .ToListAsync();
        }

        public async Task<ApplicationUser> GetUserByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<ApplicationUser> GetUserByUsername(string username)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.UserName == username);
        }

        public async Task<ApplicationUser> GetUserWithPostsAsync(string userId)
        {
            return await _context.Users.Include(u => u.Posts).FirstOrDefaultAsync(u => u.Id == userId);
        }

      

        public IQueryable<ApplicationUser> GetQuery()
        {
            return _context.Users.AsQueryable();
        }


        public async Task<ProfileDetailsDto?> GetMyProfileDetailsAsync(string userId, string currentUserRole)
        {
            return await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new ProfileDetailsDto
                {
                    UserType = u.UserType.ToString(),
                    Role = currentUserRole,
                    BasicInfo = new BasicInfoDto
                    {
                        Id = u.Id,
                        FullName = u.FullName,
                        Username = u.UserName,
                        CurrentPosition = u.CurrentPosition,
                        ProfileImage = u.ProfileImage,
                        BackgroundImage = u.BackgroundImage,
                        Location = u.Location
                    },

                    ContactInfo = new ContactInfoDto
                    {
                        Email = u.Email,
                        PhoneNumber = u.PhoneNumber,
                        Website = u.Website,
                        Address = u.Address,
                        BirthDay = u.BirthDay,
                        BirthMonth = u.BirthMonth,
                        PhoneType = u.PhoneType
                    },

                    About = new AboutDto
                    {
                        Bio = u.Bio
                    },

                    CompanyInfo = u.Company == null ? null : new CompanyInfoDto
                    {
                        Name = u.Company.Name,
                        Industry = u.Company.Industry,
                        Bio = u.Company.Bio,
                        Website = u.Company.Website,
                        Location = u.Company.Location,
                        LogoUrl = u.Company.LogoUrl,
                        IsVerified = u.Company.IsVerified,
                        Tagline = u.Company.Tagline,
                        CompanySize = u.Company.CompanySize,
                        FoundedYear = u.Company.FoundedYear
                    },

                    Experiences = u.Experiences
                        .OrderByDescending(e => e.IsCurrent)
                        .ThenByDescending(e => e.StartYear)
                        .ThenByDescending(e => e.StartMonth)
                        .Select(e => new ExperienceDto
                        {
                            Id = e.Id,
                            Title = e.Title,
                            EmploymentType = e.EmploymentType,
                            CompanyName = e.CompanyName,
                            IsCurrent = e.IsCurrent,
                            StartMonth = e.StartMonth,
                            StartYear = e.StartYear,
                            EndMonth = e.EndMonth,
                            EndYear = e.EndYear,
                            Description = e.Description
                        })
                        .ToList(),

                    Educations = u.Educations
                        .OrderByDescending(e => e.StartYear)
                        .ThenByDescending(e => e.StartMonth)
                        .Select(e => new EducationDto
                        {
                            Id = e.Id,
                            School = e.School,
                            Degree = e.Degree,
                            Field = e.Field,
                            StartMonth = e.StartMonth,
                            StartYear = e.StartYear,
                            EndMonth = e.EndMonth,
                            EndYear = e.EndYear,
                            Note = e.Note
                        })
                        .ToList(),

                    Skills = u.Skills
                        .Select(s => new SkillDto
                        {
                            Id = s.Id,
                            Name = s.Name
                        })
                        .ToList(),

                    ActivitiesPreview = new ActivitiesPreviewDto
                    {
                        PostsCount = u.Posts.Count(p => !p.IsBlocked),
                        RecentPosts = u.Posts
                            .Where(p => !p.IsBlocked)
                            .OrderByDescending(p => p.CreatedAt)
                            .Take(3)
                            .Select(p => new PostPreviewDto
                            {
                                Id = p.Id,
                                PostOwnerId = u.Id,
                                Username = u.UserName,
                                UserPhoto = u.ProfileImage,
                                Role = currentUserRole,
                                ImageUrl = p.ImageUrl,
                                Content = p.Content,
                                VideoUrl = p.VideoUrl,
                                CreatedAt = p.CreatedAt,
                                CommentCount = p.Comments.Count(),
                                LikeCount = p.Likes.Count(),
                                IsLikedByCurrentUser = p.Likes.Any(l => l.UserId == userId)
                            })
                            .ToList()
                    }
                })
                .FirstOrDefaultAsync();

        }


        public async Task<ProfileDetailsDto?> GetProfileDetailsByUsernameAsync(string username, string currentUserId, string targetUserRole)
        {
            return await _context.Users
                .AsNoTracking()
                .Where(u => u.UserName == username)
                .Select(u => new ProfileDetailsDto
                {
                    UserType = u.UserType.ToString(),
                    Role = targetUserRole,
                    BasicInfo = new BasicInfoDto
                    {
                        Id = u.Id,
                        FullName = u.FullName,
                        Username = u.UserName,
                        CurrentPosition = u.CurrentPosition,
                        ProfileImage = u.ProfileImage,
                        BackgroundImage = u.BackgroundImage,
                        Location = u.Location
                    },

                    ContactInfo = new ContactInfoDto
                    {
                        Email = u.Email,
                        PhoneNumber = u.PhoneNumber,
                        Website = u.Website,
                        Address = u.Address,
                        BirthDay = u.BirthDay,
                        BirthMonth = u.BirthMonth,
                        PhoneType = u.PhoneType
                    },

                    About = new AboutDto
                    {
                        Bio = u.Bio
                    },

                    CompanyInfo = u.Company == null ? null : new CompanyInfoDto
                    {
                        Name = u.Company.Name,
                        Industry = u.Company.Industry,
                        Bio = u.Company.Bio,
                        Website = u.Company.Website,
                        Location = u.Company.Location,
                        LogoUrl = u.Company.LogoUrl,
                        IsVerified = u.Company.IsVerified,
                        Tagline = u.Company.Tagline,
                        CompanySize = u.Company.CompanySize,
                        FoundedYear = u.Company.FoundedYear
                    },

                    Experiences = u.Experiences
                        .OrderByDescending(e => e.IsCurrent)
                        .ThenByDescending(e => e.StartYear)
                        .ThenByDescending(e => e.StartMonth)
                        .Select(e => new ExperienceDto
                        {
                            Id = e.Id,
                            Title = e.Title,
                            EmploymentType = e.EmploymentType,
                            CompanyName = e.CompanyName,
                            IsCurrent = e.IsCurrent,
                            StartMonth = e.StartMonth,
                            StartYear = e.StartYear,
                            EndMonth = e.EndMonth,
                            EndYear = e.EndYear,
                            Description = e.Description
                        })
                        .ToList(),

                    Educations = u.Educations
                        .OrderByDescending(e => e.StartYear)
                        .ThenByDescending(e => e.StartMonth)
                        .Select(e => new EducationDto
                        {
                            Id = e.Id,
                            School = e.School,
                            Degree = e.Degree,
                            Field = e.Field,
                            StartMonth = e.StartMonth,
                            StartYear = e.StartYear,
                            EndMonth = e.EndMonth,
                            EndYear = e.EndYear,
                            Note = e.Note
                        })
                        .ToList(),

                    Skills = u.Skills
                        .Select(s => new SkillDto
                        {
                            Id = s.Id,
                            Name = s.Name
                        })
                        .ToList(),

                    ActivitiesPreview = new ActivitiesPreviewDto
                    {
                        PostsCount = u.Posts.Count(p => !p.IsBlocked && !p.User.IsBlocked),
                        RecentPosts = u.Posts
                        .Where(p => !p.IsBlocked && !p.User.IsBlocked)
                            .OrderByDescending(p => p.CreatedAt)
                            .Take(3)
                            .Select(p => new PostPreviewDto
                            {
                                Id = p.Id,
                                PostOwnerId = u.Id,
                                Username = u.UserName,
                                UserPhoto = u.ProfileImage,
                                Role = targetUserRole,
                                ImageUrl = p.ImageUrl,
                                Content = p.Content,
                                VideoUrl = p.VideoUrl,
                                CreatedAt = p.CreatedAt,
                                CommentCount = p.Comments.Count(),
                                LikeCount = p.Likes.Count(),
                                IsLikedByCurrentUser = p.Likes.Any(l => l.UserId == currentUserId)
                            })
                            .ToList()
                    }
                })
                .FirstOrDefaultAsync();
        }

        public async Task<bool> IsUsernameTakenAsync(string username, string currentUserId)
        {
            if (string.IsNullOrWhiteSpace(username))
                return false;

            username = username.Trim().ToLower();

            return await _context.Users
                .AnyAsync(u => u.Id != currentUserId && u.UserName.ToLower() == username);
        }

        public async Task<bool> IsEmailTakenAsync(string email, string currentUserId)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            email = email.Trim().ToLower();

            return await _context.Users
                .AnyAsync(u => u.Id != currentUserId && u.Email.ToLower() == email);
        }

        public async Task<UserLookupDto?> GetUserByUsernameAsync(string username)
        {
            return await _context.Users
                .Where(u => u.UserName == username)
                    .Select(u => new UserLookupDto
                    {
                        Id = u.Id,
                        UserName = u.UserName,
                        ProfileImage = u.ProfileImage
                    })
                    .FirstOrDefaultAsync();
        }

        public async Task<PagedResultDto<SearchedUserDto>> GetEmployersPagedAsync(
         string currentUserId,
         int pageNumber,
         int pageSize)
        {
            var adminRoleIds = _context.Roles
                .Where(r => r.Name == "Admin")
                .Select(r => r.Id);

            var adminUserIds = _context.UserRoles
                .Where(ur => adminRoleIds.Contains(ur.RoleId))
                .Select(ur => ur.UserId);

            var query = _context.Users
                .AsNoTracking()
                .Include(u => u.Company)
                .Where(u =>
                    !u.IsBlocked &&
                    !adminUserIds.Contains(u.Id) &&
                    u.Id != currentUserId &&
                    u.UserType == Linkedin.Core.Enums.UserType.Employer
                );

            var totalCount = await query.CountAsync();

            var users = await query
                .OrderBy(u => u.UserName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new SearchedUserDto
                {
                    Id = u.Id,
                    Username = u.UserName,

                    FullName = u.Company != null
                        ? u.Company.Name
                        : u.FullName,

                    CurrentPosition = u.Company != null
                        ? u.Company.Industry
                        : u.CurrentPosition,

                    ProfileImage = u.Company != null && u.Company.LogoUrl != null
                        ? u.Company.LogoUrl
                        : u.ProfileImage,

                    Bio = u.Company != null
                        ? u.Company.Bio
                        : u.Bio,

                    Location = u.Company != null && u.Company.Location != null
                        ? u.Company.Location
                        : u.Location,

                    Visibility = u.Visibility.ToString(),
                    UserType = u.UserType.ToString(),
                    Role = "Employer",

                    CompanyName = u.Company != null ? u.Company.Name : null,
                    CompanyLogo = u.Company != null ? u.Company.LogoUrl : null,
                    CompanyIndustry = u.Company != null ? u.Company.Industry : null,

                    IsFollowing = _context.CompanyFollows.Any(cf =>
                        cf.FollowerId == currentUserId &&
                        cf.EmployerId == u.Id),

                    IsConnected = false,
                    ConnectionStatus = null,
                    RequestId = null
                })
                .ToListAsync();

            return new PagedResultDto<SearchedUserDto>
            {
                Items = users,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<PagedResultDto<SearchedUserDto>> GetJobSeekersPagedAsync(
        string currentUserId,
        int pageNumber,
        int pageSize)
        {
            var adminRoleIds = _context.Roles
                .Where(r => r.Name == "Admin")
                .Select(r => r.Id);

            var adminUserIds = _context.UserRoles
                .Where(ur => adminRoleIds.Contains(ur.RoleId))
                .Select(ur => ur.UserId);

            var query = _context.Users
                .AsNoTracking()
                .Where(u =>
                    !u.IsBlocked &&
                    !adminUserIds.Contains(u.Id) &&
                    u.Id != currentUserId &&
                    u.UserType == Linkedin.Core.Enums.UserType.JobSeeker
                );

            var totalCount = await query.CountAsync();

            var users = await query
                .OrderBy(u => u.UserName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new SearchedUserDto
                {
                    Id = u.Id,
                    Username = u.UserName,
                    FullName = u.FullName,
                    CurrentPosition = u.CurrentPosition,
                    ProfileImage = u.ProfileImage,
                    Bio = u.Bio,
                    Location = u.Location,

                    Visibility = u.Visibility.ToString(),
                    UserType = u.UserType.ToString(),
                    Role = "JobSeeker",

                    CompanyName = null,
                    CompanyLogo = null,
                    CompanyIndustry = null,

                    IsFollowing = false,

                    IsConnected = _context.Connections.Any(c =>
                        c.UserId == currentUserId &&
                        c.ConnectedUserId == u.Id),

                    ConnectionStatus =
                        _context.Connections.Any(c =>
                            c.UserId == currentUserId &&
                            c.ConnectedUserId == u.Id)
                            ? "connected"
                            : _context.ConnectionRequests.Any(r =>
                                r.SenderId == currentUserId &&
                                r.ReceiverId == u.Id &&
                                r.Status == Linkedin.Core.Enums.ConnectionRequestStatus.Pending)
                                ? "pending_sent"
                                : _context.ConnectionRequests.Any(r =>
                                    r.SenderId == u.Id &&
                                    r.ReceiverId == currentUserId &&
                                    r.Status == Linkedin.Core.Enums.ConnectionRequestStatus.Pending)
                                    ? "pending_received"
                                    : "none",

                    RequestId = _context.ConnectionRequests
                        .Where(r =>
                            (
                                r.SenderId == currentUserId &&
                                r.ReceiverId == u.Id
                            )
                            ||
                            (
                                r.SenderId == u.Id &&
                                r.ReceiverId == currentUserId
                            ))
                        .Where(r => r.Status == Linkedin.Core.Enums.ConnectionRequestStatus.Pending)
                        .Select(r => (int?)r.Id)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return new PagedResultDto<SearchedUserDto>
            {
                Items = users,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task AddSearchHistoryAsync(string userId, string query)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return;

            if (string.IsNullOrWhiteSpace(query))
                return;

            var normalizedQuery = query.Trim().ToLower();

            if (normalizedQuery.Length < 2)
                return;

            var cutoffDate = DateTime.UtcNow.AddDays(-15);

            var oldSearches = await _context.SearchHistories
                .Where(x => x.UserId == userId && x.CreatedAt < cutoffDate)
                .ToListAsync();

            if (oldSearches.Any())
                _context.SearchHistories.RemoveRange(oldSearches);

            var recentExists = await _context.SearchHistories.AnyAsync(x =>
                x.UserId == userId &&
                x.NormalizedQuery == normalizedQuery &&
                x.CreatedAt >= DateTime.UtcNow.AddMinutes(-10));

            if (!recentExists)
            {
                var searchHistory = new SearchHistory
                {
                    UserId = userId,
                    Query = query.Trim(),
                    NormalizedQuery = normalizedQuery,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.SearchHistories.AddAsync(searchHistory);
            }

            var extraSearchIds = await _context.SearchHistories
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .Skip(20)
                .Select(x => x.Id)
                .ToListAsync();

            if (extraSearchIds.Any())
            {
                var extraSearches = await _context.SearchHistories
                    .Where(x => extraSearchIds.Contains(x.Id))
                    .ToListAsync();

                _context.SearchHistories.RemoveRange(extraSearches);
            }
        }



    }
}
