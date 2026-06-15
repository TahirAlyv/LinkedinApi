using Linkedin.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Dtos
{
    public class SearchedUserDto
    {
        public string Id { get; set; } = null!;

        public string? Username { get; set; }

        public string? FullName { get; set; }

        public string? CurrentPosition { get; set; }

        public string? ProfileImage { get; set; }

        public string? Bio { get; set; }

        public string? Location { get; set; }

        public string? Visibility { get; set; }

        public string? UserType { get; set; }

        public string? Role { get; set; }

        public string? CompanyName { get; set; }

        public string? CompanyLogo { get; set; }

        public string? CompanyIndustry { get; set; }
        public bool IsFollowing { get; set; } // Employer company follow üçün

        public bool IsConnected { get; set; } // JobSeeker connection üçün

        public string? ConnectionStatus { get; set; } // none, connected, pending_sent, pending_received

        public int? RequestId { get; set; }
    }
}
