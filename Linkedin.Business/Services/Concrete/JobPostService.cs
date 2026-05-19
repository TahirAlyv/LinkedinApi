using Linkedin.Business.Services.Interface;
using Linkedin.Core.Common;
using Linkedin.Core.Dtos;
using Linkedin.Core.Entities;
using Linkedin.DataAccess.Repositories.Interfaces;
 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Business.Services.Concrete
{
    public class JobPostService : IJobPostService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUploadImage _uploadImage;

        public JobPostService(IUnitOfWork unitOfWork, IUploadImage uploadImage)
        {
            _unitOfWork = unitOfWork;
            _uploadImage = uploadImage;
        }
        public async Task<ServiceResult> CreateJobPostAsync(CreateJobPostDto postDto, string userId)
        {
 
            string? imageUrl = null;

            try
            {
                if (postDto.File != null && postDto.File.Length > 0)
                {
                    imageUrl = await _uploadImage.UploadFile(postDto.File);
                }

                var jobPost = new JobPost
                {
                    EmployerId = userId,
                    Title = postDto.Title.Trim(),
                    Description = postDto.Description.Trim(),
                    Location = postDto.Location?.Trim(),
                    Salary = postDto.Salary,
                    ImageUrl = imageUrl,
                    Skills = postDto.Skills,
                    CreatedAt = DateTime.UtcNow // ✅ CreatedAt garanti
                };

                await _unitOfWork.JobPosts.AddAsync(jobPost);

                var check = await _unitOfWork.CompleteAsync();
                if (check <= 0)
                {
                    // optional rollback: DB save olmadısa upload olunan faylı sil
                    if (!string.IsNullOrWhiteSpace(imageUrl))
                    {
                        _ = await _uploadImage.DeletePhysicalFileIfExists(imageUrl);
                    }

                    return new ServiceResult(false, "There was a problem creating the post!", null);
                }

                // ✅ DTO-nu DB save-dən sonra yarat
                var returnDto = new JobPostDto
                {
                    // Id = jobPost.Id, // varsa əlavə et
                    Title = jobPost.Title,
                    Description = jobPost.Description,
                    CreatedAt = jobPost.CreatedAt,
                    Location = jobPost.Location,
                    Salary = jobPost.Salary,
                    Skills = jobPost.Skills,
                    ImageUrl = jobPost.ImageUrl // ✅ front üçün faydalı
                };

                return new ServiceResult(true, "Post successfully created!", returnDto);
            }
            catch (Exception ex)
            {
                // optional rollback
                if (!string.IsNullOrWhiteSpace(imageUrl))
                {
                    _= await _uploadImage.DeletePhysicalFileIfExists(imageUrl);
                }

                return new ServiceResult(false, $"Error creating post: {ex.Message}", null);
            }
        }

        public async Task<bool> DeleteJobPostAsync(int jobPostId, string userId)
        {
            var jobPost = await _unitOfWork.JobPosts.GetByIdAsync(jobPostId);

            if (jobPost == null || jobPost.EmployerId != userId)
                return false;

            _unitOfWork.JobPosts.Remove(jobPost);

            return await _unitOfWork.CompleteAsync() > 0;

        }

        public async Task<ServiceResult> GetAllJobPostsByUserId(
         string postOwnerId,
         string? currentUserId,
         int page,
         int pageSize)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 10 : pageSize;
            pageSize = pageSize > 50 ? 50 : pageSize;

            var skip = (page - 1) * pageSize;

            var posts = await _unitOfWork.JobPosts.GetJobPostsByUserIdAsync(postOwnerId, skip, pageSize);

            var dtoJobList = posts.Select(post => new JobPostDto
            {
                Title = post.Title,
                Description = post.Description,
                CreatedAt = post.CreatedAt,
                Location = post.Location,
                Salary = post.Salary,
                EmployerName = post.Employer?.Company?.Name,
                EmployerPhoto = post.Employer?.Company?.LogoUrl,
                Role = "Employer",
                Skills = post.Skills,
            }).ToList();

            // ✅ posts boş olsa da success=true qaytara bilərsən
            return new ServiceResult(true,
                dtoJobList.Count > 0 ? "Posts found successfully" : "No posts found",
                dtoJobList);
        }


         


        public async Task<ServiceResult> UpdateJobPostAsync(
        int postId,
        UpdateJobPostDto postDto,
        string userId)
        {
            var jobPost = await _unitOfWork.JobPosts.GetByIdAsync(postId);

            if (jobPost == null)
                return new ServiceResult(false, "Post not found!", null);

            // ✅ Authorization
            if (jobPost.EmployerId != userId)
                return new ServiceResult(false, "You are not allowed to update this post!", null);

            // ✅ 1️⃣ Delete media (image + video)
            if (postDto.DeleteMedia)
            {
                if (!string.IsNullOrEmpty(jobPost.ImageUrl))
                {
                    await _uploadImage.DeletePhysicalFileIfExists(jobPost.ImageUrl);
                    jobPost.ImageUrl = null;
                }

                if (!string.IsNullOrEmpty(jobPost.VideoUrl))
                {
                    await _uploadImage.DeletePhysicalFileIfExists(jobPost.VideoUrl);
                    jobPost.VideoUrl = null;
                }
            }

            // ✅ 2️⃣ Update text fields
            jobPost.Title = postDto.Title?.Trim() ?? jobPost.Title;
            jobPost.Description = postDto.Description?.Trim() ?? jobPost.Description;
            jobPost.Location = postDto.Location?.Trim() ?? jobPost.Location;
            jobPost.Salary = postDto.Salary ?? jobPost.Salary;

            // ✅ 3️⃣ New file upload (override media)
            if (!postDto.DeleteMedia && postDto.File != null && postDto.File.Length > 0)
            {
                // əvvəl köhnə media silinir
                if (!string.IsNullOrEmpty(jobPost.ImageUrl))
                {
                    await _uploadImage.DeletePhysicalFileIfExists(jobPost.ImageUrl);
                    jobPost.ImageUrl = null;
                }

                if (!string.IsNullOrEmpty(jobPost.VideoUrl))
                {
                    await _uploadImage.DeletePhysicalFileIfExists(jobPost.VideoUrl);
                    jobPost.VideoUrl = null;
                }

                var extension = Path.GetExtension(postDto.File.FileName).ToLowerInvariant();
                var isVideo = extension == ".mp4" || extension == ".mov" || extension == ".avi";

                var newUrl = await _uploadImage.UploadFile(postDto.File);

                if (isVideo)
                {
                    jobPost.VideoUrl = newUrl;
                    jobPost.ImageUrl = null;
                }
                else
                {
                    jobPost.ImageUrl = newUrl;
                    jobPost.VideoUrl = null;
                }
            }

            var saved = await _unitOfWork.CompleteAsync() > 0;
            if (!saved)
                return new ServiceResult(false, "There was a problem updating the post!", null);

            return new ServiceResult(true, "Post updated successfully!", new JobPostDto
            {
                Title = jobPost.Title,
                Description = jobPost.Description,
                CreatedAt = jobPost.CreatedAt,
                Location = jobPost.Location,
                Salary = jobPost.Salary,
                Skills = jobPost.Skills,
                ImageUrl = jobPost.ImageUrl,
                VideoUrl = jobPost.VideoUrl
            });
        }
 
    }
}
