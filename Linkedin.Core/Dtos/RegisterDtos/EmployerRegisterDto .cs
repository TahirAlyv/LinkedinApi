using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Dtos.RegisterDtos
{
   public class EmployerRegisterDto:BaseRegisterDto
    {
        public string CompanyName { get; set; }
        public string Industry { get; set; }
        public string? Bio { get; set; }
    }
}
