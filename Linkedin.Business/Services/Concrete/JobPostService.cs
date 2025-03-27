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
    public class JobPostService:IJobPostService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUploadImage _uploadImage;

        public JobPostService(IUnitOfWork unitOfWork, IUploadImage uploadImage)
        {
            _unitOfWork = unitOfWork;
            _uploadImage = uploadImage;
        }
        public async Task<ServiceResult> CreateJobPostAsync(CreateJobPostDto postDto, string userId)
        {

            string imageUrl = null;

            if (postDto.File != null)
            {
                imageUrl = await _uploadImage.UploadFile(postDto.File);

            }

            var jobPost = new JobPost
            {
                Title = postDto.Title,
                Description = postDto.Description,
                Location = postDto.Location,
                Salary=postDto.Salary,
            };

            await _unitOfWork.JobPosts.AddAsync(jobPost);
            var check = await _unitOfWork.CompleteAsync();

            var result = new ServiceResult(success: true, message: "Post successfully created!", data: jobPost);

            if (check != 0)
            {
                result = new ServiceResult(success: false, message: "There was a problem creating the post!", data: jobPost);
            }

            return result;


        }

    }
}
