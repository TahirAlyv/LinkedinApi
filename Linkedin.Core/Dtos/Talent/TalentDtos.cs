namespace Linkedin.Core.Dtos.Talent
{
    public class SaveTalentRequestDto
    {
        public string CandidateId { get; set; } = null!;
    }

    public class UpdateSavedTalentStatusDto
    {
        public string Status { get; set; } = "Saved";
    }

    public class InviteTalentRequestDto
    {
        public string CandidateId { get; set; } = null!;
        public int JobPostId { get; set; }
        public string? Message { get; set; }
    }
}
