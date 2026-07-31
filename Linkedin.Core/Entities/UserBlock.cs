namespace Linkedin.Core.Entities
{
    public class UserBlock
    {
        public int Id { get; set; }
        public string BlockerId { get; set; } = null!;
        public ApplicationUser Blocker { get; set; } = null!;
        public string BlockedUserId { get; set; } = null!;
        public ApplicationUser BlockedUser { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
