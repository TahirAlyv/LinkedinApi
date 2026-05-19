using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Dtos
{
    public class UserLookupDto
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string? ProfileImage { get; set; }
    }
}
