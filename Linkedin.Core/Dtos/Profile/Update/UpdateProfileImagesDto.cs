using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Dtos.Profile.Update
{
    public class UpdateProfileImagesDto
    {
        public IFormFile? ProfileImage { get; set; }
        public IFormFile? BackgroundImage { get; set; }
    }
}
