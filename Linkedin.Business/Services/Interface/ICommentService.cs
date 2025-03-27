using Linkedin.Core.Common;
using Linkedin.Core.Dtos;
using LinkedIn.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Business.Services.Interface
{
    public interface ICommentService
    {
        Task<ServiceResult> AddComment(CreateCommentDto commentDto, string userId);
        Task<ServiceResult> RemovePost(CreateCommentDto commentDto);
    }
}
