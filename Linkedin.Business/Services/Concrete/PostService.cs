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
using Microsoft.AspNetCore.Hosting;

namespace Linkedin.Business.Services.Concrete
{
    public class PostService : IPostService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUploadImage _uploadImage;
        private readonly UserManager<ApplicationUser> _userManager;
        public PostService(
         IUnitOfWork unitOfWork,
         IUploadImage uploadImage,
         UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _uploadImage = uploadImage;
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
            {
                return new ServiceResult(
                    false,
                    "Post not found or you do not have permission to update this post.",
                    null);
            }

            // Köhnə media URL-lərini saxlayırıq.
            // Save uğurlu olandan sonra bunları Cloudinary-dən siləcəyik.
            var oldImageUrl = post.ImageUrl;
            var oldVideoUrl = post.VideoUrl;

            var hasNewFile = postDto.File != null && postDto.File.Length > 0;
            string? uploadedUrl = null;

            // Yeni fayl seçilibsə, DeleteMedia avtomatik nəzərə alınmır.
            if (hasNewFile)
            {
                postDto.DeleteMedia = false;

                // Əvvəl yeni media Cloudinary-yə yüklənir.
                uploadedUrl = await _uploadImage.UploadFile(postDto.File!);

                if (string.IsNullOrWhiteSpace(uploadedUrl))
                {
                    return new ServiceResult(
                        false,
                        "Media upload failed.",
                        null);
                }

                var extension = Path.GetExtension(postDto.File!.FileName)
                    .ToLowerInvariant();

                var isVideo = extension is ".mp4" or ".avi" or ".mov" or ".webm";

                if (isVideo)
                {
                    post.VideoUrl = uploadedUrl;
                    post.ImageUrl = null;
                }
                else
                {
                    post.ImageUrl = uploadedUrl;
                    post.VideoUrl = null;
                }
            }
            else if (postDto.DeleteMedia)
            {
                // İstifadəçi media silmək istəyirsə,
                // database-dən URL-ləri çıxarırıq.
                post.ImageUrl = null;
                post.VideoUrl = null;
            }

            if (!string.IsNullOrWhiteSpace(postDto.Content))
            {
                post.Content = postDto.Content;
            }

            var check = await _unitOfWork.CompleteAsync();

            if (check <= 0)
            {
                // Yeni fayl Cloudinary-yə yüklənib, amma database save alınmayıbsa,
                // boşuna Cloudinary-də qalmasın.
                if (!string.IsNullOrWhiteSpace(uploadedUrl))
                {
                    await _uploadImage.DeletePhysicalFileIfExists(uploadedUrl);
                }

                return new ServiceResult(
                    false,
                    "There was a problem updating the post.",
                    null);
            }

            // Database update uğurludursa, artıq köhnə media silinə bilər.
            if (hasNewFile || postDto.DeleteMedia)
            {
                if (!string.IsNullOrWhiteSpace(oldImageUrl))
                {
                    await _uploadImage.DeletePhysicalFileIfExists(oldImageUrl);
                }

                if (!string.IsNullOrWhiteSpace(oldVideoUrl))
                {
                    await _uploadImage.DeletePhysicalFileIfExists(oldVideoUrl);
                }
            }

            var role = post.User?.UserType.ToString() ?? "User";


            var returnPostDto = new PostDto
            {
                Id = post.Id,

                PostOwnerId = post.UserID,

                Username = post.User?.UserName ?? "",

                UserPhoto = role == "Employer" && post.User?.Company != null
                    ? post.User.Company.LogoUrl
                    : post.User?.ProfileImage,
                Role = role,
                ImageUrl = post.ImageUrl,
                Content = post.Content,
                VideoUrl = post.VideoUrl,
                CreatedAt = post.CreatedAt,
                CommentCount = post.CommentCount,
                LikeCount = post.LikeCount,

                IsLikedByCurrentUser = post.Likes != null &&
                              post.Likes.Any(l => l.UserId == userId)
            };

            return new ServiceResult(
                true,
                "Post updated successfully",
                returnPostDto);
        }




