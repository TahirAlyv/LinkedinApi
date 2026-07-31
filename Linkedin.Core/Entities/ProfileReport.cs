namespace Linkedin.Core.Entities
{
    public class ProfileReport
    {
        public int Id { get; set; }

        public string ReporterId { get; set; } = null!;
        public ApplicationUser Reporter { get; set; } = null!;

        public string ReportedUserId { get; set; } = null!;
        public ApplicationUser ReportedUser { get; set; } = null!;

        public string Category { get; set; } = null!;
        public string? Details { get; set; }
        public int Severity { get; set; }
        public bool IsReviewed { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedAt { get; set; }
    }
}
