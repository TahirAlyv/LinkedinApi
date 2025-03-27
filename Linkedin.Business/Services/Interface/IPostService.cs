using Linkedin.Core.Common;
using Linkedin.Core.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Business.Services.Interface
{
    public interface IPostService
    {
        Task<ServiceResult> CreatePostAsync(CreatePostDto postDto, string userId);
    }
}
