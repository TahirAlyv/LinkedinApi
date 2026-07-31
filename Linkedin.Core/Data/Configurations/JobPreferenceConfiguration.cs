using Linkedin.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Linkedin.Core.Data.Configurations
{
    public class JobPreferenceConfiguration : IEntityTypeConfiguration<JobPreference>
    {
        public void Configure(EntityTypeBuilder<JobPreference> builder)
        {
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => x.UserId).IsUnique();

            builder.Property(x => x.JobTitles).HasMaxLength(1000);
            builder.Property(x => x.Locations).HasMaxLength(1000);
            builder.Property(x => x.WorkplaceTypes).HasMaxLength(200);
            builder.Property(x => x.EmploymentTypes).HasMaxLength(300);
            builder.Property(x => x.OnsiteLocations).HasMaxLength(1000);
            builder.Property(x => x.RemoteLocations).HasMaxLength(1000);
            builder.Property(x => x.StartAvailability).HasMaxLength(50);

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
