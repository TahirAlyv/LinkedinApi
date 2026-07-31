namespace Linkedin.Core.Dtos.Profile.Read
{
    public class OrganizationOptionDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Username { get; set; }
        public string? LogoUrl { get; set; }
        public string? Industry { get; set; }
    }
}
