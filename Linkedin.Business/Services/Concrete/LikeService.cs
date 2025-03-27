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
    public class LikeService : ILikeService
    {

        private IUnitOfWork _unitOfWork;

        public LikeService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ServiceResult> AddLikeAsync(CreateLikeDto dto, string userId)
        {

            var post = await _unitOfWork.Posts.GetByIdAsync(dto.PostId);
            if (post != null)
            {
                return new ServiceResult(success: false, message: "Post not found.",data:null!);
            }

            var like = new Like
            {
                UserId = userId,
                CreatedAt = DateTime.Now,
                PostId = dto.PostId,

            };

            await _unitOfWork.Likes.AddAsync(like);
            var result = await _unitOfWork.CompleteAsync();

            if (result != 1)
            {
                return new ServiceResult(success: false, message: "An error occurred while adding like.",data:null!);
            }

            return new ServiceResult(success: true, message: "like added successfully.",data:like);
        }
    }
}
