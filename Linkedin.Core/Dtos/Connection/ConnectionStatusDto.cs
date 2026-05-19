using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Dtos.Connection
{
    public class ConnectionStatusDto
    {
        public string Status { get; set; } = "none";
        public int? RequestId { get; set; }
    }
}
