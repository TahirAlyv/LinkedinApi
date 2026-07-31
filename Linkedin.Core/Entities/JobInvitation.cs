namespace Linkedin.Core.Entities
{
    public class JobInvitation
    {
        public int Id { get; set; }
        public int JobPostId { get; set; }
        public JobPost JobPost { get; set; } = null!;
        public string EmployerId { get; set; } = null!;
        public ApplicationUser Employer { get; set; } = null!;
        public string CandidateId { get; set; } = null!;
        public ApplicationUser Candidate { get; set; } = null!;
        public string? Message { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ViewedAt { get; set; }
    }
}
