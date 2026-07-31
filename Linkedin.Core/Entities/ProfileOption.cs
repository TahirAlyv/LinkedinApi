using Linkedin.Core.Enums;

namespace Linkedin.Core.Entities
{
    public class ProfileOption
    {
        public int Id { get; set; }
        public ProfileOptionType Type { get; set; }
        public string Name { get; set; } = null!;
        public string NormalizedName { get; set; } = null!;
        public bool IsApproved { get; set; } = true;
        public string? CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
