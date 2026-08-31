using System.Security.Claims;
using Linkedin.Business.Services.Interface;
using Linkedin.Core.Data;
using Linkedin.Core.Dtos;
using Linkedin.Core.Dtos.Talent;
using Linkedin.Core.Entities;
using Linkedin.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Linkedin.Api.Controllers
{
    [ApiController]
    [Authorize(Roles = "Employer")]
    [Route("api/company/talent")]
    public class CompanyTalentController : ControllerBase
    {
        private static readonly string[] AllowedStatuses =
        {
            "Saved", "Contacted", "Invited"
        };

        private readonly AppDbContext _context;
        private readonly INotificationPublisher _notificationPublisher;

        public CompanyTalentController(
            AppDbContext context,
            INotificationPublisher notificationPublisher)
        {
            _context = context;
            _notificationPublisher = notificationPublisher;
        }

        [HttpGet("discover")]
        public async Task<IActionResult> Discover(
            [FromQuery] string? search = null,
            [FromQuery] string? position = null,
            [FromQuery] string? skills = null,
            [FromQuery] string? location = null,
            [FromQuery] int minimumExperience = 0,
            [FromQuery] string? employmentType = null,
            [FromQuery] string? workplaceType = null,
            [FromQuery] int? jobPostId = null,
            [FromQuery] bool matchOnly = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 12)
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 30);
            minimumExperience = Math.Clamp(minimumExperience, 0, 50);
            var employerId = CurrentUserId();
            var now = DateTime.UtcNow;

            var requestedSkills = Split(skills);
            var job = jobPostId.HasValue
                ? await _context.JobPosts.AsNoTracking()
                    .Where(item =>
                        item.Id == jobPostId &&
                        item.EmployerId == employerId &&
                        item.IsActive &&
                        !item.IsBlocked &&
                        (!item.ExpiresAt.HasValue ||
                         item.ExpiresAt > now))
                    .Select(item => new
                    {
                        item.Id,
                        item.Title,
                        item.Location,
                        item.WorkplaceType,
                        item.EmploymentType,
                        item.RequiredSkills,
                        item.MinimumExperienceYears
                    })
                    .FirstOrDefaultAsync()
                : null;

            if (jobPostId.HasValue && job == null)
                return NotFound("Job post was not found.");
            if (matchOnly && job == null)
                return BadRequest("Select an active job before finding matches.");

            if (job != null)
            {
                if (requestedSkills.Count == 0)
                    requestedSkills = Split(job.RequiredSkills);
                if (minimumExperience == 0)
                    minimumExperience = job.MinimumExperienceYears;
            }

            var hasCustomMatchFilters =
                !string.IsNullOrWhiteSpace(position) ||
                !string.IsNullOrWhiteSpace(skills) ||
                !string.IsNullOrWhiteSpace(location) ||
                !string.IsNullOrWhiteSpace(employmentType) ||
                !string.IsNullOrWhiteSpace(workplaceType) ||
                minimumExperience > 0;
            var companyJobs = new List<CompanyJobProfile>();
            if (job == null && !hasCustomMatchFilters)
            {
                var companyJobRows = await _context.JobPosts.AsNoTracking()
                    .Where(item =>
                        item.EmployerId == employerId &&
                        item.IsActive &&
                        !item.IsBlocked &&
                        (!item.ExpiresAt.HasValue ||
                         item.ExpiresAt > now))
                    .Select(item => new
                    {
                        item.Id,
                        item.Title,
                        item.Location,
                        item.WorkplaceType,
                        item.EmploymentType,
                        item.RequiredSkills,
                        item.MinimumExperienceYears
                    })
                    .ToListAsync();

                companyJobs = companyJobRows
                    .Select(item => new CompanyJobProfile(
                        item.Id,
                        item.Title,
                        item.Location,
                        item.WorkplaceType,
                        item.EmploymentType,
                        item.RequiredSkills,
                        item.MinimumExperienceYears))
                    .ToList();
            }

            var query = _context.Users.AsNoTracking()
                .Where(item =>
                    item.UserType == UserType.JobSeeker &&
                    !item.IsBlocked &&
                    _context.JobPreferences.Any(preference =>
                        preference.UserId == item.Id &&
                        preference.IsOpenToWork));

            if (!string.IsNullOrWhiteSpace(search))
            {
                var value = search.Trim();
                query = query.Where(item =>
                    (item.FullName != null && item.FullName.Contains(value)) ||
                    (item.UserName != null && item.UserName.Contains(value)) ||
                    (item.CurrentPosition != null && item.CurrentPosition.Contains(value)) ||
                    item.Skills.Any(skill => skill.Name.Contains(value)));
            }

            if (!string.IsNullOrWhiteSpace(position))
            {
                var value = position.Trim();
                query = query.Where(item =>
                    item.CurrentPosition != null &&
                    item.CurrentPosition.Contains(value));
            }

            if (!string.IsNullOrWhiteSpace(location))
            {
                var value = location.Trim();
                query = query.Where(item =>
                    (item.Location != null &&
                     item.Location.Contains(value)) ||
                    _context.JobPreferences.Any(preference =>
                        preference.UserId == item.Id &&
                        ((preference.OnsiteLocations != null &&
                          preference.OnsiteLocations.Contains(value)) ||
                         (preference.RemoteLocations != null &&
                          preference.RemoteLocations.Contains(value)))));
            }

            if (requestedSkills.Count > 0)
            {
                query = query.Where(item => item.Skills.Any(skill =>
                    requestedSkills.Contains(skill.Name)));
            }

            if (!string.IsNullOrWhiteSpace(employmentType))
            {
                var value = employmentType.Trim();
                query = query.Where(item =>
                    _context.JobPreferences.Any(preference =>
                        preference.UserId == item.Id &&
                        preference.EmploymentTypes != null &&
                        preference.EmploymentTypes.Contains(value)));
            }

            if (!string.IsNullOrWhiteSpace(workplaceType))
            {
                var value = workplaceType.Trim();
                query = query.Where(item =>
                    _context.JobPreferences.Any(preference =>
                        preference.UserId == item.Id &&
                        preference.WorkplaceTypes != null &&
                        preference.WorkplaceTypes.Contains(value)));
            }

            var candidates = await query
                .Include(item => item.Skills)
                .Include(item => item.Experiences)
                .Include(item => item.Educations)
                .ToListAsync();

            var candidateIds = candidates.Select(item => item.Id).ToList();
            var preferences = await _context.JobPreferences.AsNoTracking()
                .Where(item => candidateIds.Contains(item.UserId))
                .ToDictionaryAsync(item => item.UserId);

            var followerCounts = await _context.Connections.AsNoTracking()
                .Where(item => candidateIds.Contains(item.UserId))
                .GroupBy(item => item.UserId)
                .Select(group => new
                {
                    UserId = group.Key,
                    Count = group.Count()
                })
                .ToDictionaryAsync(item => item.UserId, item => item.Count);

            var saved = await _context.SavedTalents.AsNoTracking()
                .Where(item => item.EmployerId == employerId)
                .ToDictionaryAsync(item => item.CandidateId, item => item.Status);

            var invitations = await _context.JobInvitations.AsNoTracking()
                .Where(item => item.EmployerId == employerId)
                .Select(item => new { item.CandidateId, item.JobPostId })
                .ToListAsync();

            var scored = candidates
                .Select(candidate =>
                {
                    var experienceYears = ExperienceYears(candidate.Experiences);
                    var candidateSkills = candidate.Skills
                        .Select(item => item.Name)
                        .Where(item => !string.IsNullOrWhiteSpace(item))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    var matchedSkills = requestedSkills
                        .Where(skill => candidateSkills.Contains(
                            skill,
                            StringComparer.OrdinalIgnoreCase))
                        .ToList();

                    preferences.TryGetValue(candidate.Id, out var preference);
                    var match = companyJobs.Count > 0
                        ? companyJobs
                            .Select(companyJob =>
                            {
                                var jobSkills = Split(
                                    companyJob.RequiredSkills);
                                var jobMatchedSkills = jobSkills.Count(skill =>
                                    candidateSkills.Contains(
                                        skill,
                                        StringComparer.OrdinalIgnoreCase));
                                return CandidateMatch(
                                    candidate,
                                    preference,
                                    companyJob.Title,
                                    companyJob.Location,
                                    companyJob.WorkplaceType,
                                    companyJob.EmploymentType,
                                    jobSkills,
                                    jobMatchedSkills,
                                    companyJob.MinimumExperienceYears,
                                    experienceYears);
                            })
                            .OrderByDescending(item => item.Score)
                            .First()
                        : CandidateMatch(
                            candidate,
                            preference,
                            job?.Title ?? position,
                            job?.Location ?? location,
                            job?.WorkplaceType ?? workplaceType,
                            job?.EmploymentType ?? employmentType,
                            requestedSkills,
                            matchedSkills.Count,
                            minimumExperience,
                            experienceYears);

                    return new
                    {
                        candidate.Id,
                        username = candidate.UserName,
                        candidate.FullName,
                        candidate.CurrentPosition,
                        candidate.Location,
                        candidate.ProfileImage,
                        followerCount = followerCounts.GetValueOrDefault(candidate.Id),
                        experienceYears,
                        skills = candidateSkills.Take(8),
                        matchedSkills,
                        education = candidate.Educations
                            .OrderByDescending(item => item.EndYear ?? item.StartYear)
                            .Select(item => item.School)
                            .FirstOrDefault(),
                        matchScore = match.Score,
                        matchReasons = match.Reasons,
                        isOpenToWork = preference?.IsOpenToWork == true,
                        jobTitles = Split(preference?.JobTitles),
                        workplaceTypes = Split(preference?.WorkplaceTypes),
                        onsiteLocations = Split(preference?.OnsiteLocations),
                        remoteLocations = Split(preference?.RemoteLocations),
                        employmentTypes = Split(preference?.EmploymentTypes),
                        startAvailability = string.IsNullOrWhiteSpace(
                            preference?.StartAvailability)
                                ? "Immediately"
                                : preference!.StartAvailability,
                        savedStatus = saved.GetValueOrDefault(candidate.Id),
                        isSaved = saved.ContainsKey(candidate.Id),
                        isInvited = invitations.Any(item =>
                            item.CandidateId == candidate.Id &&
                            (!jobPostId.HasValue ||
                             item.JobPostId == jobPostId.Value))
                    };
                })
                .Where(item => item.experienceYears >= minimumExperience)
                .Where(item => !matchOnly || item.matchScore >= 60)
                .OrderByDescending(item => item.matchScore)
                .ThenBy(item => item.FullName)
                .ToList();

            var totalCount = scored.Count;
            var items = scored
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Ok(new
            {
                items,
                page,
                pageSize,
                totalCount,
                totalPages = Math.Max(1,
                    (int)Math.Ceiling(totalCount / (double)pageSize)),
                job,
                matchOnly
            });
        }

        [HttpGet("following")]
        public async Task<IActionResult> Following()
        {
            var employerId = CurrentUserId();
            var items = await _context.CompanyFollows.AsNoTracking()
                .Where(item =>
                    item.FollowerId == employerId &&
                    !item.Employer.IsBlocked &&
                    !_context.UserBlocks.Any(block =>
                        (block.BlockerId == employerId && block.BlockedUserId == item.EmployerId) ||
                        (block.BlockerId == item.EmployerId && block.BlockedUserId == employerId)))
                .OrderByDescending(item => item.CreatedAt)
                .Select(item => new
                {
                    candidateId = item.EmployerId,
                    username = item.Employer.UserName,
                    fullName = item.Employer.UserType == UserType.Employer && item.Employer.Company != null
                        ? item.Employer.Company.Name
                        : item.Employer.FullName,
                    currentPosition = item.Employer.UserType == UserType.Employer && item.Employer.Company != null
                        ? item.Employer.Company.Industry
                        : item.Employer.CurrentPosition,
                    item.Employer.Location,
                    profileImage = item.Employer.UserType == UserType.Employer && item.Employer.Company != null
                        ? item.Employer.Company.LogoUrl ?? item.Employer.ProfileImage
                        : item.Employer.ProfileImage,
                    targetType = item.Employer.UserType == UserType.Employer ? "company" : "member",
                    canInvite = item.Employer.UserType == UserType.JobSeeker,
                    followedAt = item.CreatedAt,
                    followerCount = item.Employer.UserType == UserType.Employer
                        ? _context.CompanyFollows.Count(follow => follow.EmployerId == item.EmployerId)
                        : _context.Connections.Count(connection => connection.UserId == item.EmployerId),
                    skills = item.Employer.Skills.Select(skill => skill.Name).Take(5)
                })
                .ToListAsync();

            return Ok(new { items });
        }

        [HttpGet("followers")]
        public async Task<IActionResult> Followers(
            [FromQuery] string? search = null)
        {
            var employerId = CurrentUserId();
            var query = _context.CompanyFollows.AsNoTracking()
                .Where(item =>
                    item.EmployerId == employerId &&
                    !item.Follower.IsBlocked);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var value = search.Trim();
                query = query.Where(item =>
                    (item.Follower.FullName != null &&
                     item.Follower.FullName.Contains(value)) ||
                    (item.Follower.UserName != null &&
                     item.Follower.UserName.Contains(value)) ||
                    (item.Follower.CurrentPosition != null &&
                     item.Follower.CurrentPosition.Contains(value)));
            }

            var items = await query
                .OrderByDescending(item => item.CreatedAt)
                .Select(item => new
                {
                    id = item.FollowerId,
                    username = item.Follower.UserName,
                    item.Follower.FullName,
                    item.Follower.CurrentPosition,
                    item.Follower.Location,
                    item.Follower.ProfileImage,
                    followedAt = item.CreatedAt,
                    skills = item.Follower.Skills
                        .Select(skill => skill.Name)
                        .Take(5)
                })
                .ToListAsync();

            return Ok(items);
        }

        [HttpGet("employees")]
        public async Task<IActionResult> Employees(
            [FromQuery] string? search = null)
        {
            var employerId = CurrentUserId();
            var companyId = await _context.Users.AsNoTracking()
                .Where(item =>
                    item.Id == employerId &&
                    item.Company != null)
                .Select(item => (int?)item.Company!.Id)
                .FirstOrDefaultAsync();

            if (!companyId.HasValue)
                return Ok(Array.Empty<object>());

            var query = _context.Experience.AsNoTracking()
                .Where(item =>
                    item.CompanyId == companyId &&
                    item.IsCurrent &&
                    !item.User.IsBlocked);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var value = search.Trim();
                query = query.Where(item =>
                    item.Title.Contains(value) ||
                    (item.User.FullName != null &&
                     item.User.FullName.Contains(value)) ||
                    (item.User.UserName != null &&
                     item.User.UserName.Contains(value)));
            }

            var items = await query
                .OrderBy(item => item.User.FullName)
                .Select(item => new
                {
                    id = item.UserId,
                    username = item.User.UserName,
                    item.User.FullName,
                    currentPosition = item.Title,
                    item.User.Location,
                    item.User.ProfileImage,
                    item.StartMonth,
                    item.StartYear,
                    skills = item.User.Skills
                        .Select(skill => skill.Name)
                        .Take(5)
                })
                .ToListAsync();

            return Ok(new
            {
                basedOnMemberProfiles = true,
                items
            });
        }

        [HttpGet("saved")]
        public async Task<IActionResult> Saved(
            [FromQuery] string? status = null)
        {
            var employerId = CurrentUserId();
            var query = _context.SavedTalents.AsNoTracking()
                .Where(item =>
                    item.EmployerId == employerId &&
                    !item.Candidate.IsBlocked);

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(item => item.Status == status);

            var items = await query
                .OrderByDescending(item => item.UpdatedAt)
                .Select(item => new
                {
                    item.Id,
                    candidateId = item.CandidateId,
                    username = item.Candidate.UserName,
                    item.Candidate.FullName,
                    item.Candidate.CurrentPosition,
                    item.Candidate.Location,
                    item.Candidate.ProfileImage,
                    item.Status,
                    item.CreatedAt,
                    item.UpdatedAt,
                    skills = item.Candidate.Skills
                        .Select(skill => skill.Name)
                        .Take(6)
                })
                .ToListAsync();

            return Ok(items);
        }

        [HttpPost("saved")]
        public async Task<IActionResult> Save(
            [FromBody] SaveTalentRequestDto request)
        {
            var employerId = CurrentUserId();
            var candidate = await ValidCandidate(request.CandidateId);
            if (candidate == null)
                return NotFound("Candidate was not found.");

            var existing = await _context.SavedTalents
                .FirstOrDefaultAsync(item =>
                    item.EmployerId == employerId &&
                    item.CandidateId == candidate.Id);

            if (existing == null)
            {
                existing = new SavedTalent
                {
                    EmployerId = employerId,
                    CandidateId = candidate.Id
                };
                _context.SavedTalents.Add(existing);
                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                existing.Id,
                existing.CandidateId,
                existing.Status
            });
        }

        [HttpPatch("saved/{candidateId}/status")]
        public async Task<IActionResult> ChangeStatus(
            string candidateId,
            [FromBody] UpdateSavedTalentStatusDto request)
        {
            var status = AllowedStatuses.FirstOrDefault(item =>
                string.Equals(
                    item,
                    request.Status?.Trim(),
                    StringComparison.OrdinalIgnoreCase));

            if (status == null)
                return BadRequest("Status must be Saved, Contacted or Invited.");

            var employerId = CurrentUserId();
            var saved = await _context.SavedTalents
                .FirstOrDefaultAsync(item =>
                    item.EmployerId == employerId &&
                    item.CandidateId == candidateId);

            if (saved == null)
                return NotFound();

            saved.Status = status;
            saved.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { saved.CandidateId, saved.Status });
        }

        [HttpDelete("saved/{candidateId}")]
        public async Task<IActionResult> RemoveSaved(string candidateId)
        {
            var employerId = CurrentUserId();
            var saved = await _context.SavedTalents
                .FirstOrDefaultAsync(item =>
                    item.EmployerId == employerId &&
                    item.CandidateId == candidateId);

            if (saved == null)
                return NoContent();

            _context.SavedTalents.Remove(saved);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("invite")]
        public async Task<IActionResult> Invite(
            [FromBody] InviteTalentRequestDto request)
        {
            var employerId = CurrentUserId();
            var job = await _context.JobPosts
                .FirstOrDefaultAsync(item =>
                    item.Id == request.JobPostId &&
                    item.EmployerId == employerId &&
                    item.IsActive &&
                    !item.IsBlocked);

            if (job == null)
                return NotFound("Active job post was not found.");

            var candidate = await ValidCandidate(request.CandidateId);
            if (candidate == null)
                return NotFound("Candidate was not found.");

            var exists = await _context.JobInvitations.AnyAsync(item =>
                item.JobPostId == job.Id &&
                item.EmployerId == employerId &&
                item.CandidateId == candidate.Id);

            if (exists)
                return Conflict("This candidate has already been invited.");

            var message = string.IsNullOrWhiteSpace(request.Message)
                ? null
                : request.Message.Trim();
            if (message?.Length > 500)
                return BadRequest("Invitation message cannot exceed 500 characters.");

            var sender = await _context.Users.AsNoTracking()
                .Where(item => item.Id == employerId)
                .Select(item => new
                {
                    Username = item.UserName!,
                    Name = item.Company != null
                        ? item.Company.Name
                        : item.FullName,
                    Photo = item.Company != null
                        ? item.Company.LogoUrl
                        : item.ProfileImage
                })
                .FirstAsync();

            var invitation = new JobInvitation
            {
                JobPostId = job.Id,
                EmployerId = employerId,
                CandidateId = candidate.Id,
                Message = message
            };
            _context.JobInvitations.Add(invitation);

            var saved = await _context.SavedTalents.FirstOrDefaultAsync(item =>
                item.EmployerId == employerId &&
                item.CandidateId == candidate.Id);
            if (saved != null)
            {
                saved.Status = "Invited";
                saved.UpdatedAt = DateTime.UtcNow;
            }

            var notification = new Notification
            {
                SenderId = employerId,
                ReceiverId = candidate.Id,
                Type = NotificationType.JobInvitation,
                JobPostId = job.Id,
                SenderUsername = sender.Username,
                SenderProfilePhoto = sender.Photo,
                ContentPreview = $"invited you to view {job.Title}",
                LastTriggeredAt = DateTime.UtcNow
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            await _notificationPublisher.PublishAsync(
                candidate.Id,
                new NotificationReturnDto
                {
                    Id = notification.Id,
                    SenderId = employerId,
                    ReceiverId = candidate.Id,
                    Type = notification.Type,
                    JobPostId = job.Id,
                    SenderUsername = sender.Username,
                    SenderProfilePhoto = sender.Photo,
                    SenderIsCompany = true,
                    ContentPreview = notification.ContentPreview,
                    CreatedAt = notification.CreatedAt,
                    LastTriggeredAt = notification.LastTriggeredAt,
                    IsRead = false
                });

            return Ok(new
            {
                invitation.Id,
                invitation.JobPostId,
                invitation.CandidateId,
                invitation.CreatedAt
            });
        }

        [HttpGet("invitations")]
        public async Task<IActionResult> Invitations()
        {
            var employerId = CurrentUserId();
            var items = await _context.JobInvitations.AsNoTracking()
                .Where(item => item.EmployerId == employerId)
                .OrderByDescending(item => item.CreatedAt)
                .Select(item => new
                {
                    item.Id,
                    item.JobPostId,
                    jobTitle = item.JobPost.Title,
                    candidateId = item.CandidateId,
                    username = item.Candidate.UserName,
                    item.Candidate.FullName,
                    item.Candidate.CurrentPosition,
                    item.Candidate.ProfileImage,
                    item.Message,
                    item.CreatedAt,
                    item.ViewedAt
                })
                .ToListAsync();
            return Ok(items);
        }

        [HttpDelete("invitations/{invitationId:int}")]
        public async Task<IActionResult> WithdrawInvitation(int invitationId)
        {
            var employerId = CurrentUserId();
            var invitation = await _context.JobInvitations
                .FirstOrDefaultAsync(item =>
                    item.Id == invitationId &&
                    item.EmployerId == employerId);

            if (invitation == null)
                return NotFound("Invitation was not found.");

            var relatedNotifications = await _context.Notifications
                .Where(item =>
                    item.SenderId == employerId &&
                    item.ReceiverId == invitation.CandidateId &&
                    item.JobPostId == invitation.JobPostId &&
                    item.Type == NotificationType.JobInvitation)
                .ToListAsync();

            if (relatedNotifications.Count > 0)
                _context.Notifications.RemoveRange(relatedNotifications);

            _context.JobInvitations.Remove(invitation);

            var saved = await _context.SavedTalents
                .FirstOrDefaultAsync(item =>
                    item.EmployerId == employerId &&
                    item.CandidateId == invitation.CandidateId);

            if (saved != null &&
                string.Equals(
                    saved.Status,
                    "Invited",
                    StringComparison.OrdinalIgnoreCase))
            {
                var hasAnotherInvitation = await _context.JobInvitations
                    .AnyAsync(item =>
                        item.Id != invitation.Id &&
                        item.EmployerId == employerId &&
                        item.CandidateId == invitation.CandidateId);

                if (!hasAnotherInvitation)
                {
                    saved.Status = "Saved";
                    saved.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("active-jobs")]
        public async Task<IActionResult> ActiveJobs()
        {
            var employerId = CurrentUserId();
            var now = DateTime.UtcNow;
            var jobs = await _context.JobPosts.AsNoTracking()
                .Where(item =>
                    item.EmployerId == employerId &&
                    item.IsActive &&
                    !item.IsBlocked &&
                    (!item.ExpiresAt.HasValue ||
                     item.ExpiresAt > now))
                .OrderByDescending(item => item.CreatedAt)
                .Select(item => new
                {
                    item.Id,
                    item.Title,
                    item.Location,
                    item.RequiredSkills,
                    item.MinimumExperienceYears
                })
                .ToListAsync();
            return Ok(jobs);
        }

        private async Task<ApplicationUser?> ValidCandidate(string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            return await _context.Users.AsNoTracking()
                .FirstOrDefaultAsync(item =>
                    item.Id == id &&
                    item.UserType == UserType.JobSeeker &&
                    !item.IsBlocked);
        }

        private string CurrentUserId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User id claim is missing.");

        private static List<string> Split(string? value) =>
            string.IsNullOrWhiteSpace(value)
                ? new List<string>()
                : value.Split(
                        new[] { ',', '|', ';' },
                        StringSplitOptions.RemoveEmptyEntries |
                        StringSplitOptions.TrimEntries)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(20)
                    .ToList();

        private static int ExperienceYears(IEnumerable<Experience> experiences)
        {
            var months = experiences.Sum(item =>
            {
                if (!item.StartYear.HasValue)
                    return 0;
                var startMonth = Math.Clamp(item.StartMonth ?? 1, 1, 12);
                var endYear = item.IsCurrent
                    ? DateTime.UtcNow.Year
                    : item.EndYear ?? item.StartYear.Value;
                var endMonth = item.IsCurrent
                    ? DateTime.UtcNow.Month
                    : Math.Clamp(item.EndMonth ?? 12, 1, 12);
                return Math.Max(
                    0,
                    ((endYear - item.StartYear.Value) * 12) +
                    endMonth -
                    startMonth +
                    1);
            });
            return (int)Math.Round(months / 12d, MidpointRounding.AwayFromZero);
        }

        private static CandidateMatchResult CandidateMatch(
            ApplicationUser candidate,
            JobPreference? preference,
            string? position,
            string? location,
            string? workplaceType,
            string? employmentType,
            IReadOnlyCollection<string> requestedSkills,
            int matchedSkillCount,
            int minimumExperience,
            int experienceYears)
        {
            double score = 0;
            var reasons = new List<string>();
            var isOpenToWork = preference?.IsOpenToWork == true;

            if (isOpenToWork)
            {
                score += 30;
                reasons.Add("Open to work");
            }

            var preferredTitles = Split(preference?.JobTitles);
            var titleMatched = !string.IsNullOrWhiteSpace(position) &&
                ((candidate.CurrentPosition?.Contains(
                    position,
                    StringComparison.OrdinalIgnoreCase) == true) ||
                 preferredTitles.Any(title =>
                    position.Contains(
                        title,
                        StringComparison.OrdinalIgnoreCase) ||
                    title.Contains(
                        position,
                        StringComparison.OrdinalIgnoreCase)));

            if (titleMatched)
            {
                score += 20;
                reasons.Add("Job title");
            }
            else if (string.IsNullOrWhiteSpace(position) &&
                     (!string.IsNullOrWhiteSpace(candidate.CurrentPosition) ||
                      preferredTitles.Count > 0))
            {
                score += 15;
            }

            if (requestedSkills.Count > 0)
            {
                var skillScore = matchedSkillCount * 25d / requestedSkills.Count;
                score += skillScore;
                if (matchedSkillCount > 0)
                    reasons.Add($"{matchedSkillCount} skill match");
            }
            else if (candidate.Skills.Count > 0)
            {
                score += 15;
            }

            var workplaceMatched =
                !string.IsNullOrWhiteSpace(workplaceType) &&
                Split(preference?.WorkplaceTypes).Contains(
                    workplaceType,
                    StringComparer.OrdinalIgnoreCase);
            if (workplaceMatched)
            {
                score += 10;
                reasons.Add(workplaceType!);
            }

            var employmentMatched =
                !string.IsNullOrWhiteSpace(employmentType) &&
                Split(preference?.EmploymentTypes).Contains(
                    employmentType,
                    StringComparer.OrdinalIgnoreCase);
            if (employmentMatched)
            {
                score += 10;
                reasons.Add(employmentType!);
            }

            if (!string.IsNullOrWhiteSpace(location))
            {
                var preferredLocations = string.Equals(
                        workplaceType,
                        "Remote",
                        StringComparison.OrdinalIgnoreCase)
                    ? Split(preference?.RemoteLocations)
                    : Split(preference?.OnsiteLocations);
                var locationMatched =
                    candidate.Location?.Contains(
                        location,
                        StringComparison.OrdinalIgnoreCase) == true ||
                    preferredLocations.Any(item =>
                        item.Contains(
                            location,
                            StringComparison.OrdinalIgnoreCase) ||
                        location.Contains(
                            item,
                            StringComparison.OrdinalIgnoreCase));
                if (locationMatched)
                {
                    score += 10;
                    reasons.Add("Location");
                }
            }
            else if (!string.IsNullOrWhiteSpace(candidate.Location))
            {
                score += 10;
            }

            if (minimumExperience <= 0 || experienceYears >= minimumExperience)
            {
                score += 10;
                if (minimumExperience > 0)
                    reasons.Add("Experience");
            }

            return new CandidateMatchResult(
                Math.Clamp((int)Math.Round(score), 0, 100),
                reasons.Distinct(StringComparer.OrdinalIgnoreCase).Take(4).ToList());
        }

        private sealed record CandidateMatchResult(
            int Score,
            List<string> Reasons);

        private sealed record CompanyJobProfile(
            int Id,
            string Title,
            string? Location,
            string WorkplaceType,
            string EmploymentType,
            string? RequiredSkills,
            int MinimumExperienceYears);
    }
}
