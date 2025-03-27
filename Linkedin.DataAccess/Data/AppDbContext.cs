using LinkedIn.Core.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Data
{
    public class AppDbContext: IdentityDbContext<ApplicationUser,ApplicationRole,string>
    {
        public DbSet<Post> Posts { get; set; }
        public DbSet<Like> Likes { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Chat> Chats { get; set; }   
        public DbSet<Message> Messages { get; set; }
        public DbSet<Follow> Follows { get; set; } 
        public DbSet<FollowRequest> FollowRequests { get; set; }
        public DbSet<JobPost> JobPosts { get; set; }
        public DbSet<JobApplication> JobApplications { get; set; }
        public AppDbContext(DbContextOptions<AppDbContext> options): base(options) { }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);  


            builder.Entity<Follow>()
                .HasOne(f=> f.Follower)
                .WithMany()
                .HasForeignKey(f=>f.FollowerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Follow>()
                .HasOne(f => f.Following)
                .WithMany()
                .HasForeignKey(f => f.FollowingId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<FollowRequest>()
                 .HasOne(fr => fr.Sender)
                 .WithMany()
                 .HasForeignKey(fr => fr.SenderId)
                 .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<FollowRequest>()
                .HasOne(fr => fr.Receiver)
                .WithMany()
                .HasForeignKey(fr => fr.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Post>()
                .HasOne(p=> p.User)
                .WithMany()
                .HasForeignKey(u=> u.UserID)
                .OnDelete(DeleteBehavior.Cascade);

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

            builder.Entity<Like>()
                .HasOne(l => l.Post)
                .WithMany()
                .HasForeignKey(l => l.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Like>()
                .HasOne(l => l.User)
                .WithMany()
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Restrict);


            builder.Entity<Message>()
                .HasOne(m => m.Chat)
                .WithMany()
                .HasForeignKey(m => m.ChatId)
                .OnDelete(DeleteBehavior.Cascade);


            builder.Entity<Chat>()
                .HasOne(c=> c.Sender)
                .WithMany()
                .HasForeignKey(c=> c.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Chat>()
                .HasOne(c=> c.Receiver) 
                .WithMany()
                .HasForeignKey(c=> c.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<JobApplication>()
                .HasOne(j=> j.JobPost)
                .WithMany()
                .HasForeignKey(j=> j.JobPostId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<JobApplication>()
                .HasOne(j=> j.User)
                .WithMany()
                .HasForeignKey(j=> j.UserId)
                .OnDelete(DeleteBehavior.Restrict);

        }



    }
}
