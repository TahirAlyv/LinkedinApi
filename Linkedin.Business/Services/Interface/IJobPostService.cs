using Linkedin.Core.Common;
using Linkedin.Core.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Business.Services.Interface
{
    public interface IJobPostService
    {
        Task<ServiceResult> CreateJobPostAsync(CreateJobPostDto postDto, string userId);
    }
}
