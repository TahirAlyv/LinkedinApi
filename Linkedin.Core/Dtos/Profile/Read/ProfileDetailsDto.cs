using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Dtos.Profile.Read
{
    public class ProfileDetailsDto
    {
        public BasicInfoDto? BasicInfo { get; set; }
        public ContactInfoDto? ContactInfo { get; set; }
        public AboutDto? About { get; set; }
        public List<ExperienceDto>? Experiences { get; set; }
        public List<EducationDto>? Educations { get; set; }
        public List<SkillDto>? Skills { get; set; }
        public ActivitiesPreviewDto? ActivitiesPreview { get; set; }
    }
}
