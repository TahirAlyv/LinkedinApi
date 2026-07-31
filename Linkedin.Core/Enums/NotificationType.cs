using Linkedin.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Enums
{
    public enum NotificationType
    {
        Comment = 1,
        Like = 2,
        Follow = 3,
        FollowRequest = 4,
        FollowAccepted = 5,
        PostModerationWarning = 6,
        Event = 7,
        CompanyMention = 8,
        EventAttendance = 9,
        JobInvitation = 10
    }
}
