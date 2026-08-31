using Linkedin.Business.Services.Interface;
using Linkedin.Core.Common;
using Linkedin.Core.Data;
using Linkedin.Core.Dtos;
using Linkedin.Core.Dtos.AI;
using Linkedin.Core.Dtos.JobPost.Read;
using Linkedin.Core.Entities;
using Linkedin.Core.Enums;
using Linkedin.DataAccess.Repositories.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Linkedin.Business.Services.Concrete
{
    public class PostService : IPostService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUploadImage _uploadImage;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAiService _aiService;
        private readonly INotficationsService _notificationsService;
        private readonly AppDbContext _context;
        public PostService(
      IUnitOfWork unitOfWork,
      IUploadImage uploadImage,
      UserManager<ApplicationUser> userManager,
      IAiService aiService,
      INotficationsService notificationsService,
      AppDbContext context)
        {
            _unitOfWork = unitOfWork;
            _uploadImage = uploadImage;
            _userManager = userManager;
            _aiService = aiService;
            _notificationsService = notificationsService;
            _context = context;
        }

        public async Task<ServiceResult> CreatePostAsync(CreatePostDto postDto, string userId)
        {
            var mentionedCompanyId = postDto.MentionedCompanyId;
            var mentionedCompanyOwner = await FindMentionedCompanyOwnerAsync(
                mentionedCompanyId);

            // The suggestion picker sends MentionedCompanyId, but older clients
            // and manually typed mentions only send the post text. Resolve the
            // first valid employer username so the visual @mention and the
            // persisted company relation cannot drift apart.
            if (!mentionedCompanyId.HasValue)
            {
                mentionedCompanyOwner =
                    await FindMentionedCompanyOwnerFromContentAsync(
                        postDto.Content);
                mentionedCompanyId = mentionedCompanyOwner?.Company?.Id;
            }

            if (mentionedCompanyId.HasValue &&
                mentionedCompanyOwner == null)
            {
                return ServiceResult.Failure("The mentioned company was not found.");
            }

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

            PostModerationResultDto? moderation = null;
            var shouldSendToReview = false;
            var moderationReason = "";

            if (!string.IsNullOrWhiteSpace(postDto.Content))
            {
                var moderationResult = await _aiService.ModeratePostAsync(postDto.Content);

                if (moderationResult.Success &&
                    moderationResult.Data is PostModerationResultDto moderationData)
                {
                    moderation = moderationData;

                    shouldSendToReview =
                        moderation.IsFlagged &&
                        string.Equals(
                            moderation.SuggestedAction,
                            "PendingReview",
                            StringComparison.OrdinalIgnoreCase);

                    moderationReason = moderation.Reason;
                }
                else
                {
                    // AI yoxlama alınmadısa, təhlükəsizlik üçün postu admin review-a göndəririk.
                    // Beləliklə Gemini quota/error olsa belə riskli post avtomatik publish olmur.
                    shouldSendToReview = true;
                    moderationReason = moderationResult.Message;
                }
            }

            var post = new Post
            {
                UserID = userId,
                Content = postDto.Content,
                ImageUrl = imageUrl,
                VideoUrl = videoUrl,
                MentionedCompanyId = mentionedCompanyId,
                CreatedAt = DateTime.UtcNow,
                CommentCount = 0,
                LikeCount = 0,

                IsAiFlagged = shouldSendToReview,

                ModerationStatus = shouldSendToReview
                    ? PostModerationStatus.PendingReview
                    : PostModerationStatus.Published,

                IsBlocked = shouldSendToReview,

                BlockReason = shouldSendToReview
                    ? "AI moderation: pending admin review"
                    : null,

                AiModerationRiskLevel = moderation?.RiskLevel,

                AiModerationCategories = moderation?.Categories != null
                    ? string.Join(", ", moderation.Categories)
                    : null,

                AiModerationReason = shouldSendToReview
                    ? moderationReason
                    : moderation?.Reason,

                AiModerationCheckedAt = !string.IsNullOrWhiteSpace(postDto.Content)
                    ? DateTime.UtcNow
                    : null
            };

            await _unitOfWork.Posts.AddAsync(post);
            var check = await _unitOfWork.CompleteAsync();

            if (check <= 0)
            {
                return new ServiceResult(false, "There was a problem creating the post!", null!);
            }

            var user = await _context.Users
                .Include(item => item.Company)
                .FirstOrDefaultAsync(item => item.Id == userId);

            if (user == null)
            {
                return new ServiceResult(false, "User not found!", null!);
            }

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "JobSeeker";

            if (shouldSendToReview)
            {
                await _notificationsService.CreateOrUpdateAsync(
                    senderId: userId,
                    receiverId: userId,
                    type: NotificationType.PostModerationWarning,
                    postId: post.Id,
                    contentPreview:
                        "Your post has been sent to admin review because it may contain inappropriate content.",
                    senderUsername: "System",
                    senderProfilePhoto: ""
                );
            }
            else if (mentionedCompanyOwner != null &&
                     mentionedCompanyOwner.Id != userId)
            {
                await _notificationsService.CreateOrUpdateAsync(
                    senderId: userId,
                    receiverId: mentionedCompanyOwner.Id,
                    type: NotificationType.CompanyMention,
                    postId: post.Id,
                    contentPreview: $"mentioned {mentionedCompanyOwner.Company?.Name ?? "your company"} in a post",
                    senderUsername: user.UserName ?? user.FullName ?? "Member",
                    senderProfilePhoto:
                        user.UserType == UserType.Employer
                            ? user.Company?.LogoUrl ?? user.ProfileImage ?? ""
                            : user.ProfileImage ?? "");
            }

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
                MentionedCompanyId = post.MentionedCompanyId,
                MentionedCompanyName = mentionedCompanyOwner?.Company?.Name,
                MentionedCompanyUsername = mentionedCompanyOwner?.UserName,
                CommentCount = post.CommentCount,
                LikeCount = post.LikeCount,
                IsLikedByCurrentUser = false,

                ModerationStatus = post.ModerationStatus.ToString(),
                IsAiFlagged = post.IsAiFlagged,
                AiModerationReason = post.AiModerationReason
            };

            var message = shouldSendToReview
                ? "Your post has been sent to admin review due to a content warning."
                : "Post successfully created!";

            return new ServiceResult(true, message, returnDto);
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
            var oldMentionedCompanyId = post.MentionedCompanyId;
            ApplicationUser? mentionedCompanyOwner = null;

            if (postDto.ClearCompanyMention)
            {
                post.MentionedCompanyId = null;
            }
            else if (postDto.MentionedCompanyId.HasValue)
            {
                mentionedCompanyOwner = await FindMentionedCompanyOwnerAsync(
                    postDto.MentionedCompanyId);

                if (mentionedCompanyOwner == null)
                {
                    return ServiceResult.Failure(
                        "The mentioned company was not found.");
                }

                post.MentionedCompanyId = postDto.MentionedCompanyId;
            }
            else if (!post.MentionedCompanyId.HasValue)
            {
                mentionedCompanyOwner =
                    await FindMentionedCompanyOwnerFromContentAsync(
                        postDto.Content);

                if (mentionedCompanyOwner?.Company != null)
                {
                    post.MentionedCompanyId =
                        mentionedCompanyOwner.Company.Id;
                }
            }

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

            if (mentionedCompanyOwner == null && post.MentionedCompanyId.HasValue)
            {
                mentionedCompanyOwner = await FindMentionedCompanyOwnerAsync(
                    post.MentionedCompanyId);
            }

            if (post.MentionedCompanyId.HasValue &&
                post.MentionedCompanyId != oldMentionedCompanyId &&
                mentionedCompanyOwner != null &&
                mentionedCompanyOwner.Id != userId &&
                post.ModerationStatus == PostModerationStatus.Published &&
                !post.IsBlocked)
            {
                await _notificationsService.CreateOrUpdateAsync(
                    senderId: userId,
                    receiverId: mentionedCompanyOwner.Id,
                    type: NotificationType.CompanyMention,
                    postId: post.Id,
                    contentPreview: $"mentioned {mentionedCompanyOwner.Company?.Name ?? "your company"} in a post",
                    senderUsername: post.User?.UserName ?? "Member",
                    senderProfilePhoto:
                        post.User?.UserType == UserType.Employer
                            ? post.User.Company?.LogoUrl ?? post.User.ProfileImage ?? ""
                            : post.User?.ProfileImage ?? "");
            }


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
                MentionedCompanyId = post.MentionedCompanyId,
                MentionedCompanyName = mentionedCompanyOwner?.Company?.Name,
                MentionedCompanyUsername = mentionedCompanyOwner?.UserName,
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
                MentionedCompanyId = post.MentionedCompanyId,
                MentionedCompanyName = post.MentionedCompany?.Name,
                MentionedCompanyUsername = post.MentionedCompany?.User?.UserName,
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

        public async Task<ServiceResult> GetPostByIdAsync(
            int postId,
            string? currentUserId)
        {
            var post = await _unitOfWork.Posts.GetPostByIdAsync(
                postId,
                item => item.User,
                item => item.Likes!,
                item => item.Comments!);

            if (post == null ||
                post.IsBlocked ||
                post.ModerationStatus != PostModerationStatus.Published)
            {
                return new ServiceResult(false, "Post was not found.", null!);
            }

            var dto = new PostDto
            {
                Id = post.Id,
                PostOwnerId = post.UserID,
                Content = post.Content,
                CreatedAt = post.CreatedAt,
                ImageUrl = post.ImageUrl,
                VideoUrl = post.VideoUrl,
                MentionedCompanyId = post.MentionedCompanyId,
                MentionedCompanyName = post.MentionedCompany?.Name,
                MentionedCompanyUsername = post.MentionedCompany?.User?.UserName,
                Username = post.User?.UserName ?? string.Empty,
                UserPhoto = post.User?.ProfileImage,
                Role = post.User?.UserType == UserType.Employer
                    ? "Employer"
                    : "JobSeeker",
                CommentCount = post.Comments?.Count ?? post.CommentCount ?? 0,
                LikeCount = post.Likes?.Count ?? post.LikeCount ?? 0,
                IsLikedByCurrentUser =
                    currentUserId != null &&
                    post.Likes?.Any(like => like.UserId == currentUserId) == true,
                ModerationStatus = post.ModerationStatus.ToString(),
                IsAiFlagged = post.IsAiFlagged,
                AiModerationReason = post.AiModerationReason
            };

            return new ServiceResult(true, "Post retrieved successfully.", dto);
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
                    MentionedCompanyId = post.MentionedCompanyId,
                    MentionedCompanyName = post.MentionedCompany?.Name,
                    MentionedCompanyUsername = post.MentionedCompany?.User?.UserName,
                };

            return dto;

        }

        public async Task<ServiceResult> SearchPostsAsync(
            string query,
            string? currentUserId,
            int page,
            int pageSize)
        {
            var cleanQuery = query?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(cleanQuery))
                return ServiceResult.SuccessResult("No search query.", new List<PostDto>());

            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 30);

            var posts = await _unitOfWork.Posts.SearchPostsAsync(
                cleanQuery,
                (page - 1) * pageSize,
                pageSize);

            var result = posts.Select(post => new PostDto
            {
                Id = post.Id,
                PostOwnerId = post.UserID,
                Username = post.User?.UserName ?? string.Empty,
                UserPhoto = post.User?.ProfileImage,
                Role = post.User?.Company != null ? "Employer" : "JobSeeker",
                Content = post.Content,
                ImageUrl = post.ImageUrl,
                VideoUrl = post.VideoUrl,
                MentionedCompanyId = post.MentionedCompanyId,
                MentionedCompanyName = post.MentionedCompany?.Name,
                MentionedCompanyUsername = post.MentionedCompany?.User?.UserName,
                CreatedAt = post.CreatedAt,
                CommentCount = post.Comments?.Count ?? 0,
                LikeCount = post.Likes?.Count ?? post.LikeCount ?? 0,
                IsLikedByCurrentUser =
                    currentUserId != null &&
                    post.Likes != null &&
                    post.Likes.Any(like => like.UserId == currentUserId),
                ModerationStatus = post.ModerationStatus.ToString(),
            }).ToList();

            return ServiceResult.SuccessResult("Posts retrieved successfully.", result);
        }

        public async Task<ServiceResult> GetHashtagSuggestionsAsync(
            string? query,
            int take)
        {
            take = Math.Clamp(take, 1, 20);
            var contents = await _unitOfWork.Posts.GetHashtagContentsAsync(query, 250);
            var normalizedQuery = query?.Trim().TrimStart('#') ?? string.Empty;
            var pattern = new Regex(
                @"(?<![\p{L}\p{N}_])#([\p{L}\p{N}_-]+)",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

            var tags = contents
                .SelectMany(content => pattern
                    .Matches(content)
                    .Cast<Match>()
                    .Select(match => match.Groups[1].Value))
                .Where(tag =>
                    string.IsNullOrWhiteSpace(normalizedQuery) ||
                    tag.StartsWith(normalizedQuery, StringComparison.OrdinalIgnoreCase))
                .GroupBy(tag => tag, StringComparer.OrdinalIgnoreCase)
                .Select(group => new HashtagSuggestionDto
                {
                    Name = group.Key,
                    PostCount = group.Count(),
                })
                .OrderByDescending(item => item.PostCount)
                .ThenBy(item => item.Name)
                .Take(take)
                .ToList();

            return ServiceResult.SuccessResult("Hashtags retrieved successfully.", tags);
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

            var currentUser = await _unitOfWork.Users.GetByIdAsync(currentUserId);
            var canCurrentUserApply = currentUser?.UserType == UserType.JobSeeker;

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
                        MentionedCompanyId = post.MentionedCompanyId,
                        MentionedCompanyName = post.MentionedCompany?.Name,
                        MentionedCompanyUsername = post.MentionedCompany?.User?.UserName,

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
                        CanApply = canCurrentUserApply && job.IsActive && !isExpired && hasApplyUrl,

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

        private async Task<ApplicationUser?> FindMentionedCompanyOwnerAsync(
            int? companyId)
        {
            if (!companyId.HasValue)
                return null;

            return await _context.Users
                .AsNoTracking()
                .Include(item => item.Company)
                .FirstOrDefaultAsync(item =>
                    item.UserType == UserType.Employer &&
                    item.Company != null &&
                    item.Company.Id == companyId.Value &&
                    !item.IsBlocked);
        }

        private async Task<ApplicationUser?>
            FindMentionedCompanyOwnerFromContentAsync(string? content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return null;

            var matches = Regex.Matches(
                content,
                @"(?<![\p{L}\p{N}._-])@([\p{L}\p{N}._-]{3,30})",
                RegexOptions.CultureInvariant);

            foreach (Match match in matches)
            {
                // A sentence-ending dot is commonly typed immediately after a
                // mention. It is not part of the username in that context.
                var username = match.Groups[1].Value
                    .TrimEnd('.', '_', '-');

                if (string.IsNullOrWhiteSpace(username))
                    continue;

                var normalizedUsername =
                    _userManager.NormalizeName(username);

                var owner = await _context.Users
                    .AsNoTracking()
                    .Include(item => item.Company)
                    .FirstOrDefaultAsync(item =>
                        item.UserType == UserType.Employer &&
                        item.Company != null &&
                        item.NormalizedUserName == normalizedUsername &&
                        !item.IsBlocked);

                if (owner != null)
                    return owner;
            }

            return null;
        }


    }
}
