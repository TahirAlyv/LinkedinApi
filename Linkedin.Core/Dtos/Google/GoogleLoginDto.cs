using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Dtos.Google
{
    public class GoogleLoginDto
    {
        public string IdToken { get; set; }
        public string? AccountType { get; set; }
        public string? CompanyName { get; set; }
    }
}
