using Linkedin.Core.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Business.Services.Interface
{
    public interface INotificationPublisher
    {
        Task PublishAsync(string userId, NotificationReturnDto dto);
    }
}
