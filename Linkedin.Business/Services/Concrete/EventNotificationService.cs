using System.Text.RegularExpressions;
using Linkedin.Business.Services.Interface;
using Linkedin.Core.Data;
using Linkedin.Core.Dtos;
using Linkedin.Core.Entities;
using Linkedin.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace Linkedin.Business.Services.Concrete
{
    public class EventNotificationService : IEventNotificationService
    {
        private const int NotificationScoreThreshold = 6;

        private static readonly HashSet<string> IgnoredWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "and", "the", "for", "with", "from", "your", "our", "event", "meetup",
            "conference", "workshop", "specialist", "manager", "developer", "engineer"
        };

        private readonly AppDbContext _context;
        private readonly INotificationPublisher _publisher;

        public EventNotificationService(AppDbContext context, INotificationPublisher publisher)
        {
            _context = context;
            _publisher = publisher;
        }

        // This method is intentionally called both when an event is created and when a new
        // attendee joins. The second call lets social proof unlock a notification later.
        public async Task NotifyMatchingUsersAsync(int eventId, string organizerId)
        {
            var eventItem = await _context.Events
                .AsNoTracking()
                .Include(item => item.Employer)
                    .ThenInclude(user => user.Company)
                .FirstOrDefaultAsync(item => item.Id == eventId);

            if (eventItem == null || eventItem.StartsAt <= DateTime.UtcNow)
                return;

            var attendeeIds = await _context.EventAttendances
                .AsNoTracking()
                .Where(item => item.EventItemId == eventId)
                .Select(item => item.UserId)
                .ToListAsync();

            var attendeeSet = attendeeIds.ToHashSet(StringComparer.Ordinal);

            var connectedToOrganizer = await _context.Connections
                .AsNoTracking()
                .Where(item => item.UserId == organizerId || item.ConnectedUserId == organizerId)
                .Select(item => item.UserId == organizerId ? item.ConnectedUserId : item.UserId)
                .Distinct()
                .ToListAsync();

            var organizerConnectionSet = connectedToOrganizer.ToHashSet(StringComparer.Ordinal);

            var companyFollowerSet = new HashSet<string>(StringComparer.Ordinal);
            if (eventItem.Employer.UserType == UserType.Employer)
            {
                var followerIds = await _context.CompanyFollows
                    .AsNoTracking()
                    .Where(item => item.EmployerId == organizerId)
                    .Select(item => item.FollowerId)
                    .ToListAsync();

                companyFollowerSet.UnionWith(followerIds);
            }

            var attendeeFriendCounts = await GetAttendeeFriendCountsAsync(attendeeSet);

            var alreadyNotified = await _context.Notifications
                .AsNoTracking()
                .Where(item => item.EventId == eventId && item.Type == NotificationType.Event)
                .Select(item => item.ReceiverId)
                .ToListAsync();

            var notifiedSet = alreadyNotified.ToHashSet(StringComparer.Ordinal);
            var candidates = await _context.Users
                .AsNoTracking()
                .Include(user => user.Skills)
                .Where(user => !user.IsBlocked && user.Id != organizerId && !attendeeSet.Contains(user.Id))
                .ToListAsync();

            // Keep the event logic consistent with the existing recommendations: a
            // user's recent searches are an interest signal, but never enough on
            // their own to produce a notification.
            var recentSearchesByUser = await GetRecentSearchesByUserAsync(
                candidates.Select(user => user.Id).ToList());

            var notifications = new List<Notification>();

            foreach (var candidate in candidates)
            {
                if (notifiedSet.Contains(candidate.Id))
                    continue;

                var isDirectSocialMatch = organizerConnectionSet.Contains(candidate.Id) ||
                                          companyFollowerSet.Contains(candidate.Id);

                var score = CalculateScore(
                    eventItem,
                    candidate,
                    organizerConnectionSet.Contains(candidate.Id),
                    companyFollowerSet.Contains(candidate.Id),
                    attendeeFriendCounts.GetValueOrDefault(candidate.Id),
                    recentSearchesByUser.GetValueOrDefault(candidate.Id) ?? []);

                // Following the organizer's company or being connected to the
                // organizer is an explicit relationship, so notify immediately.
                // All other candidates must reach the relevance threshold.
                if (!isDirectSocialMatch && score < NotificationScoreThreshold)
                    continue;

                notifications.Add(new Notification
                {
                    SenderId = organizerId,
                    ReceiverId = candidate.Id,
                    Type = NotificationType.Event,
                    EventId = eventId,
                    SenderUsername = eventItem.Employer.UserName ?? "Nexora",
                    SenderProfilePhoto = eventItem.Employer.Company?.LogoUrl ?? eventItem.Employer.ProfileImage,
                    ContentPreview = $"Recommended event: {Cut(eventItem.Title, 120)}",
                    CreatedAt = DateTime.UtcNow,
                    LastTriggeredAt = DateTime.UtcNow,
                    IsRead = false
                });
            }

            if (notifications.Count == 0)
                return;

            await _context.Notifications.AddRangeAsync(notifications);
            await _context.SaveChangesAsync();

            foreach (var notification in notifications)
                await _publisher.PublishAsync(notification.ReceiverId, ToDto(notification));
        }

        private async Task<Dictionary<string, int>> GetAttendeeFriendCountsAsync(HashSet<string> attendeeIds)
        {
            if (attendeeIds.Count == 0)
                return new Dictionary<string, int>(StringComparer.Ordinal);

            var connections = await _context.Connections
                .AsNoTracking()
                .Where(connection => attendeeIds.Contains(connection.UserId) || attendeeIds.Contains(connection.ConnectedUserId))
                .Select(connection => new { connection.UserId, connection.ConnectedUserId })
                .ToListAsync();

            var result = new Dictionary<string, int>(StringComparer.Ordinal);

            foreach (var connection in connections)
            {
                var userIsAttendee = attendeeIds.Contains(connection.UserId);
                var connectedUserIsAttendee = attendeeIds.Contains(connection.ConnectedUserId);

                if (userIsAttendee && !connectedUserIsAttendee)
                    result[connection.ConnectedUserId] = result.GetValueOrDefault(connection.ConnectedUserId) + 1;

                if (connectedUserIsAttendee && !userIsAttendee)
                    result[connection.UserId] = result.GetValueOrDefault(connection.UserId) + 1;
            }

            // A connection is stored in both directions in this project, so each friend was
            // counted twice above. Keep the actual number of friends who joined the event.
            return result.ToDictionary(item => item.Key, item => (item.Value + 1) / 2, StringComparer.Ordinal);
        }

        private async Task<Dictionary<string, List<string>>> GetRecentSearchesByUserAsync(
            List<string> userIds)
        {
            if (userIds.Count == 0)
                return new Dictionary<string, List<string>>(StringComparer.Ordinal);

            var searches = await _context.SearchHistories
                .AsNoTracking()
                .Where(item => userIds.Contains(item.UserId))
                .OrderByDescending(item => item.CreatedAt)
                .Select(item => new { item.UserId, item.NormalizedQuery })
                .ToListAsync();

            return searches
                .GroupBy(item => item.UserId, StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .Take(10)
                        .Select(item => item.NormalizedQuery)
                        .Where(query => !string.IsNullOrWhiteSpace(query))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList(),
                    StringComparer.Ordinal);
        }

        private static int CalculateScore(
            EventItem eventItem,
            ApplicationUser user,
            bool isConnectedToOrganizer,
            bool followsOrganizerCompany,
            int attendingFriendCount,
            IReadOnlyCollection<string> recentSearches)
        {
            var title = Normalize(eventItem.Title);
            var topics = Normalize(eventItem.Topics);
            var searchable = Normalize($"{eventItem.Title} {eventItem.Description} {eventItem.Topics}");
            var score = 0;

            // Direct social relationship: these users should always be notified.
            if (isConnectedToOrganizer)
                score += 10;

            if (followsOrganizerCompany)
                score += 10;

            // Exact/near exact position-title match, e.g. Software Developer <-> Software Developer.
            var position = Normalize(user.CurrentPosition);
            if (position.Length >= 3)
            {
                if (title.Contains(position) || position.Contains(title))
                    score += 8;
                else if (searchable.Contains(position))
                    score += 5;
            }

            // Topic-to-skill matching, e.g. event topics ".NET, React" and user skill ".NET".
            foreach (var skill in user.Skills
                         .Select(item => Normalize(item.Name))
                         .Where(item => item.Length >= 2)
                         .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (topics.Contains(skill))
                    score += 6;
                else if (title.Contains(skill))
                    score += 5;
                else if (searchable.Contains(skill))
                    score += 3;
            }

            var eventWords = Tokenize(searchable)
                .Where(word => word.Length >= 3 && !IgnoredWords.Contains(word))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var profileWords = Tokenize($"{user.CurrentPosition} {string.Join(' ', user.Skills.Select(item => item.Name))}")
                .Where(word => word.Length >= 3 && !IgnoredWords.Contains(word))
                .Distinct(StringComparer.OrdinalIgnoreCase);

            score += Math.Min(4, profileWords.Count(eventWords.Contains));

            // Search history follows the same "last 10 searches" approach used
            // by user/post recommendations. Cap it at four points, so a search
            // alone cannot meet the six-point notification threshold.
            var searchMatches = recentSearches
                .Select(Normalize)
                .Where(query => query.Length >= 2)
                .Count(query => searchable.Contains(query) ||
                                Tokenize(query)
                                    .Where(word => word.Length >= 3 && !IgnoredWords.Contains(word))
                                    .Any(eventWords.Contains));

            score += Math.Min(searchMatches, 2) * 2;

            // A single friend is not enough. Two or more friends make the event relevant later.
            score += Math.Min(attendingFriendCount, 3) * 3;

            var eventLocation = Normalize(eventItem.Location);
            var userLocation = Normalize(user.Location);
            if (!string.IsNullOrWhiteSpace(eventLocation) &&
                !eventLocation.Contains("online") &&
                !eventLocation.Contains("remote") &&
                !eventLocation.Contains("virtual") &&
                !string.IsNullOrWhiteSpace(userLocation) &&
                (eventLocation.Contains(userLocation) || userLocation.Contains(eventLocation)))
            {
                score += 1;
            }

            return score;
        }

        private static IEnumerable<string> Tokenize(string? value) =>
            Regex.Split(value ?? string.Empty, @"[^\p{L}\p{N}]+")
                .Where(part => !string.IsNullOrWhiteSpace(part));

        private static string Normalize(string? value) => (value ?? string.Empty).Trim().ToLowerInvariant();

        private static string Cut(string value, int maxLength) => value.Length <= maxLength ? value : value[..maxLength] + "...";

        private static NotificationReturnDto ToDto(Notification item) => new()
        {
            Id = item.Id,
            SenderId = item.SenderId,
            ReceiverId = item.ReceiverId,
            Type = item.Type,
            EventId = item.EventId,
            SenderUsername = item.SenderUsername,
            SenderProfilePhoto = item.SenderProfilePhoto,
            SenderIsCompany = true,
            ContentPreview = item.ContentPreview,
            CreatedAt = item.CreatedAt,
            LastTriggeredAt = item.LastTriggeredAt,
            IsRead = item.IsRead
        };
    }
}
