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
    public class PostService : IPostService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUploadImage _uploadImage;

        public PostService(IUnitOfWork unitOfWork, IUploadImage uploadImage)
        {
            _unitOfWork = unitOfWork;
            _uploadImage = uploadImage;
        }

        public async Task<ServiceResult> CreatePostAsync(CreatePostDto postDto,string userId)
        {

            string imageUrl = null;
 
            if (postDto.File != null)
            {
                imageUrl = await _uploadImage.UploadFile(postDto.File);

            }

            var post = new Post
            {
                UserID = userId,  
                Content = postDto.Content,
                ImageUrl = imageUrl, 
                CreatedAt = DateTime.UtcNow,
                CommentCount = 0,
                LikeCount = 0
            };

            await _unitOfWork.Posts.AddAsync(post);
            var check=await _unitOfWork.CompleteAsync();

            var result = new ServiceResult(success: true, message: "Post successfully created!", data: post);

            if (check != 0)
            {
                result = new ServiceResult(success: false, message: "There was a problem creating the post!", data: post);
            }

            return result;


        }
    }
}
