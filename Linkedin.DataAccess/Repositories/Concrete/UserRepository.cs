using Linkedin.Core.Data;
using Linkedin.Core.Dtos;
using Linkedin.Core.Dtos.Pagination;
using Linkedin.Core.Dtos.Profile.Read;
using Linkedin.Core.Dtos.Search;
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

                    CompanyId = u.Company != null ? u.Company.Id : null,
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
                            CompanyId = e.CompanyId,
                            CompanyLogoUrl = e.Company != null
                                ? e.Company.LogoUrl ?? e.Company.User.ProfileImage
                                : null,
                            CompanyUsername = e.Company != null ? e.Company.User.UserName : null,
                            IsCurrent = e.IsCurrent,
                            StartMonth = e.StartMonth,
                            StartYear = e.StartYear,
                            EndMonth = e.EndMonth,
                            EndYear = e.EndYear,
                            Location = e.Location,
                            LocationType = e.LocationType,
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
                            InstitutionCompanyId = e.InstitutionCompanyId,
                            InstitutionLogoUrl = e.InstitutionCompany != null
                                ? e.InstitutionCompany.LogoUrl ?? e.InstitutionCompany.User.ProfileImage
                                : null,
                            InstitutionUsername = e.InstitutionCompany != null
                                ? e.InstitutionCompany.User.UserName
                                : null,
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
                            CompanyId = e.CompanyId,
                            CompanyLogoUrl = e.Company != null
                                ? e.Company.LogoUrl ?? e.Company.User.ProfileImage
                                : null,
                            CompanyUsername = e.Company != null ? e.Company.User.UserName : null,
                            IsCurrent = e.IsCurrent,
                            StartMonth = e.StartMonth,
                            StartYear = e.StartYear,
                            EndMonth = e.EndMonth,
                            EndYear = e.EndYear,
                            Location = e.Location,
                            LocationType = e.LocationType,
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
                            InstitutionCompanyId = e.InstitutionCompanyId,
                            InstitutionLogoUrl = e.InstitutionCompany != null
                                ? e.InstitutionCompany.LogoUrl ?? e.InstitutionCompany.User.ProfileImage
                                : null,
                            InstitutionUsername = e.InstitutionCompany != null
                                ? e.InstitutionCompany.User.UserName
                                : null,
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

                    CompanyId = u.Company != null ? u.Company.Id : null,
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

            var recentItem = await _context.SearchHistories.FirstOrDefaultAsync(x =>
                x.UserId == userId &&
                x.NormalizedQuery == normalizedQuery &&
                x.CreatedAt >= DateTime.UtcNow.AddMinutes(-10));

            if (recentItem == null)
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
            else
            {
                recentItem.Query = query.Trim();
                recentItem.CreatedAt = DateTime.UtcNow;
                recentItem.HiddenAt = null;
            }
        }

        public async Task<PagedResultDto<RecommendedUserDto>> GetRecommendedUsersAsync(
        string currentUserId,
         int pageNumber,
         int pageSize)
        {
            if (pageNumber < 1)
                pageNumber = 1;

            if (pageSize < 1)
                pageSize = 10;

            if (pageSize > 50)
                pageSize = 50;

            var currentUser = await _context.Users
                .AsNoTracking()
                .Include(u => u.Skills)
                .Include(u => u.Experiences)
                .Include(u => u.Educations)
                .FirstOrDefaultAsync(u => u.Id == currentUserId);

            if (currentUser == null)
            {
                return new PagedResultDto<RecommendedUserDto>
                {
                    Items = new List<RecommendedUserDto>(),
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = 0
                };
            }

            var myConnectionIds = await _context.Connections
                .AsNoTracking()
                .Where(c => c.UserId == currentUserId)
                .Select(c => c.ConnectedUserId)
                .Distinct()
                .ToListAsync();

            var mySkillNames = currentUser.Skills
                .Where(s => !string.IsNullOrWhiteSpace(s.Name))
                .Select(s => s.Name.Trim().ToLower())
                .Distinct()
                .ToList();

            var myEducationNames = currentUser.Educations
                .Where(e => !string.IsNullOrWhiteSpace(e.School))
                .Select(e => e.School.Trim().ToLower())
                .Distinct()
                .ToList();

            var myCompanyNames = currentUser.Experiences
                .Where(e => !string.IsNullOrWhiteSpace(e.CompanyName))
                .Select(e => e.CompanyName.Trim().ToLower())
                .Distinct()
                .ToList();

            var searchKeywords = await _context.SearchHistories
                .AsNoTracking()
                .Where(x => x.UserId == currentUserId)
                .OrderByDescending(x => x.CreatedAt)
                .Take(10)
                .Select(x => x.NormalizedQuery)
                .ToListAsync();

            var candidateUsers = await _context.Users
                .AsNoTracking()
                .Include(u => u.Skills)
                .Include(u => u.Experiences)
                .Include(u => u.Educations)
                .Include(u => u.Company)
                .Where(u =>
                    !u.IsBlocked &&
                    u.Id != currentUserId &&
                    !myConnectionIds.Contains(u.Id))
                .Take(500)
                .ToListAsync();

            var allConnections = await _context.Connections
                .AsNoTracking()
                .ToListAsync();

            var pendingRequests = await _context.ConnectionRequests
                .AsNoTracking()
                .Where(r =>
                    r.Status == Linkedin.Core.Enums.ConnectionRequestStatus.Pending &&
                    (r.SenderId == currentUserId || r.ReceiverId == currentUserId))
                .ToListAsync();

            var followedEmployerIds = await _context.CompanyFollows
                .AsNoTracking()
                .Where(cf => cf.FollowerId == currentUserId)
                .Select(cf => cf.EmployerId)
                .ToListAsync();

            var recommended = candidateUsers
                .Select(u =>
                {
                    var candidateConnectionIds = allConnections
                        .Where(c => c.UserId == u.Id)
                        .Select(c => c.ConnectedUserId)
                        .Distinct()
                        .ToList();

                    var mutualConnectionsCount = myConnectionIds
                        .Intersect(candidateConnectionIds)
                        .Count();

                    var candidateSkillNames = u.Skills
                        .Where(s => !string.IsNullOrWhiteSpace(s.Name))
                        .Select(s => s.Name.Trim().ToLower())
                        .Distinct()
                        .ToList();

                    var commonSkillsCount = mySkillNames
                        .Intersect(candidateSkillNames)
                        .Count();

                    var candidateEducationNames = u.Educations
                        .Where(e => !string.IsNullOrWhiteSpace(e.School))
                        .Select(e => e.School.Trim().ToLower())
                        .Distinct()
                        .ToList();

                    var sameEducation = myEducationNames
                        .Intersect(candidateEducationNames)
                        .Any();

                    var candidateCompanyNames = u.Experiences
                        .Where(e => !string.IsNullOrWhiteSpace(e.CompanyName))
                        .Select(e => e.CompanyName.Trim().ToLower())
                        .Distinct()
                        .ToList();

                    var sameCompany = myCompanyNames
                        .Intersect(candidateCompanyNames)
                        .Any();

                    var sameLocation =
                        !string.IsNullOrWhiteSpace(currentUser.Location) &&
                        !string.IsNullOrWhiteSpace(u.Location) &&
                        currentUser.Location.Trim().ToLower() == u.Location.Trim().ToLower();

                    var searchableText = (
                        $"{u.UserName} " +
                        $"{u.FullName} " +
                        $"{u.CurrentPosition} " +
                        $"{u.Location} " +
                        $"{u.Bio} " +
                        $"{u.Company?.Name} " +
                        $"{u.Company?.Industry}"
                    ).ToLower();

                    var searchMatch = searchKeywords.Any(k =>
                        !string.IsNullOrWhiteSpace(k) &&
                        searchableText.Contains(k));

                    var hasRealMatch =
                        mutualConnectionsCount > 0 ||
                        commonSkillsCount > 0 ||
                        sameCompany ||
                        sameEducation ||
                        sameLocation ||
                        searchMatch;

                    var score = 0;

                    score += mutualConnectionsCount * 25;
                    score += commonSkillsCount * 15;

                    if (sameCompany)
                        score += 40;

                    if (sameEducation)
                        score += 25;

                    if (sameLocation)
                        score += 15;

                    if (searchMatch)
                        score += 20;

                    if (hasRealMatch &&
                        currentUser.UserType == Linkedin.Core.Enums.UserType.JobSeeker &&
                        u.UserType == Linkedin.Core.Enums.UserType.Employer)
                        score += 15;

                    if (hasRealMatch &&
                        currentUser.UserType == Linkedin.Core.Enums.UserType.Employer &&
                        u.UserType == Linkedin.Core.Enums.UserType.JobSeeker)
                        score += 15;

                    var request = pendingRequests.FirstOrDefault(r =>
                        (r.SenderId == currentUserId && r.ReceiverId == u.Id) ||
                        (r.SenderId == u.Id && r.ReceiverId == currentUserId));

                    var connectionStatus = request == null
                        ? "none"
                        : request.SenderId == currentUserId
                            ? "pending_sent"
                            : "pending_received";

                    var reason = "Recommended for you";

                    if (mutualConnectionsCount > 0)
                        reason = $"{mutualConnectionsCount} mutual connection(s)";
                    else if (commonSkillsCount > 0)
                        reason = $"{commonSkillsCount} common skill(s)";
                    else if (sameCompany)
                        reason = "Same company experience";
                    else if (sameEducation)
                        reason = "Same education";
                    else if (sameLocation)
                        reason = "Same location";
                    else if (searchMatch)
                        reason = "Based on your search activity";

                    return new RecommendedUserDto
                    {
                        Id = u.Id,
                        Username = u.UserName,

                        FullName = u.UserType == Linkedin.Core.Enums.UserType.Employer && u.Company != null
                            ? u.Company.Name
                            : u.FullName,

                        CurrentPosition = u.UserType == Linkedin.Core.Enums.UserType.Employer && u.Company != null
                            ? u.Company.Industry
                            : u.CurrentPosition,

                        ProfileImage = u.UserType == Linkedin.Core.Enums.UserType.Employer &&
                                       u.Company != null &&
                                       !string.IsNullOrWhiteSpace(u.Company.LogoUrl)
                            ? u.Company.LogoUrl
                            : u.ProfileImage,

                        Location = u.UserType == Linkedin.Core.Enums.UserType.Employer &&
                                   u.Company != null &&
                                   !string.IsNullOrWhiteSpace(u.Company.Location)
                            ? u.Company.Location
                            : u.Location,

                        UserType = u.UserType.ToString(),

                        CompanyName = u.Company?.Name,
                        CompanyLogo = u.Company?.LogoUrl,

                        Score = score,
                        MutualConnectionsCount = mutualConnectionsCount,
                        CommonSkillsCount = commonSkillsCount,
                        RecommendationReason = reason,

                        IsConnected = false,
                        ConnectionStatus = connectionStatus,
                        RequestId = request?.Id,

                        IsFollowing = u.UserType == Linkedin.Core.Enums.UserType.Employer &&
                                      followedEmployerIds.Contains(u.Id)
                    };
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.FullName)
                .ToList();

            var totalCount = recommended.Count;

            var items = recommended
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedResultDto<RecommendedUserDto>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<List<SearchHistoryDto>> GetSearchHistoryAsync(
            string userId,
            int take)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return new List<SearchHistoryDto>();

            if (take < 1)
                take = 10;

            if (take > 30)
                take = 30;

            return await _context.SearchHistories
                .AsNoTracking()
                .Where(x => x.UserId == userId && x.HiddenAt == null)
                .OrderByDescending(x => x.CreatedAt)
                .Take(take)
                .Select(x => new SearchHistoryDto
                {
                    Id = x.Id,
                    Query = x.Query,
                    NormalizedQuery = x.NormalizedQuery,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<bool> HideSearchHistoryAsync(string userId, int historyId)
        {
            var item = await _context.SearchHistories
                .FirstOrDefaultAsync(x =>
                    x.Id == historyId &&
                    x.UserId == userId &&
                    x.HiddenAt == null);

            if (item == null)
                return false;

            item.HiddenAt = DateTime.UtcNow;
            return true;
        }

        public async Task<int> HideAllSearchHistoryAsync(string userId)
        {
            var visibleItems = await _context.SearchHistories
                .Where(x => x.UserId == userId && x.HiddenAt == null)
                .ToListAsync();

            var hiddenAt = DateTime.UtcNow;
            foreach (var item in visibleItems)
                item.HiddenAt = hiddenAt;

            return visibleItems.Count;
        }



    }
}
