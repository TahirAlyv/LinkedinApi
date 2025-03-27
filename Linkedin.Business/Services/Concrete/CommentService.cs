using Linkedin.Business.Services.Interface;
using Linkedin.Core.Common;
using Linkedin.Core.Dtos;
using Linkedin.DataAccess.Repositories.Interfaces;
using LinkedIn.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Business.Services.Concrete
{
    public class CommentService : ICommentService
    {

        private readonly IUnitOfWork _unitOfWork;

        public CommentService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ServiceResult> AddComment(CreateCommentDto dto, string userId)
        {
            var post= await _unitOfWork.Posts.GetByIdAsync(dto.PostId);
            if(post == null)
            {
                return new ServiceResult(success: false,message:"not font post!",data:null!);
            }

            var comment = new Comment
            {
                PostId = post.Id,
                Post = post,
                CreatedAt = DateTime.Now,
                UserId= userId
            };

            _=_unitOfWork.Comments.AddAsync(comment);
            var check = await _unitOfWork.CompleteAsync();

            if (check != 1)
            {
                return new ServiceResult(success: true, message: "There was a problem adding a comment!",data:comment);

            }

            return new ServiceResult(success: true, message: "comment added successfully!", data: comment);

        }

 
        public Task<ServiceResult> RemovePost(CreateCommentDto commentDto)
        {
            throw new NotImplementedException();
        }
    }
}
