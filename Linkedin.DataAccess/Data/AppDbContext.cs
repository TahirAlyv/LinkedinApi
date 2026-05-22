using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Linkedin.Core.Entities;

namespace Linkedin.Core.Data
{
    public class AppDbContext
        : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<Post> Posts { get; set; }
        public DbSet<Like> Likes { get; set; }
        public DbSet<Comment> Comments { get; set; }

        public DbSet<Chat> Chats { get; set; }
        public DbSet<Message> Messages { get; set; }

        // Connection system
        public DbSet<ConnectionRequest> ConnectionRequests { get; set; }
        public DbSet<Connection> Connections { get; set; }

        // Jobs system
        public DbSet<JobPost> JobPosts { get; set; }
        public DbSet<JobApplication> JobApplications { get; set; }
        public DbSet<SavedJob> SavedJobs { get; set; }

        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        public DbSet<Experience> Experience { get; set; }
        public DbSet<Education> Education { get; set; }
        public DbSet<UserSkill> userSkills { get; set; }

        public DbSet<CompanyFollow> CompanyFollows { get; set; }

        // Admin / Reports
        public DbSet<Report> Reports { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            // 1. Identity + base config
            base.OnModelCreating(builder);

            // 2. Configuration files
            builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

            // =========================
            // NOTIFICATIONS
            // =========================

            builder.Entity<Notification>()
                .HasOne(n => n.Receiver)
                .WithMany(u => u.ReceivedNotifications)
                .HasForeignKey(n => n.ReceiverId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Notification>()
                .HasOne(n => n.Sender)
                .WithMany(u => u.SentNotifications)
                .HasForeignKey(n => n.SenderId)
                .OnDelete(DeleteBehavior.Restrict);


            // =========================
            // CONNECTION REQUESTS
            // =========================

            builder.Entity<ConnectionRequest>()
                .HasOne(cr => cr.Sender)
                .WithMany(u => u.SentConnectionRequests)
                .HasForeignKey(cr => cr.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ConnectionRequest>()
                .HasOne(cr => cr.Receiver)
                .WithMany(u => u.ReceivedConnectionRequests)
                .HasForeignKey(cr => cr.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ConnectionRequest>()
                .HasIndex(cr => new { cr.SenderId, cr.ReceiverId })
                .IsUnique()
                .HasFilter("[Status] = 0");


            // =========================
            // CONNECTIONS
            // =========================

            builder.Entity<Connection>()
                .HasOne(c => c.User)
                .WithMany(u => u.Connections)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Connection>()
                .HasOne(c => c.ConnectedUser)
                .WithMany(u => u.ConnectedUsers)
                .HasForeignKey(c => c.ConnectedUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Connection>()
                .HasIndex(c => new { c.UserId, c.ConnectedUserId })
                .IsUnique();


            // =========================
            // POSTS
            // =========================

            builder.Entity<Post>()
                .HasOne(p => p.User)
                .WithMany(u => u.Posts)
                .HasForeignKey(p => p.UserID)
                .OnDelete(DeleteBehavior.Cascade);


            // =========================
            // COMMENTS
            // =========================

            builder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Comment>()
                .HasOne(c => c.Post)
                .WithMany(p => p.Comments)
                .HasForeignKey(c => c.PostId)
                .OnDelete(DeleteBehavior.Cascade);


            // =========================
            // LIKES
            // =========================

            builder.Entity<Like>()
                .HasOne(l => l.Post)
                .WithMany(p => p.Likes)
                .HasForeignKey(l => l.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Like>()
                .HasOne(l => l.User)
                .WithMany()
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Restrict);


            // =========================
            // MESSAGES / CHATS
            // =========================

            builder.Entity<Message>()
                .HasOne(m => m.Chat)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ChatId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany(u => u.Messages)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Chat>()
                .HasOne(c => c.Sender)
                .WithMany(u => u.SentChats)
                .HasForeignKey(c => c.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Chat>()
                .HasOne(c => c.Receiver)
                .WithMany(u => u.ReceivedChats)
                .HasForeignKey(c => c.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);


            // =========================
            // JOBS
            // =========================

            builder.Entity<JobPost>()
                .HasOne(jp => jp.Employer)
                .WithMany(u => u.JobPosts)
                .HasForeignKey(jp => jp.EmployerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<JobApplication>()
                .HasOne(a => a.Applicant)
                .WithMany(u => u.JobApplications)
                .HasForeignKey(a => a.ApplicantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<JobApplication>()
                .HasOne(a => a.JobPost)
                .WithMany(jp => jp.Applications)
                .HasForeignKey(a => a.JobPostId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<JobApplication>()
                .HasIndex(a => new { a.ApplicantId, a.JobPostId })
                .IsUnique();

            builder.Entity<SavedJob>()
                .HasOne(s => s.User)
                .WithMany(u => u.SavedJobs)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SavedJob>()
                .HasOne(s => s.JobPost)
                .WithMany(jp => jp.SavedJobs)
                .HasForeignKey(s => s.JobPostId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<SavedJob>()
                .HasIndex(s => new { s.UserId, s.JobPostId })
                .IsUnique();


            // =========================
            // PROFILE SECTIONS
            // =========================

            builder.Entity<Experience>()
                .HasOne(ex => ex.User)
                .WithMany(u => u.Experiences)
                .HasForeignKey(ex => ex.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Education>()
                .HasOne(ed => ed.User)
                .WithMany(u => u.Educations)
                .HasForeignKey(ed => ed.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserSkill>()
                .HasOne(us => us.User)
                .WithMany(u => u.Skills)
                .HasForeignKey(us => us.UserId)
                .OnDelete(DeleteBehavior.Cascade);


            // =========================
            // COMPANY
            // =========================

            builder.Entity<ApplicationUser>()
                .HasOne(u => u.Company)
                .WithOne(c => c.User)
                .HasForeignKey<Company>(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);


            // =========================
            // COMPANY FOLLOW
            // =========================

            builder.Entity<CompanyFollow>()
                .HasOne(cf => cf.Follower)
                .WithMany()
                .HasForeignKey(cf => cf.FollowerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<CompanyFollow>()
                .HasOne(cf => cf.Employer)
                .WithMany()
                .HasForeignKey(cf => cf.EmployerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<CompanyFollow>()
                .HasIndex(cf => new { cf.FollowerId, cf.EmployerId })
                .IsUnique();


            // =========================
            // REPORTS
            // =========================

            builder.Entity<Report>()
                .HasOne(r => r.Reporter)
                .WithMany()
                .HasForeignKey(r => r.ReporterId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Report>()
                .HasOne(r => r.Post)
                .WithMany()
                .HasForeignKey(r => r.PostId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}