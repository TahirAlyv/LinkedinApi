using System;

namespace Linkedin.Core.Entities
{
    public class JobPreference
    {
        public int Id { get; set; }
        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;

        // Values are stored with a pipe separator and exposed as arrays by the API.
        public string? JobTitles { get; set; }
        public string? Locations { get; set; }
        public string? WorkplaceTypes { get; set; }
        public string? EmploymentTypes { get; set; }
        public bool IsOpenToWork { get; set; }
        public string? OnsiteLocations { get; set; }
        public string? RemoteLocations { get; set; }
        public string? StartAvailability { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
