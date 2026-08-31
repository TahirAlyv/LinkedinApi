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
        public DbSet<AiRequestLog> AiRequestLogs { get; set; }
        public DbSet<Like> Likes { get; set; }
        public DbSet<Comment> Comments { get; set; }

        public DbSet<Chat> Chats { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<ChatAttachment> ChatAttachments { get; set; }

        // Connection system
        public DbSet<ConnectionRequest> ConnectionRequests { get; set; }
        public DbSet<Connection> Connections { get; set; }

        // Jobs system
        public DbSet<JobPost> JobPosts { get; set; }
        public DbSet<JobApplication> JobApplications { get; set; }
        public DbSet<SavedJob> SavedJobs { get; set; }
        public DbSet<JobPreference> JobPreferences { get; set; }
        public DbSet<SavedTalent> SavedTalents { get; set; }
        public DbSet<JobInvitation> JobInvitations { get; set; }
        public DbSet<SavedPost> SavedPosts { get; set; }
        public DbSet<EventItem> Events { get; set; }
        public DbSet<EventAttendance> EventAttendances { get; set; }

        // Analytics
        public DbSet<AnalyticsEvent> AnalyticsEvents { get; set; }

        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        public DbSet<Experience> Experience { get; set; }
        public DbSet<Education> Education { get; set; }
        public DbSet<UserSkill> userSkills { get; set; }
        public DbSet<ProfileOption> ProfileOptions { get; set; }

        public DbSet<CompanyFollow> CompanyFollows { get; set; }

        // Admin / Reports
        public DbSet<Report> Reports { get; set; }
        public DbSet<ProfileReport> ProfileReports { get; set; }
        public DbSet<UserBlock> UserBlocks { get; set; }
        public DbSet<SearchHistory> SearchHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)


        {
            // 1. Identity + base config
            base.OnModelCreating(builder);

            // 2. Configuration files
            builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
            builder.ApplyConfigurationsFromAssembly(
                typeof(Linkedin.Core.Data.Configurations.AnalyticsEventConfiguration).Assembly
            );

            // =========================
            // AI REQUEST LOGS
            // =========================

            builder.Entity<AiRequestLog>()
                .HasIndex(x => new { x.ProjectKey, x.CreatedAt });

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

            builder.Entity<Post>()
                .HasOne(p => p.MentionedCompany)
                .WithMany()
                .HasForeignKey(p => p.MentionedCompanyId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Post>()
                .HasIndex(p => new { p.MentionedCompanyId, p.CreatedAt });


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

            builder.Entity<Like>()
                .HasIndex(l => new { l.PostId, l.UserId })
                .IsUnique();


            // =========================
            // MESSAGES / CHATS
            // =========================

            builder.Entity<Message>()
                .HasOne(message => message.Chat)
                .WithMany(chat => chat.Messages)
                .HasForeignKey(message => message.ChatId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Message>()
                .HasOne(message => message.Sender)
                .WithMany(user => user.Messages)
                .HasForeignKey(message => message.SenderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ChatAttachment>(entity =>
            {
                entity.HasOne(attachment => attachment.Message)
                    .WithMany(message => message.Attachments)
                    .HasForeignKey(attachment => attachment.MessageId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(attachment => attachment.Url)
                    .HasMaxLength(2048)
                    .IsRequired();

                entity.Property(attachment => attachment.PublicId)
                    .HasMaxLength(500)
                    .IsRequired();

                entity.Property(attachment => attachment.ResourceType)
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(attachment => attachment.OriginalFileName)
                    .HasMaxLength(255)
                    .IsRequired();

                entity.Property(attachment => attachment.ContentType)
                    .HasMaxLength(150)
                    .IsRequired();
            });


            builder.Entity<Chat>()
             .HasOne(chat => chat.Sender)
             .WithMany(user => user.SentChats)
             .HasForeignKey(chat => chat.SenderId)
             .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Chat>()
                .HasOne(chat => chat.Receiver)
                .WithMany(user => user.ReceivedChats)
                .HasForeignKey(chat => chat.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Chat>()
                .Property(chat => chat.InvitedByUserId)
                .HasMaxLength(450);


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

            builder.Entity<SavedTalent>(entity =>
            {
                entity.Property(item => item.Status)
                    .HasMaxLength(20)
                    .IsRequired();

                entity.HasIndex(item => new { item.EmployerId, item.CandidateId })
                    .IsUnique();

                entity.HasOne(item => item.Employer)
                    .WithMany()
                    .HasForeignKey(item => item.EmployerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(item => item.Candidate)
                    .WithMany()
                    .HasForeignKey(item => item.CandidateId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<JobInvitation>(entity =>
            {
                entity.Property(item => item.Message)
                    .HasMaxLength(500);

                entity.HasIndex(item => new
                    {
                        item.JobPostId,
                        item.EmployerId,
                        item.CandidateId
                    })
                    .IsUnique();

                entity.HasOne(item => item.JobPost)
                    .WithMany(item => item.Invitations)
                    .HasForeignKey(item => item.JobPostId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(item => item.Employer)
                    .WithMany()
                    .HasForeignKey(item => item.EmployerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(item => item.Candidate)
                    .WithMany()
                    .HasForeignKey(item => item.CandidateId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<JobPost>()
                .Property(item => item.RequiredSkills)
                .HasMaxLength(1000);


            // =========================
            // PROFILE SECTIONS
            // =========================

            builder.Entity<Experience>()
                .HasOne(ex => ex.User)
                .WithMany(u => u.Experiences)
                .HasForeignKey(ex => ex.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<SavedPost>()
                .HasIndex(item => new { item.UserId, item.PostId })
                .IsUnique();

            builder.Entity<SavedPost>()
                .HasOne(item => item.User)
                .WithMany()
                .HasForeignKey(item => item.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<SavedPost>()
                .HasOne(item => item.Post)
                .WithMany()
                .HasForeignKey(item => item.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<EventItem>()
                .HasOne(item => item.Employer)
                .WithMany()
                .HasForeignKey(item => item.EmployerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<EventItem>()
                .Property(item => item.Topics)
                .HasMaxLength(300);

            builder.Entity<EventItem>()
                .Property(item => item.EventUrl)
                .HasMaxLength(500);

            builder.Entity<EventAttendance>()
                .HasIndex(item => new { item.EventItemId, item.UserId })
                .IsUnique();

            builder.Entity<EventAttendance>()
                .HasOne(item => item.EventItem)
                .WithMany(item => item.Attendees)
                .HasForeignKey(item => item.EventItemId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<EventAttendance>()
                .HasOne(item => item.User)
                .WithMany()
                .HasForeignKey(item => item.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Notification>()
                .HasOne<EventItem>()
                .WithMany()
                .HasForeignKey(item => item.EventId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Notification>()
                .HasOne<JobPost>()
                .WithMany()
                .HasForeignKey(item => item.JobPostId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Notification>()
                .HasIndex(item => new
                {
                    item.SenderId,
                    item.ReceiverId,
                    item.EventId,
                    item.Type
                })
                .IsUnique()
                .HasFilter("[EventId] IS NOT NULL");

            builder.Entity<Experience>()
                .HasOne(ex => ex.Company)
                .WithMany()
                .HasForeignKey(ex => ex.CompanyId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Education>()
                .HasOne(ed => ed.User)
                .WithMany(u => u.Educations)
                .HasForeignKey(ed => ed.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Education>()
                .HasOne(ed => ed.InstitutionCompany)
                .WithMany()
                .HasForeignKey(ed => ed.InstitutionCompanyId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<UserSkill>()
                .HasOne(us => us.User)
                .WithMany(u => u.Skills)
                .HasForeignKey(us => us.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ProfileOption>(entity =>
            {
                entity.Property(option => option.Name)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(option => option.NormalizedName)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(option => option.CreatedAt)
                    .HasDefaultValueSql("SYSUTCDATETIME()");

                entity.HasIndex(option => new { option.Type, option.NormalizedName })
                    .IsUnique();

            });


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

            builder.Entity<ProfileReport>()
                .HasOne(report => report.Reporter)
                .WithMany()
                .HasForeignKey(report => report.ReporterId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ProfileReport>()
                .HasOne(report => report.ReportedUser)
                .WithMany()
                .HasForeignKey(report => report.ReportedUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ProfileReport>()
                .HasIndex(report => new { report.ReporterId, report.ReportedUserId, report.IsReviewed });

            builder.Entity<ProfileReport>()
                .HasIndex(report => new { report.ReportedUserId, report.CreatedAt });

            builder.Entity<ProfileReport>()
                .Property(report => report.Category)
                .HasMaxLength(80);

            builder.Entity<ProfileReport>()
                .Property(report => report.Details)
                .HasMaxLength(500);

            builder.Entity<UserBlock>()
                .HasIndex(block => new { block.BlockerId, block.BlockedUserId })
                .IsUnique();

            builder.Entity<UserBlock>()
                .HasOne(block => block.Blocker)
                .WithMany()
                .HasForeignKey(block => block.BlockerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<UserBlock>()
                .HasOne(block => block.BlockedUser)
                .WithMany()
                .HasForeignKey(block => block.BlockedUserId)
                .OnDelete(DeleteBehavior.Restrict);



            builder.Entity<SearchHistory>()
                .HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<SearchHistory>()
                .HasIndex(x => new { x.UserId, x.CreatedAt });
        }
    }
}
