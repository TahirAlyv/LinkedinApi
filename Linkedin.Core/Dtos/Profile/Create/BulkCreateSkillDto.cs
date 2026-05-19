using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Dtos.Profile.Create
{
    public class BulkCreateSkillDto
    {
        public List<CreateSkillDto> Skills { get; set; } = new();
    }
}
