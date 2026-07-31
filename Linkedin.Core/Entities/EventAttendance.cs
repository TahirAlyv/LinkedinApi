namespace Linkedin.Core.Entities
{
    public class EventAttendance
    {
        public int Id { get; set; }
        public int EventItemId { get; set; }
        public EventItem EventItem { get; set; } = null!;
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
