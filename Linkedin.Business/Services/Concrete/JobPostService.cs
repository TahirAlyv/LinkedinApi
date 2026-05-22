using Linkedin.Business.Services.Interface;
using Linkedin.Core.Common;
using Linkedin.Core.Dtos;
using Linkedin.Core.Dtos.JobPost.Read;
using Linkedin.Core.Entities;
using Linkedin.Core.Enums;
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

        private static readonly string[] AllowedWorkplaceTypes =
        {
            "On-site", "Remote", "Hybrid"
        };

        private static readonly string[] AllowedEmploymentTypes =
        {
            "Full-time", "Part-time", "Internship", "Contract"
        };

        public JobPostService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ServiceResult> GetAllJobPostsAsync(string? currentUserId, int page, int pageSize, string? query)
        {
            NormalizePagination(ref page, ref pageSize);

            var skip = (page - 1) * pageSize;
            var jobs = await _unitOfWork.JobPosts.GetAllJobPostsAsync(skip, pageSize, query);

            var dtoList = new List<JobPostDto>();

            foreach (var job in jobs)
                dtoList.Add(await MapToDtoAsync(job, currentUserId));

            return new ServiceResult(true, "Job posts loaded successfully.", dtoList);
        }

        public async Task<ServiceResult> GetJobPostByIdAsync(int id, string? currentUserId)
        {
            var job = await _unitOfWork.JobPosts.GetJobPostDetailsAsync(id);

            if (job == null)
                return new ServiceResult(false, "Job post not found.", null);

            return new ServiceResult(true, "Job post loaded successfully.", await MapToDtoAsync(job, currentUserId));
        }

        public async Task<ServiceResult> GetMyJobPostsAsync(string employerId, int page, int pageSize)
        {
            NormalizePagination(ref page, ref pageSize);

            var skip = (page - 1) * pageSize;
            var jobs = await _unitOfWork.JobPosts.GetMyJobPostsAsync(employerId, skip, pageSize);

            var dtoList = new List<JobPostDto>();

            foreach (var job in jobs)
                dtoList.Add(await MapToDtoAsync(job, employerId));

            return new ServiceResult(true, "My job posts loaded successfully.", dtoList);
        }

        public async Task<ServiceResult> GetJobPostsByEmployerUsernameAsync(
            string username,
            string? currentUserId,
            int page,
            int pageSize)
        {
            if (string.IsNullOrWhiteSpace(username))
                return new ServiceResult(false, "Username is required.", null);

            NormalizePagination(ref page, ref pageSize);

            var skip = (page - 1) * pageSize;
            var jobs = await _unitOfWork.JobPosts.GetJobPostsByEmployerUsernameAsync(username.Trim(), skip, pageSize);

            var dtoList = new List<JobPostDto>();

            foreach (var job in jobs)
                dtoList.Add(await MapToDtoAsync(job, currentUserId));

            return new ServiceResult(true, "Company job posts loaded successfully.", dtoList);
        }

        public async Task<ServiceResult> CreateJobPostAsync(CreateJobPostDto dto, string employerId)
        {
            var validation = await ValidateEmployerAndInputAsync(
                employerId,
                dto.Title,
                dto.Description,
                dto.WorkplaceType,
                dto.EmploymentType,
                dto.ApplyUrl,
                dto.ExpiresAt);

            if (!validation.Success)
                return validation;

            var jobPost = new JobPost
            {
                EmployerId = employerId,
                Title = dto.Title.Trim(),
                Description = dto.Description.Trim(),
                Location = dto.Location?.Trim(),
                WorkplaceType = NormalizeValue(dto.WorkplaceType, "On-site"),
                EmploymentType = NormalizeValue(dto.EmploymentType, "Full-time"),
                ApplyUrl = NormalizeNullable(dto.ApplyUrl),
                ExpiresAt = dto.ExpiresAt,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.JobPosts.AddAsync(jobPost);

            var saved = await _unitOfWork.CompleteAsync() > 0;

            if (!saved)
                return new ServiceResult(false, "There was a problem creating the job post.", null);

            var createdJob = await _unitOfWork.JobPosts.GetJobPostDetailsAsync(jobPost.Id);

            return new ServiceResult(true, "Job post created successfully.", await MapToDtoAsync(createdJob!, employerId));
        }

        public async Task<ServiceResult> UpdateJobPostAsync(int id, UpdateJobPostDto dto, string employerId)
        {
            var jobPost = await _unitOfWork.JobPosts.GetJobPostDetailsAsync(id);

            if (jobPost == null)
                return new ServiceResult(false, "Job post not found.", null);

            if (jobPost.EmployerId != employerId)
                return new ServiceResult(false, "You are not allowed to update this job post.", null);

            var validation = await ValidateEmployerAndInputAsync(
                employerId,
                dto.Title,
                dto.Description,
                dto.WorkplaceType,
                dto.EmploymentType,
                dto.ApplyUrl,
                dto.ExpiresAt);

            if (!validation.Success)
                return validation;

            jobPost.Title = dto.Title.Trim();
            jobPost.Description = dto.Description.Trim();
            jobPost.Location = dto.Location?.Trim();
            jobPost.WorkplaceType = NormalizeValue(dto.WorkplaceType, "On-site");
            jobPost.EmploymentType = NormalizeValue(dto.EmploymentType, "Full-time");
            jobPost.ApplyUrl = NormalizeNullable(dto.ApplyUrl);
            jobPost.ExpiresAt = dto.ExpiresAt;
            jobPost.IsActive = dto.IsActive;
            jobPost.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.JobPosts.Update(jobPost);

            var saved = await _unitOfWork.CompleteAsync() > 0;

            if (!saved)
                return new ServiceResult(false, "There was a problem updating the job post.", null);

            var updatedJob = await _unitOfWork.JobPosts.GetJobPostDetailsAsync(jobPost.Id);

            return new ServiceResult(true, "Job post updated successfully.", await MapToDtoAsync(updatedJob!, employerId));
        }

        public async Task<ServiceResult> DeleteJobPostAsync(int id, string employerId)
        {
            var jobPost = await _unitOfWork.JobPosts.GetByIdAsync(id);

            if (jobPost == null)
                return new ServiceResult(false, "Job post not found.", null);

            if (jobPost.EmployerId != employerId)
                return new ServiceResult(false, "You are not allowed to delete this job post.", null);

            _unitOfWork.JobPosts.Remove(jobPost);

            var saved = await _unitOfWork.CompleteAsync() > 0;

            if (!saved)
                return new ServiceResult(false, "There was a problem deleting the job post.", null);

            return new ServiceResult(true, "Job post deleted successfully.", id);
        }

        public async Task<ServiceResult> SaveJobAsync(int jobPostId, string userId)
        {
            var job = await _unitOfWork.JobPosts.GetByIdAsync(jobPostId);

            if (job == null)
                return new ServiceResult(false, "Job post not found.", null);

            var existing = await _unitOfWork.SavedJobs.GetByUserAndJobAsync(userId, jobPostId);

            if (existing != null)
                return new ServiceResult(true, "Job is already saved.", jobPostId);

            var savedJob = new SavedJob
            {
                UserId = userId,
                JobPostId = jobPostId,
                SavedAt = DateTime.UtcNow
            };

            await _unitOfWork.SavedJobs.AddAsync(savedJob);

            var saved = await _unitOfWork.CompleteAsync() > 0;

            if (!saved)
                return new ServiceResult(false, "There was a problem saving the job.", null);

            return new ServiceResult(true, "Job saved successfully.", jobPostId);
        }

        public async Task<ServiceResult> UnsaveJobAsync(int jobPostId, string userId)
        {
            var savedJob = await _unitOfWork.SavedJobs.GetByUserAndJobAsync(userId, jobPostId);

            if (savedJob == null)
                return new ServiceResult(false, "Saved job not found.", null);

            _unitOfWork.SavedJobs.Remove(savedJob);

            var saved = await _unitOfWork.CompleteAsync() > 0;

            if (!saved)
                return new ServiceResult(false, "There was a problem removing the saved job.", null);

            return new ServiceResult(true, "Job removed from saved jobs.", jobPostId);
        }

        public async Task<ServiceResult> GetSavedJobsAsync(string userId, int page, int pageSize)
        {
            NormalizePagination(ref page, ref pageSize);

            var skip = (page - 1) * pageSize;
            var savedJobs = await _unitOfWork.SavedJobs.GetSavedJobsByUserIdAsync(userId, skip, pageSize);

            var dtoList = new List<JobPostDto>();

            foreach (var savedJob in savedJobs)
                dtoList.Add(await MapToDtoAsync(savedJob.JobPost, userId));

            return new ServiceResult(true, "Saved jobs loaded successfully.", dtoList);
        }

        public async Task<ServiceResult> ApplyToJobAsync(int jobPostId, string userId)
        {
            var job = await _unitOfWork.JobPosts.GetJobPostDetailsAsync(jobPostId);

            if (job == null)
                return new ServiceResult(false, "Job post not found.", null);

            var now = DateTime.UtcNow;
            var isExpired = job.ExpiresAt.HasValue && job.ExpiresAt.Value <= now;
            var hasApplyUrl = !string.IsNullOrWhiteSpace(job.ApplyUrl);

            if (!job.IsActive || isExpired)
                return new ServiceResult(false, "Applications are no longer accepted for this job.", null);

            if (!hasApplyUrl)
                return new ServiceResult(false, "Application link is not available.", null);

            var existing = await _unitOfWork.JobApplications.GetByUserAndJobAsync(userId, jobPostId);

            if (existing == null)
            {
                var application = new JobApplication
                {
                    ApplicantId = userId,
                    JobPostId = jobPostId,
                    AppliedAt = DateTime.UtcNow
                };

                await _unitOfWork.JobApplications.AddAsync(application);

                var saved = await _unitOfWork.CompleteAsync() > 0;

                if (!saved)
                    return new ServiceResult(false, "There was a problem saving your application.", null);
            }

            return new ServiceResult(true, "Application recorded successfully.", job.ApplyUrl);
        }

        public async Task<ServiceResult> GetAppliedJobsAsync(string userId, int page, int pageSize)
        {
            NormalizePagination(ref page, ref pageSize);

            var skip = (page - 1) * pageSize;
            var applications = await _unitOfWork.JobApplications.GetAppliedJobsByUserIdAsync(userId, skip, pageSize);

            var dtoList = new List<JobPostDto>();

            foreach (var application in applications)
                dtoList.Add(await MapToDtoAsync(application.JobPost, userId));

            return new ServiceResult(true, "Applied jobs loaded successfully.", dtoList);
        }

        private async Task<ServiceResult> ValidateEmployerAndInputAsync(
            string employerId,
            string title,
            string description,
            string workplaceType,
            string employmentType,
            string? applyUrl,
            DateTime? expiresAt)
        {
            var employer = await _unitOfWork.Users.GetByIdAsync(employerId);

            if (employer == null)
                return new ServiceResult(false, "Employer not found.", null);

            if (employer.UserType != UserType.Employer)
                return new ServiceResult(false, "Only employers can manage job posts.", null);

            if (string.IsNullOrWhiteSpace(title))
                return new ServiceResult(false, "Job title is required.", null);

            if (title.Trim().Length > 150)
                return new ServiceResult(false, "Job title cannot exceed 150 characters.", null);

            if (string.IsNullOrWhiteSpace(description))
                return new ServiceResult(false, "Job description is required.", null);

            if (description.Trim().Length > 3000)
                return new ServiceResult(false, "Job description cannot exceed 3000 characters.", null);

            if (!AllowedWorkplaceTypes.Contains(NormalizeValue(workplaceType, "On-site")))
                return new ServiceResult(false, "Invalid workplace type.", null);

            if (!AllowedEmploymentTypes.Contains(NormalizeValue(employmentType, "Full-time")))
                return new ServiceResult(false, "Invalid employment type.", null);

            if (!string.IsNullOrWhiteSpace(applyUrl) && !IsValidUrl(applyUrl))
                return new ServiceResult(false, "Apply URL must be a valid link.", null);

            if (expiresAt.HasValue && expiresAt.Value <= DateTime.UtcNow)
                return new ServiceResult(false, "Expiration date must be in the future.", null);

            return new ServiceResult(true, "Valid.", null);
        }

        private async Task<JobPostDto> MapToDtoAsync(JobPost job, string? currentUserId)
        {
            var now = DateTime.UtcNow;

            var isExpired = job.ExpiresAt.HasValue && job.ExpiresAt.Value <= now;
            var hasApplyUrl = !string.IsNullOrWhiteSpace(job.ApplyUrl);

            var isSaved = false;
            var isApplied = false;

            if (!string.IsNullOrWhiteSpace(currentUserId))
            {
                isSaved = await _unitOfWork.SavedJobs.IsSavedAsync(currentUserId, job.Id);
                isApplied = await _unitOfWork.JobApplications.IsAppliedAsync(currentUserId, job.Id);
            }

            return new JobPostDto
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

                IsOwner = !string.IsNullOrWhiteSpace(currentUserId) && job.EmployerId == currentUserId,
                IsSaved = isSaved,
                IsApplied = isApplied
            };
        }

        private static void NormalizePagination(ref int page, ref int pageSize)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 10 : pageSize;
            pageSize = pageSize > 50 ? 50 : pageSize;
        }

        private static string NormalizeValue(string? value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
                return fallback;

            return value.Trim();
        }

        private static string? NormalizeNullable(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static bool IsValidUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }

       
    }
}
