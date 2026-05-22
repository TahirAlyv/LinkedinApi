using Linkedin.Business.Services.Interface;
using Linkedin.Core.Common;
using Linkedin.Core.Dtos;
using Linkedin.Core.Dtos.JobPost.Read;
using Linkedin.Core.Entities;
using Linkedin.DataAccess.Repositories.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Business.Services.Concrete
{
    public class PostService : IPostService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUploadImage _uploadImage;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<ApplicationUser> _userManager;
        public PostService(IUnitOfWork unitOfWork, IUploadImage uploadImage, IWebHostEnvironment env, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _uploadImage = uploadImage;
            _env = env;
            _userManager = userManager;
        }

        public async Task<ServiceResult> CreatePostAsync(CreatePostDto postDto, string userId)
        {
            string? imageUrl = null;
            string? videoUrl = null;

            if (postDto.File != null)
            {
                var uploadedPath = await _uploadImage.UploadFile(postDto.File);

                if (postDto.File.ContentType.StartsWith("image/"))
                {
                    imageUrl = uploadedPath;
                }
                else if (postDto.File.ContentType.StartsWith("video/"))
                {
                    videoUrl = uploadedPath;
                }
            }

            var post = new Post
            {
                UserID = userId,
                Content = postDto.Content,
                ImageUrl = imageUrl,
                VideoUrl = videoUrl,
                CreatedAt = DateTime.UtcNow,
                CommentCount = 0,
                LikeCount = 0
            };

            await _unitOfWork.Posts.AddAsync(post);
            var check = await _unitOfWork.CompleteAsync();

            if (check <= 0)
            {
                return new ServiceResult(false, "There was a problem creating the post!", null!);
            }

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return new ServiceResult(false, "User not found!", null!);
            }

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "JobSeeker";

            var returnDto = new PostDto
            {
                Id = post.Id,
                PostOwnerId = user.Id,
                Username = user.UserName,
                UserPhoto = user.ProfileImage,
                Role = role,
                Content = post.Content,
                CreatedAt = post.CreatedAt,
                ImageUrl = post.ImageUrl,
                VideoUrl = post.VideoUrl,
                CommentCount = post.CommentCount,
                LikeCount = post.LikeCount,
                IsLikedByCurrentUser = false
            };

            return new ServiceResult(true, "Post successfully created!", returnDto);
        }

        



        public async Task<ServiceResult> UpdatePost(string userId, UpdatePostDto postDto)
        {
            var post = await _unitOfWork.Posts.GetUserPostAsync(userId, postDto.PostId);

            if (post == null)
                return new ServiceResult(false, "Post not found or you do not have permission to update this post.", null);


            var hasNewFile = postDto.File != null && postDto.File.Length > 0;
            if (hasNewFile)
                postDto.DeleteMedia = false;

            if (postDto.DeleteMedia)
            {
               _ = await _uploadImage.DeletePhysicalFileIfExists(post.ImageUrl);
               _ = await _uploadImage.DeletePhysicalFileIfExists(post.VideoUrl);

                post.ImageUrl = null;
                post.VideoUrl = null;
            }
            if (hasNewFile)
            {
                // köhnə media nədirsə sil
                _ = await _uploadImage.DeletePhysicalFileIfExists(post.ImageUrl);
                _ = await _uploadImage.DeletePhysicalFileIfExists(post.VideoUrl);
 
                var extension = Path.GetExtension(postDto.File!.FileName).ToLowerInvariant();
                var isVideo = extension == ".mp4" || extension == ".avi" || extension == ".mov" || extension == ".mkv";

                var newFileUrl = await _uploadImage.UploadFile(postDto.File);

                if (isVideo)
                {
                    post.VideoUrl = newFileUrl;
                    post.ImageUrl = null;
                }
                else
                {
                    post.ImageUrl = newFileUrl;
                    post.VideoUrl = null;
                }
            }

            // ✅ Content update (null/empty göndərilsə köhnə qalır)
            if (!string.IsNullOrWhiteSpace(postDto.Content))
                post.Content = postDto.Content;

            await _unitOfWork.CompleteAsync();


            var returnPostDto = new PostDto
            {
                Id = post.Id,
                ImageUrl = post.ImageUrl,
                Content = post.Content,
                VideoUrl = post.VideoUrl,
                CreatedAt = post.CreatedAt,
                CommentCount = post.CommentCount,
                LikeCount = post.LikeCount,
                IsLikedByCurrentUser = post.Likes != null && post.Likes.Any(l => l.UserId == userId)
            };

            return new ServiceResult(true, "Post updated successfully", returnPostDto);
        }

    




        public async Task<ServiceResult> DeletePostAsync(string userId, int postId)
        {
            var post = await _unitOfWork.Posts.GetUserPostAsync(userId, postId);
            if (post == null)
                return new ServiceResult(false, "Post not found or you do not have permission to delete this post.", null);


            if (!string.IsNullOrEmpty(post.ImageUrl))
            {
                var imagePath = Path.Combine(_env.WebRootPath, post.ImageUrl.TrimStart('/'));
                if (File.Exists(imagePath))
                    File.Delete(imagePath);
            }

            if (!string.IsNullOrEmpty(post.VideoUrl))
            {
                var videoPath = Path.Combine(_env.WebRootPath, post.VideoUrl.TrimStart('/'));
                if (File.Exists(videoPath))
                    File.Delete(videoPath);
            }


            _unitOfWork.Posts.Remove(post);
            await _unitOfWork.CompleteAsync();

            return new ServiceResult(true, "Post deleted successfully.", null);
        }


        public async Task<ServiceResult> GetPostsByUserIdAsync(string postOwnerId,
        string? currentUserId, int page,int pageSize)
        {
            var skip = (page - 1) * pageSize;

            var posts = await _unitOfWork.Posts
                .GetPostsByUserIdAsync(postOwnerId!, skip, pageSize);

            if (!posts.Any())
                return new ServiceResult(false, "No posts found", null!);

            var dtoList = posts.Select(post => new PostDto
            {
                Id = post.Id,
                PostOwnerId = post.UserID,
                Content = post.Content,
                CreatedAt = post.CreatedAt,
                ImageUrl = post.ImageUrl,
                VideoUrl = post.VideoUrl,
                Username = post.User?.UserName ?? "",
                UserPhoto = post.User?.ProfileImage,
                CommentCount = post.Comments?.Count ?? 0,
                LikeCount = post.LikeCount,
                Role = post.User?.Company != null ? "Employer" : "JobSeeker",
                IsLikedByCurrentUser =
                currentUserId != null &&
                post.Likes!.Any(l => l.UserId == currentUserId),




            }).ToList();

            return new ServiceResult(true, "Posts retrieved successfully", dtoList);
        }


        public async Task<PostDto> GetUserIdPostId(string userId,int postId)
        {
             var post = await _unitOfWork.Posts.GetUserPostAsync(userId,postId);
            
             if (post == null)
                 return null!;
                var dto = new PostDto
                {
                    Id = post.Id,
                    PostOwnerId = post.UserID,
                    Content = post.Content,
                    CreatedAt = post.CreatedAt,
                    ImageUrl = post.ImageUrl,
                    Username = post.User.UserName!,
                    UserPhoto = post.User.ProfileImage,
                    CommentCount = post.CommentCount,
                    LikeCount = post.LikeCount,
                    VideoUrl = post.VideoUrl,
                };

            return dto;

        }
        public async Task<ServiceResult> GetHomeFeedAsync(
    string currentUserId,
    int page,
    int pageSize)
        {
            if (string.IsNullOrWhiteSpace(currentUserId))
                return new ServiceResult(false, "User not found.", new List<HomeFeedItemDto>());

            if (page <= 0)
                page = 1;

            if (pageSize <= 0)
                pageSize = 20;

            if (pageSize > 50)
                pageSize = 50;

            var skip = (page - 1) * pageSize;

            var followedCompanies = await _unitOfWork.CompanyFollows
                .GetFollowedCompaniesAsync(currentUserId);

            var followedEmployerIds = followedCompanies
                .Where(f => !string.IsNullOrWhiteSpace(f.EmployerId))
                .Select(f => f.EmployerId)
                .Distinct()
                .ToList();

            var connections = await _unitOfWork.Connections
                .GetUserConnectionsAsync(currentUserId);

            var connectedUserIds = connections
                .Where(c => !string.IsNullOrWhiteSpace(c.ConnectedUserId))
                .Select(c => c.ConnectedUserId)
                .Distinct()
                .ToList();

            var allowedPostUserIds = new List<string>
    {
        currentUserId
    };

            allowedPostUserIds.AddRange(connectedUserIds);
            allowedPostUserIds.AddRange(followedEmployerIds);

            allowedPostUserIds = allowedPostUserIds
                .Distinct()
                .ToList();

            var posts = await _unitOfWork.Posts.GetHomeFeedPostsAsync(
                allowedPostUserIds,
                skip,
                pageSize);

            var jobPosts = await _unitOfWork.JobPosts.GetJobPostsByEmployerIdsAsync(
                followedEmployerIds,
                skip,
                pageSize);

            var feedItems = new List<HomeFeedItemDto>();

            foreach (var post in posts)
            {
                feedItems.Add(new HomeFeedItemDto
                {
                    ItemType = "post",
                    CreatedAt = post.CreatedAt,
                    Post = new PostDto
                    {
                        Id = post.Id,
                        PostOwnerId = post.UserID,
                        Content = post.Content,
                        CreatedAt = post.CreatedAt,
                        ImageUrl = post.ImageUrl,
                        VideoUrl = post.VideoUrl,
                        Username = post.User?.UserName ?? "",
                        UserPhoto = post.User?.ProfileImage,
                        CommentCount = post.Comments?.Count ?? 0,
                        LikeCount = post.LikeCount,
                        Role = post.User?.Company != null ? "Employer" : "JobSeeker",
                        IsLikedByCurrentUser =
                            post.Likes != null &&
                            post.Likes.Any(l => l.UserId == currentUserId)
                    }
                });
            }

            foreach (var job in jobPosts)
            {
                var now = DateTime.UtcNow;

                var isExpired = job.ExpiresAt.HasValue && job.ExpiresAt.Value <= now;
                var hasApplyUrl = !string.IsNullOrWhiteSpace(job.ApplyUrl);

                var isSaved = await _unitOfWork.SavedJobs.IsSavedAsync(currentUserId, job.Id);
                var isApplied = await _unitOfWork.JobApplications.IsAppliedAsync(currentUserId, job.Id);

                feedItems.Add(new HomeFeedItemDto
                {
                    ItemType = "job",
                    CreatedAt = job.CreatedAt,
                    JobPost = new JobPostDto
                    {
                        Id = job.Id,
                        EmployerId = job.EmployerId,

                        CompanyName = job.Employer?.Company?.Name ?? job.Employer?.FullName,
                        CompanyLogo = job.Employer?.Company?.LogoUrl ?? job.Employer?.ProfileImage,
                        CompanyUsername = job.Employer?.UserName,
                        Industry = job.Employer?.Company?.Industry,

                        Title = job.Title,
                        Description = job.Description,
                        Location = job.Location,

                        WorkplaceType = job.WorkplaceType,
                        EmploymentType = job.EmploymentType,

                        ApplyUrl = job.ApplyUrl,

                        CreatedAt = job.CreatedAt,
                        UpdatedAt = job.UpdatedAt,
                        ExpiresAt = job.ExpiresAt,

                        IsActive = job.IsActive,
                        IsExpired = isExpired,
                        HasApplyUrl = hasApplyUrl,
                        CanApply = job.IsActive && !isExpired && hasApplyUrl,

                        IsOwner = job.EmployerId == currentUserId,
                        IsSaved = isSaved,
                        IsApplied = isApplied
                    }
                });
            }

            var orderedFeed = feedItems
                .OrderByDescending(x => x.CreatedAt)
                .Skip(0)
                .Take(pageSize)
                .ToList();

            return new ServiceResult(
                true,
                "Home feed loaded successfully.",
                orderedFeed);
        }


    }
}
