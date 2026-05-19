using Linkedin.Core.Enums;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace Linkedin.Core.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }

        public UserType UserType { get; set; } = UserType.JobSeeker;
        public Visibility Visibility { get; set; } = Visibility.Public;

        public string? CurrentPosition { get; set; }
        public string? ProfileImage { get; set; }
        public string? BackgroundImage { get; set; }
        public string? Location { get; set; }

        public PhoneType? PhoneType { get; set; }

        // IdentityUser-da PhoneNumber onsuz da var.
        // Amma səndə əvvəl də var idi deyə saxlaya bilərsən.
        // Əgər warning/conflict versə, bunu sil.
        public string? Address { get; set; }
        public string? Website { get; set; }

        public int? BirthMonth { get; set; }
        public int? BirthDay { get; set; }

        public string? Bio { get; set; }

        public Company? Company { get; set; }

        // =========================
        // PROFILE SECTIONS
        // =========================

        public ICollection<UserSkill> Skills { get; set; } = new List<UserSkill>();
        public ICollection<Experience> Experiences { get; set; } = new List<Experience>();
        public ICollection<Education> Educations { get; set; } = new List<Education>();

        // =========================
        // CONNECTION SYSTEM
        // =========================

        public ICollection<ConnectionRequest> SentConnectionRequests { get; set; } = new List<ConnectionRequest>();
        public ICollection<ConnectionRequest> ReceivedConnectionRequests { get; set; } = new List<ConnectionRequest>();

        public ICollection<Connection> Connections { get; set; } = new List<Connection>();
        public ICollection<Connection> ConnectedUsers { get; set; } = new List<Connection>();

        // =========================
        // JOBS
        // =========================

        public ICollection<JobApplication> JobApplications { get; set; } = new List<JobApplication>();
        public ICollection<JobPost> JobPosts { get; set; } = new List<JobPost>();

        // =========================
        // POSTS
        // =========================

        public ICollection<Post> Posts { get; set; } = new List<Post>();

        // =========================
        // CHATS / MESSAGES
        // =========================

        public virtual ICollection<Chat> SentChats { get; set; } = new List<Chat>();
        public virtual ICollection<Chat> ReceivedChats { get; set; } = new List<Chat>();
        public ICollection<Message> Messages { get; set; } = new List<Message>();

        // =========================
        // NOTIFICATIONS
        // =========================

        public ICollection<Notification> SentNotifications { get; set; } = new List<Notification>();
        public ICollection<Notification> ReceivedNotifications { get; set; } = new List<Notification>();
    }
}