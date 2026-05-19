using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Dtos.Connection
{
    public class DirectConnectionDto
    {
        public ConnectionUserDto CurrentUser { get; set; } = null!;
        public ConnectionUserDto TargetUser { get; set; } = null!;
    }
}
