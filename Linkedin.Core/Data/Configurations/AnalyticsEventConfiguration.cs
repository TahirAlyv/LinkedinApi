using Linkedin.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Linkedin.Core.Data.Configurations
{
    public class AnalyticsEventConfiguration : IEntityTypeConfiguration<AnalyticsEvent>
    {
        public void Configure(EntityTypeBuilder<AnalyticsEvent> builder)
        {
            builder.ToTable("AnalyticsEvents");
            builder.HasKey(item => item.Id);

            builder.Property(item => item.SearchQuery).HasMaxLength(150);
            builder.Property(item => item.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

            builder.HasOne(item => item.ViewerUser)
                .WithMany()
                .HasForeignKey(item => item.ViewerUserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(item => item.TargetUser)
                .WithMany()
                .HasForeignKey(item => item.TargetUserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(item => item.Post)
                .WithMany()
                .HasForeignKey(item => item.PostId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(item => item.JobPost)
                .WithMany()
                .HasForeignKey(item => item.JobPostId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(item => new { item.TargetUserId, item.EventType, item.CreatedAt });
            builder.HasIndex(item => new { item.ViewerUserId, item.EventType, item.CreatedAt });
            builder.HasIndex(item => new { item.PostId, item.EventType, item.CreatedAt });
            builder.HasIndex(item => new { item.JobPostId, item.EventType, item.CreatedAt });
        }
    }
}