        public async Task<ServiceResult> DeletePostAsync(string userId, int postId)
        {
            var post = await _unitOfWork.Posts.GetUserPostAsync(userId, postId);

            if (post == null)
            {
                return new ServiceResult(
                    false,
                    "Post not found or you do not have permission to delete this post.",
                    null);
            }

            // Cloudinary URL-lərini post silinməzdən qabaq yadda saxlayırıq
            var imageUrl = post.ImageUrl;
            var videoUrl = post.VideoUrl;

            // Əvvəl database-dən postu silirik
            _unitOfWork.Posts.Remove(post);

            var check = await _unitOfWork.CompleteAsync();

            if (check <= 0)
            {
                return new ServiceResult(
                    false,
                    "There was a problem deleting the post.",
                    null);
            }

            // Database silinməsi uğurludursa, media fayllarını Cloudinary-dən silirik
            if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                await _uploadImage.DeletePhysicalFileIfExists(imageUrl);
            }

            if (!string.IsNullOrWhiteSpace(videoUrl))
            {
                await _uploadImage.DeletePhysicalFileIfExists(videoUrl);
            }

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

            var jobTake = pageSize >= 3 ? pageSize / 3 : 0;
            var postTake = pageSize - jobTake;

            var posts = await _unitOfWork.Posts.GetRecommendedFeedPostsAsync(
                currentUserId,
                page,
                postTake);

            var jobPosts = jobTake > 0
                ? await _unitOfWork.JobPosts.GetRecommendedJobPostsAsync(
                    currentUserId,
                    page,
                    jobTake)
                : new List<JobPost>();

            var postItems = new List<HomeFeedItemDto>();

            foreach (var post in posts)
            {
                postItems.Add(new HomeFeedItemDto
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

                        UserPhoto =
                            post.User?.UserType == Linkedin.Core.Enums.UserType.Employer &&
                            post.User.Company != null &&
                            !string.IsNullOrWhiteSpace(post.User.Company.LogoUrl)
                                ? post.User.Company.LogoUrl
                                : post.User?.ProfileImage,

                        CommentCount = post.Comments?.Count ?? post.CommentCount,
                        LikeCount = post.Likes?.Count ?? post.LikeCount,

                        Role = post.User?.UserType.ToString() ?? "User",

                        IsLikedByCurrentUser =
                            post.Likes != null &&
                            post.Likes.Any(l => l.UserId == currentUserId)
                    }
                });
            }

            var jobItems = new List<HomeFeedItemDto>();

            foreach (var job in jobPosts)
            {
                var now = DateTime.UtcNow;

                var isExpired = job.ExpiresAt.HasValue && job.ExpiresAt.Value <= now;
                var hasApplyUrl = !string.IsNullOrWhiteSpace(job.ApplyUrl);

                var isSaved = await _unitOfWork.SavedJobs.IsSavedAsync(
                    currentUserId,
                    job.Id);

                var isApplied = await _unitOfWork.JobApplications.IsAppliedAsync(
                    currentUserId,
                    job.Id);

                jobItems.Add(new HomeFeedItemDto
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

            var feedItems = new List<HomeFeedItemDto>();

            var postIndex = 0;
            var jobIndex = 0;

            while (feedItems.Count < pageSize &&
                   (postIndex < postItems.Count || jobIndex < jobItems.Count))
            {
                for (int i = 0; i < 2 && postIndex < postItems.Count && feedItems.Count < pageSize; i++)
                {
                    feedItems.Add(postItems[postIndex]);
                    postIndex++;
                }

                if (jobIndex < jobItems.Count && feedItems.Count < pageSize)
                {
                    feedItems.Add(jobItems[jobIndex]);
                    jobIndex++;
                }
            }

            return new ServiceResult(
                true,
                "Home feed loaded successfully.",
                feedItems);
        }


    }
}
