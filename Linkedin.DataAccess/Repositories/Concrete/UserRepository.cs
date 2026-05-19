using Linkedin.Core.Data;
using Linkedin.Core.Dtos;
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

            query = query.ToLower();

            return await _context.Users
                .Where(u => u.UserName.ToLower().Contains(query))
                .Select(u => new SearchedUserDto
                {
                    Id = u.Id,
                    Username = u.UserName,
                    ProfileImage = u.ProfileImage,
                    Bio = u.Bio,
                    Visibility = u.Visibility
                })
                .Take(10)
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
                        PostsCount = u.Posts.Count(),
                        RecentPosts = u.Posts
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
                        PostsCount = u.Posts.Count(),
                        RecentPosts = u.Posts
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

        
    }
}
