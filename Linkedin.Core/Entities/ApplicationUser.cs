
using LinkedIn.Core.Enums;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinkedIn.Core.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
        public string? ProfileImage { get; set; }
        public string? Bio { get; set; }
        public string? Skills { get; set; }
        public string? Experience { get; set; }
        public string? CompanyName { get; set; }
        public string? Industry { get; set; }
        public ICollection<FollowRequest> SentFollowRequests { get; set; }
        public ICollection<FollowRequest> ReceivedFollowRequests { get; set; }
        public ICollection<Follow> Followers { get; set; }
        public ICollection<Follow> Following { get; set; }
        public ICollection<JobApplication> JobApplications { get; set; }
        public ICollection<JobPost> JobPosts { get; set; }
        public ICollection<Post> Posts { get; set; }
        public virtual ICollection<Chat> SentChats { get; set; }
        public virtual ICollection<Chat> ReceivedChats { get; set; }
        public Visibility Visibility { get; set; } = Visibility.Public;
    }
}
