namespace Linkedin.Core.Entities
{
    public class SavedTalent
    {
        public int Id { get; set; }
        public string EmployerId { get; set; } = null!;
        public ApplicationUser Employer { get; set; } = null!;
        public string CandidateId { get; set; } = null!;
        public ApplicationUser Candidate { get; set; } = null!;
        public string Status { get; set; } = "Saved";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
