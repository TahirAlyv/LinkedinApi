using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Dtos
{
    public class RecommendedUserDto
    {
        public string Id { get; set; } = null!;

        public string? Username { get; set; }

        public string? FullName { get; set; }

        public string? CurrentPosition { get; set; }

        public string? ProfileImage { get; set; }

        public string? Location { get; set; }

        public string? UserType { get; set; }

        public string? CompanyName { get; set; }

        public string? CompanyLogo { get; set; }

        public int Score { get; set; }

        public int MutualConnectionsCount { get; set; }

        public int CommonSkillsCount { get; set; }

        public string? RecommendationReason { get; set; }

        public bool IsConnected { get; set; }

        public string? ConnectionStatus { get; set; }

        public int? RequestId { get; set; }

        public bool IsFollowing { get; set; }
    }
}
