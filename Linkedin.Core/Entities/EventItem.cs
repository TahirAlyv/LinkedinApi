namespace Linkedin.Core.Entities
{
    public class EventItem
    {
        public int Id { get; set; }
        public string EmployerId { get; set; } = string.Empty;
        public ApplicationUser Employer { get; set; } = null!;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Topics { get; set; }
        public string? Location { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime StartsAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<EventAttendance> Attendees { get; set; } = new List<EventAttendance>();
    }
}
