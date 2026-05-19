using Linkedin.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.DataAccess.Configurations
{
    public class NotificationConfiguration: IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.HasIndex(x => new
            {
                x.SenderId,
                x.ReceiverId,
                x.PostId,
                x.Type
            }).IsUnique();
        }
    }
}
