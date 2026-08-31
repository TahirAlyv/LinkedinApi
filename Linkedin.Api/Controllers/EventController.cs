using System.Security.Claims;
using Linkedin.Business.Services.Interface;
using Linkedin.Core.Data;
using Linkedin.Core.Dtos;
using Linkedin.Core.Entities;
using Linkedin.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Linkedin.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class EventController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IUploadImage _uploadImage;
        private readonly IEventNotificationService _eventNotifications;
        private readonly INotficationsService _notificationsService;

        public EventController(
            AppDbContext context,
            IUploadImage uploadImage,
            IEventNotificationService eventNotifications,
            INotficationsService notificationsService)
        {
            _context = context;
            _uploadImage = uploadImage;
            _eventNotifications = eventNotifications;
            _notificationsService = notificationsService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? query = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 6,
            [FromQuery] bool upcoming = false,
            [FromQuery] bool recommended = false,
            [FromQuery] bool mine = false,
            [FromQuery] string? username = null)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 20);
            var currentUserId = CurrentUserId();
            var now = DateTime.UtcNow;

            var eventQuery = _context.Events
                .AsNoTracking()
                .AsQueryable();

            if (upcoming)
                eventQuery = eventQuery.Where(item => item.StartsAt >= now);

            if (mine)
            {
                eventQuery = eventQuery.Where(item =>
                    item.EmployerId == currentUserId);
            }
            else if (!string.IsNullOrWhiteSpace(username))
            {
                var organizerUsername = username.Trim();
                eventQuery = eventQuery.Where(item =>
                    item.Employer.UserName == organizerUsername);
            }

            if (!string.IsNullOrWhiteSpace(query))
            {
                var term = query.Trim();
                eventQuery = eventQuery.Where(item =>
                    item.Title.Contains(term) ||
                    (item.Description != null &&
                     item.Description.Contains(term)) ||
                    (item.Topics != null &&
                     item.Topics.Contains(term)) ||
                    (item.Location != null &&
                     item.Location.Contains(term)));
            }

            var totalCount = await eventQuery.CountAsync();

            eventQuery = recommended
                ? eventQuery
                    .OrderByDescending(item =>
                        _context.CompanyFollows.Any(follow =>
                            follow.FollowerId == currentUserId &&
                            follow.EmployerId == item.EmployerId))
                    .ThenByDescending(item => item.Attendees.Count)
                    .ThenBy(item => item.StartsAt)
                : eventQuery.OrderBy(item => item.StartsAt);

            var items = await eventQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(item => new
                {
                    item.Id,
                    item.Title,
                    item.Description,
                    item.Topics,
                    item.Location,
                    item.ImageUrl,
                    item.EventUrl,
                    item.StartsAt,
                    Username = item.Employer.UserName,
                    OrganizerName =
                        item.Employer.Company != null
                            ? item.Employer.Company.Name
                            : item.Employer.FullName,
                    OrganizerImage =
                        item.Employer.Company != null
                            ? item.Employer.Company.LogoUrl
                            : item.Employer.ProfileImage,
                    AttendeeCount = item.Attendees.Count,
                    IsAttending = item.Attendees.Any(attendee =>
                        attendee.UserId == currentUserId),
                    IsOwner = item.EmployerId == currentUserId
                })
                .ToListAsync();

            var totalPages = Math.Max(
                1,
                (int)Math.Ceiling(totalCount / (double)pageSize));

            return Ok(new
            {
                items,
                page,
                pageSize,
                totalCount,
                totalPages,
                hasMore = page < totalPages
            });
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var currentUserId = CurrentUserId();

            var item = await _context.Events
                .AsNoTracking()
                .Where(eventItem => eventItem.Id == id)
                .Select(eventItem => new
                {
                    eventItem.Id,
                    eventItem.Title,
                    eventItem.Description,
                    eventItem.Topics,
                    eventItem.Location,
                    eventItem.ImageUrl,
                    eventItem.EventUrl,
                    eventItem.StartsAt,
                    eventItem.CreatedAt,
                    OrganizerId = eventItem.EmployerId,
                    Username = eventItem.Employer.UserName,
                    OrganizerName =
                        eventItem.Employer.Company != null
                            ? eventItem.Employer.Company.Name
                            : eventItem.Employer.FullName,
                    OrganizerImage =
                        eventItem.Employer.Company != null
                            ? eventItem.Employer.Company.LogoUrl
                            : eventItem.Employer.ProfileImage,
                    AttendeeCount = eventItem.Attendees.Count,
                    IsAttending = eventItem.Attendees.Any(attendee =>
                        attendee.UserId == currentUserId),
                    IsOwner = eventItem.EmployerId == currentUserId
                })
                .FirstOrDefaultAsync();

            return item == null
                ? NotFound("Event was not found.")
                : Ok(item);
        }

        [HttpGet("upcoming")]
        public async Task<IActionResult> GetUpcoming(
            [FromQuery] int take = 5,
            [FromQuery] string? username = null)
        {
            var currentUserId = CurrentUserId();
            var query = _context.Events
                .AsNoTracking()
                .Where(item => item.StartsAt >= DateTime.UtcNow);

            if (!string.IsNullOrWhiteSpace(username))
            {
                var organizerUsername = username.Trim();
                query = query.Where(item =>
                    item.Employer.UserName == organizerUsername);
            }

            var items = await query
                .OrderBy(item => item.StartsAt)
                .Take(Math.Clamp(take, 1, 20))
                .Select(item => new
                {
                    item.Id,
                    item.Title,
                    item.Description,
                    item.Topics,
                    item.Location,
                    item.ImageUrl,
                    item.EventUrl,
                    item.StartsAt,
                    Username = item.Employer.UserName,
                    OrganizerName =
                        item.Employer.Company != null
                            ? item.Employer.Company.Name
                            : item.Employer.FullName,
                    OrganizerImage =
                        item.Employer.Company != null
                            ? item.Employer.Company.LogoUrl
                            : item.Employer.ProfileImage,
                    AttendeeCount = item.Attendees.Count,
                    IsAttending = item.Attendees.Any(attendee =>
                        attendee.UserId == currentUserId),
                    IsOwner = item.EmployerId == currentUserId
                })
                .ToListAsync();

            return Ok(items);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromForm] CreateEventDto dto)
        {
            if (dto.StartsAt <= DateTime.UtcNow)
                return BadRequest("Event date must be in the future.");

            if (!TryNormalizeEventUrl(dto.EventUrl, out var eventUrl))
                return BadRequest("Enter a valid event link starting with http:// or https://.");

            var organizerId = CurrentUserId();
            var isEmployer = await _context.Users.AsNoTracking()
                .AnyAsync(item =>
                    item.Id == organizerId &&
                    item.UserType == UserType.Employer &&
                    !item.IsBlocked);

            if (!isEmployer)
                return Forbid();

            var imageUrl = await UploadImage(dto.Image);

            if (dto.Image != null && imageUrl == null)
                return BadRequest("The event image is invalid or too large.");

            var item = new EventItem
            {
                EmployerId = organizerId,
                Title = dto.Title.Trim(),
                Description = Clean(dto.Description),
                Topics = Clean(dto.Topics),
                Location = Clean(dto.Location),
                ImageUrl = imageUrl,
                EventUrl = eventUrl,
                StartsAt = dto.StartsAt.ToUniversalTime()
            };

            _context.Events.Add(item);
            await _context.SaveChangesAsync();

            await _eventNotifications.NotifyMatchingUsersAsync(
                item.Id,
                organizerId);

            return CreatedAtAction(
                nameof(GetById),
                new { id = item.Id },
                new { item.Id });
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
            int id,
            [FromForm] CreateEventDto dto)
        {
            if (dto.StartsAt <= DateTime.UtcNow)
                return BadRequest("Event date must be in the future.");

            if (!TryNormalizeEventUrl(dto.EventUrl, out var eventUrl))
                return BadRequest("Enter a valid event link starting with http:// or https://.");

            var userId = CurrentUserId();
            var item = await _context.Events
                .FirstOrDefaultAsync(eventItem => eventItem.Id == id);

            if (item == null)
                return NotFound("Event was not found.");

            if (item.EmployerId != userId)
                return Forbid();

            if (dto.Image != null)
            {
                var imageUrl = await UploadImage(dto.Image);

                if (imageUrl == null)
                    return BadRequest(
                        "The event image is invalid or too large.");

                item.ImageUrl = imageUrl;
            }

            item.Title = dto.Title.Trim();
            item.Description = Clean(dto.Description);
            item.Topics = Clean(dto.Topics);
            item.Location = Clean(dto.Location);
            item.EventUrl = eventUrl;
            item.StartsAt = dto.StartsAt.ToUniversalTime();

            await _context.SaveChangesAsync();

            return Ok(new { item.Id });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = CurrentUserId();
            var item = await _context.Events
                .FirstOrDefaultAsync(eventItem => eventItem.Id == id);

            if (item == null)
                return NotFound("Event was not found.");

            if (item.EmployerId != userId)
                return Forbid();

            var notifications = await _context.Notifications
                .Where(notification => notification.EventId == id)
                .ToListAsync();

            _context.Notifications.RemoveRange(notifications);
            _context.Events.Remove(item);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("{id:int}/attend")]
        public async Task<IActionResult> Attend(int id)
        {
            var userId = CurrentUserId();
            var eventInfo = await _context.Events.AsNoTracking()
                .Where(item =>
                    item.Id == id &&
                    item.StartsAt >= DateTime.UtcNow)
                .Select(item => new
                {
                    item.EmployerId,
                    item.Title
                })
                .FirstOrDefaultAsync();

            if (eventInfo == null)
                return BadRequest(
                    "This event has ended or was not found.");

            var exists = await _context.EventAttendances.AnyAsync(item =>
                item.EventItemId == id &&
                item.UserId == userId);

            if (!exists)
            {
                _context.EventAttendances.Add(new EventAttendance
                {
                    EventItemId = id,
                    UserId = userId
                });

                await _context.SaveChangesAsync();

                if (eventInfo.EmployerId != userId)
                {
                    var attendee = await _context.Users.AsNoTracking()
                        .Where(item => item.Id == userId)
                        .Select(item => new
                        {
                            Username = item.UserName,
                            item.FullName,
                            item.ProfileImage
                        })
                        .FirstAsync();

                    await _notificationsService.CreateOrUpdateAsync(
                        senderId: userId,
                        receiverId: eventInfo.EmployerId,
                        type: NotificationType.EventAttendance,
                        postId: null,
                        contentPreview: $"joined your event: {eventInfo.Title}",
                        senderUsername:
                            attendee.Username ??
                            attendee.FullName ??
                            "Member",
                        senderProfilePhoto: attendee.ProfileImage ?? "",
                        eventId: id);
                }

                // A newly attending connection can raise this event's social
                // relevance score for other users, so evaluate it again.
                await _eventNotifications.NotifyMatchingUsersAsync(
                    id,
                    eventInfo.EmployerId);
            }

            var attendeeCount = await _context.EventAttendances.CountAsync(
                item => item.EventItemId == id);

            return Ok(new { attendeeCount });
        }

        [HttpGet("{id:int}/attendees")]
        public async Task<IActionResult> GetAttendees(
            int id,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 50);
            var userId = CurrentUserId();

            var eventInfo = await _context.Events.AsNoTracking()
                .Where(item => item.Id == id)
                .Select(item => new
                {
                    item.EmployerId,
                    item.Title
                })
                .FirstOrDefaultAsync();

            if (eventInfo == null)
                return NotFound("Event was not found.");

            if (eventInfo.EmployerId != userId)
                return Forbid();

            var query = _context.EventAttendances.AsNoTracking()
                .Where(item => item.EventItemId == id)
                .OrderByDescending(item => item.CreatedAt);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(item => new
                {
                    item.UserId,
                    username = item.User.UserName,
                    item.User.FullName,
                    item.User.CurrentPosition,
                    profileImage = item.User.ProfileImage,
                    joinedAt = item.CreatedAt
                })
                .ToListAsync();

            var totalPages = Math.Max(
                1,
                (int)Math.Ceiling(totalCount / (double)pageSize));

            return Ok(new
            {
                eventId = id,
                eventTitle = eventInfo.Title,
                items,
                page,
                pageSize,
                totalCount,
                totalPages,
                hasMore = page < totalPages
            });
        }

        [HttpDelete("{id:int}/attend")]
        public async Task<IActionResult> Leave(int id)
        {
            var userId = CurrentUserId();
            var attendance = await _context.EventAttendances
                .FirstOrDefaultAsync(item =>
                    item.EventItemId == id &&
                    item.UserId == userId);

            if (attendance != null)
            {
                _context.EventAttendances.Remove(attendance);
                await _context.SaveChangesAsync();
            }

            return NoContent();
        }

        private string CurrentUserId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        private async Task<string?> UploadImage(IFormFile? image)
        {
            if (image == null)
                return null;

            var result = await _uploadImage.UploadFile(image, "event");
            return string.IsNullOrWhiteSpace(result) ? null : result;
        }

        private static string? Clean(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static bool TryNormalizeEventUrl(
            string? value,
            out string? normalizedUrl)
        {
            normalizedUrl = null;
            if (string.IsNullOrWhiteSpace(value))
                return true;

            var cleanValue = value.Trim();
            if (!Uri.TryCreate(cleanValue, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp &&
                 uri.Scheme != Uri.UriSchemeHttps))
            {
                return false;
            }

            normalizedUrl = uri.AbsoluteUri;
            return true;
        }

    }
}
