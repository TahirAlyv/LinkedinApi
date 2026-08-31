using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Linkedin.Core.Data;
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
    public class AnalyticsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AnalyticsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("track/post-view/{postId:int}")]
        public async Task<IActionResult> TrackPostView(
            int postId,
            [FromQuery] string? source = null,
            [FromQuery] string? query = null)
        {
            var viewerId = CurrentUserId();
            var post = await _context.Posts.AsNoTracking()
                .Where(item => item.Id == postId && !item.IsBlocked)
                .Select(item => new { item.Id, item.UserID })
                .FirstOrDefaultAsync();

            if (post == null) return NotFound();
            if (post.UserID == viewerId) return NoContent();

            var normalizedSource = Normalize(source) == "search" ? "search" : "feed";
            var normalizedQuery = Normalize(query);
            var rawContext = normalizedSource == "search" && normalizedQuery.Length >= 2
                ? $"search:{normalizedQuery}"
                : normalizedSource;
            var context = rawContext[..Math.Min(150, rawContext.Length)];

            await AddIfUniqueAsync(
                AnalyticsEventType.PostView,
                viewerId,
                post.UserID,
                post.Id,
                context,
                TimeSpan.FromMinutes(30));

            return NoContent();
        }

        [HttpPost("track/profile-view")]
        public async Task<IActionResult> TrackProfileView([FromBody] TrackProfileViewRequest request)
        {
            var viewerId = CurrentUserId();
            var username = request.Username?.Trim();
            if (string.IsNullOrWhiteSpace(username)) return BadRequest("Username is required.");

            var target = await _context.Users.AsNoTracking()
                .Where(item => item.UserName == username && !item.IsBlocked)
                .Select(item => new { item.Id })
                .FirstOrDefaultAsync();

            if (target == null) return NotFound();
            if (target.Id == viewerId) return NoContent();

            await AddIfUniqueAsync(
                AnalyticsEventType.ProfileView,
                viewerId,
                target.Id,
                null,
                null,
                TimeSpan.FromHours(6));

            return NoContent();
        }

        [HttpPost("track/search-appearances")]
        public async Task<IActionResult> TrackSearchAppearances([FromBody] TrackSearchAppearancesRequest request)
        {
            var viewerId = CurrentUserId();
            var query = Normalize(request.Query);
            if (query.Length < 2 || request.Usernames == null || request.Usernames.Count == 0)
                return NoContent();

            var usernames = request.Usernames
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim().ToLower())
                .Distinct()
                .Take(20)
                .ToList();

            var targets = await _context.Users.AsNoTracking()
                .Where(item =>
                    item.Id != viewerId &&
                    item.UserName != null &&
                    usernames.Contains(item.UserName.ToLower()) &&
                    !item.IsBlocked)
                .Select(item => item.Id)
                .ToListAsync();

            foreach (var targetId in targets)
            {
                await AddIfUniqueAsync(
                    AnalyticsEventType.SearchAppearance,
                    viewerId,
                    targetId,
                    null,
                    query,
                    TimeSpan.FromHours(6));
            }

            return NoContent();
        }

        [HttpGet("overview")]
        public async Task<IActionResult> Overview()
        {
            var userId = CurrentUserId();
            var user = await _context.Users.AsNoTracking()
                .Where(item => item.Id == userId)
                .Select(item => new
                {
                    item.UserType,
                    DisplayName = item.Company != null ? item.Company.Name : item.FullName,
                    Username = item.UserName,
                    Image = item.Company != null
                        ? item.Company.LogoUrl ?? item.ProfileImage
                        : item.ProfileImage
                })
                .FirstOrDefaultAsync();

            if (user == null) return Unauthorized();

            var now = DateTime.UtcNow;
            var postViews = await EventComparison(userId, AnalyticsEventType.PostView, now, 7);
            var profileViews = await EventComparison(userId, AnalyticsEventType.ProfileView, now, 90);
            var searchAppearances = await EventComparison(userId, AnalyticsEventType.SearchAppearance, now, 7);
            var audience = await AudienceSnapshot(userId, user.UserType, now, 30);

            return Ok(new
            {
                account = new
                {
                    user.DisplayName,
                    user.Username,
                    user.Image,
                    isEmployer = user.UserType == UserType.Employer
                },
                cards = new[]
                {
                    Metric("Post views", postViews.Current, postViews.Previous, "Last 7 days"),
                    Metric("Profile views", profileViews.Current, profileViews.Previous, "Last 90 days"),
                    Metric("Search appearances", searchAppearances.Current, searchAppearances.Previous, "Last 7 days"),
                    Metric(
                        user.UserType == UserType.Employer ? "Followers" : "Connections",
                        audience.Total,
                        audience.PreviousTotal,
                        "Total audience")
                }
            });
        }

        [HttpGet("content")]
        public async Task<IActionResult> Content([FromQuery] int days = 30)
        {
            days = days is 7 or 30 or 90 ? days : 30;
            var userId = CurrentUserId();
            var from = DateTime.UtcNow.Date.AddDays(-(days - 1));

            var posts = await _context.Posts.AsNoTracking()
                .Where(item => item.UserID == userId && !item.IsBlocked)
                .Select(item => new
                {
                    item.Id,
                    item.Content,
                    item.CreatedAt,
                    item.ImageUrl,
                    item.VideoUrl
                })
                .ToListAsync();

            var allPostIds = await _context.Posts.AsNoTracking()
                .Where(item => item.UserID == userId && !item.IsBlocked)
                .Select(item => item.Id)
                .ToListAsync();

            var views = await _context.Set<AnalyticsEvent>().AsNoTracking()
                .Where(item =>
                    item.TargetUserId == userId &&
                    item.EventType == AnalyticsEventType.PostView &&
                    item.CreatedAt >= from)
                .Select(item => new { item.PostId, item.CreatedAt })
                .ToListAsync();

            var likes = await _context.Likes.AsNoTracking()
                .Where(item => allPostIds.Contains(item.PostId) && item.isLiked && item.CreatedAt >= from)
                .Select(item => new { item.PostId, item.CreatedAt })
                .ToListAsync();

            var comments = await _context.Comments.AsNoTracking()
                .Where(item => allPostIds.Contains(item.PostId) && item.CreatedAt >= from)
                .Select(item => new { item.PostId, item.CreatedAt })
                .ToListAsync();

            var saves = await _context.SavedPosts.AsNoTracking()
                .Where(item => allPostIds.Contains(item.PostId) && item.SavedAt >= from)
                .Select(item => new { item.PostId, CreatedAt = item.SavedAt })
                .ToListAsync();

            var chart = Enumerable.Range(0, days).Select(index =>
            {
                var date = from.AddDays(index);
                var next = date.AddDays(1);
                return new
                {
                    date = date.ToString("yyyy-MM-dd"),
                    views = views.Count(item => item.CreatedAt >= date && item.CreatedAt < next),
                    engagements =
                        likes.Count(item => item.CreatedAt >= date && item.CreatedAt < next) +
                        comments.Count(item => item.CreatedAt >= date && item.CreatedAt < next) +
                        saves.Count(item => item.CreatedAt >= date && item.CreatedAt < next)
                };
            }).ToList();

            var topPosts = posts.Select(post =>
            {
                var postViews = views.Count(item => item.PostId == post.Id);
                var postLikes = likes.Count(item => item.PostId == post.Id);
                var postComments = comments.Count(item => item.PostId == post.Id);
                var postSaves = saves.Count(item => item.PostId == post.Id);
                var engagement = postLikes + postComments + postSaves;
                return new
                {
                    post.Id,
                    content = Preview(post.Content),
                    post.CreatedAt,
                    post.ImageUrl,
                    hasVideo = !string.IsNullOrWhiteSpace(post.VideoUrl),
                    views = postViews,
                    reactions = postLikes,
                    comments = postComments,
                    saves = postSaves,
                    engagementRate = postViews == 0 ? 0 : Math.Round(engagement * 100d / postViews, 1)
                };
            })
            .OrderByDescending(item => item.views)
            .ThenByDescending(item => item.reactions + item.comments + item.saves)
            .Take(3)
            .ToList();

            return Ok(new
            {
                periodDays = days,
                totals = new
                {
                    postViews = views.Count,
                    reactions = likes.Count,
                    comments = comments.Count,
                    saves = saves.Count,
                    engagementRate = views.Count == 0
                        ? 0
                        : Math.Round((likes.Count + comments.Count + saves.Count) * 100d / views.Count, 1)
                },
                chart,
                topPosts
            });
        }

        [HttpGet("audience")]
        public async Task<IActionResult> Audience()
        {
            var userId = CurrentUserId();
            var userType = await _context.Users.AsNoTracking()
                .Where(item => item.Id == userId)
                .Select(item => item.UserType)
                .FirstOrDefaultAsync();

            var from = DateTime.UtcNow.Date.AddDays(-29);
            List<AudienceMember> members;

            if (userType == UserType.Employer)
            {
                members = await _context.CompanyFollows.AsNoTracking()
                    .Where(item => item.EmployerId == userId)
                    .Select(item => new AudienceMember(item.FollowerId, item.CreatedAt))
                    .ToListAsync();
            }
            else
            {
                var connections = await _context.Connections.AsNoTracking()
                    .Where(item => item.UserId == userId || item.ConnectedUserId == userId)
                    .Select(item => new
                    {
                        OtherId = item.UserId == userId ? item.ConnectedUserId : item.UserId,
                        item.ConnectedAt
                    })
                    .ToListAsync();

                members = connections
                    .GroupBy(item => item.OtherId)
                    .Select(group => new AudienceMember(group.Key, group.Min(item => item.ConnectedAt)))
                    .ToList();
            }

            var memberIds = members.Select(item => item.UserId).Distinct().ToList();
            var profiles = await _context.Users.AsNoTracking()
                .Where(item => memberIds.Contains(item.Id))
                .Select(item => new { item.Id, item.Location, item.CurrentPosition })
                .ToListAsync();
            var skills = await _context.userSkills.AsNoTracking()
                .Where(item => memberIds.Contains(item.UserId))
                .Select(item => new { item.UserId, item.Name })
                .ToListAsync();

            var chart = Enumerable.Range(0, 30).Select(index =>
            {
                var date = from.AddDays(index);
                return new
                {
                    date = date.ToString("yyyy-MM-dd"),
                    total = members.Count(item => item.CreatedAt.Date <= date)
                };
            }).ToList();

            return Ok(new
            {
                title = userType == UserType.Employer ? "Follower growth" : "Connection growth",
                total = memberIds.Count,
                newThisPeriod = members.Count(item => item.CreatedAt >= from),
                chart,
                locations = TopGroups(profiles.Select(item => item.Location), memberIds.Count),
                positions = TopGroups(profiles.Select(item => item.CurrentPosition), memberIds.Count),
                skills = TopSkillGroups(
                    skills.Select(item => new AudienceSkill(
                        item.UserId,
                        item.Name)),
                    memberIds.Count)
            });
        }

        private string CurrentUserId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User id claim is missing.");

        private async Task AddIfUniqueAsync(
            AnalyticsEventType eventType,
            string viewerId,
            string targetId,
            int? postId,
            string? searchQuery,
            TimeSpan window)
        {
            var since = DateTime.UtcNow.Subtract(window);
            var exists = await _context.Set<AnalyticsEvent>().AsNoTracking().AnyAsync(item =>
                item.EventType == eventType &&
                item.ViewerUserId == viewerId &&
                item.TargetUserId == targetId &&
                item.PostId == postId &&
                (eventType != AnalyticsEventType.SearchAppearance || item.SearchQuery == searchQuery) &&
                item.CreatedAt >= since);

            if (exists) return;

            _context.Set<AnalyticsEvent>().Add(new AnalyticsEvent
            {
                EventType = eventType,
                ViewerUserId = viewerId,
                TargetUserId = targetId,
                PostId = postId,
                SearchQuery = searchQuery
            });
            await _context.SaveChangesAsync();
        }

        private async Task<(int Current, int Previous)> EventComparison(
            string userId,
            AnalyticsEventType type,
            DateTime now,
            int days)
        {
            var start = now.AddDays(-days);
            var previousStart = start.AddDays(-days);
            var dates = await _context.Set<AnalyticsEvent>().AsNoTracking()
                .Where(item =>
                    item.TargetUserId == userId &&
                    item.EventType == type &&
                    item.CreatedAt >= previousStart)
                .Select(item => item.CreatedAt)
                .ToListAsync();
            return (dates.Count(date => date >= start), dates.Count(date => date < start));
        }

        private async Task<(int Total, int PreviousTotal)> AudienceSnapshot(
            string userId,
            UserType userType,
            DateTime now,
            int days)
        {
            var boundary = now.AddDays(-days);
            if (userType == UserType.Employer)
            {
                var dates = await _context.CompanyFollows.AsNoTracking()
                    .Where(item => item.EmployerId == userId)
                    .Select(item => item.CreatedAt)
                    .ToListAsync();
                return (dates.Count, dates.Count(date => date < boundary));
            }

            var rows = await _context.Connections.AsNoTracking()
                .Where(item => item.UserId == userId || item.ConnectedUserId == userId)
                .Select(item => new
                {
                    OtherId = item.UserId == userId ? item.ConnectedUserId : item.UserId,
                    item.ConnectedAt
                })
                .ToListAsync();
            var firstDates = rows.GroupBy(item => item.OtherId).Select(group => group.Min(item => item.ConnectedAt)).ToList();
            return (firstDates.Count, firstDates.Count(date => date < boundary));
        }

        private static object Metric(string label, int current, int previous, string period)
        {
            double? change = previous == 0
                ? current == 0 ? 0 : null
                : Math.Round((current - previous) * 100d / previous, 1);
            return new { label, value = current, previousValue = previous, changePercent = change, period };
        }

        private static List<object> TopGroups(IEnumerable<string?> values, int audienceTotal) =>
            values
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!.Trim())
                .GroupBy(value => value, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(group => group.Count())
                .ThenBy(group => group.Key)
                .Take(3)
                .Select(group => (object)new
                {
                    label = group.Key,
                    count = group.Count(),
                    percentage = audienceTotal == 0 ? 0 : Math.Round(group.Count() * 100d / audienceTotal, 1)
                })
                .ToList();

        private static List<object> TopSkillGroups(
            IEnumerable<AudienceSkill> values,
            int audienceTotal) =>
            values
                .Where(value => !string.IsNullOrWhiteSpace(value.Name))
                .GroupBy(
                    value => value.Name.Trim(),
                    StringComparer.OrdinalIgnoreCase)
                .Select(group => new
                {
                    Label = group.Key,
                    Count = group
                        .Select(item => item.UserId)
                        .Distinct()
                        .Count()
                })
                .OrderByDescending(item => item.Count)
                .ThenBy(item => item.Label)
                .Take(3)
                .Select(item => (object)new
                {
                    label = item.Label,
                    count = item.Count,
                    percentage = audienceTotal == 0
                        ? 0
                        : Math.Round(item.Count * 100d / audienceTotal, 1)
                })
                .ToList();

        private static string Normalize(string? value) =>
            (value ?? string.Empty).Trim().ToLowerInvariant();

        private static string Preview(string? content)
        {
            var value = string.IsNullOrWhiteSpace(content) ? "Media post" : content.Trim();
            return value.Length <= 120 ? value : value[..117] + "...";
        }

        public sealed class TrackProfileViewRequest
        {
            public string? Username { get; set; }
        }

        public sealed class TrackSearchAppearancesRequest
        {
            public string? Query { get; set; }
            public List<string> Usernames { get; set; } = new();
        }

        private sealed record AudienceMember(string UserId, DateTime CreatedAt);
        private sealed record AudienceSkill(string UserId, string Name);
    }
}
