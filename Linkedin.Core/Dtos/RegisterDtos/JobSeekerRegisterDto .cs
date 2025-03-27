using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Dtos.RegisterDtos
{
    public class JobSeekerRegisterDto: BaseRegisterDto
    {
        public string FullName { get; set; }
        public string? Bio { get; set; }
        public string? Skills { get; set; }
        public string? Experience { get; set; }
    }
}
