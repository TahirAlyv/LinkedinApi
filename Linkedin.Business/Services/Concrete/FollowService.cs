using Linkedin.Business.Services.Interface;
using Linkedin.Core.Common;
using Linkedin.DataAccess.Repositories.Interfaces;
using LinkedIn.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Business.Services.Concrete
{
    public class FollowService : IFollowService
    {

        private readonly IUnitOfWork _unitOfWork;


        public FollowService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
 
        }

        public async Task<ServiceResult> FollowAsync(string followerUserId, string followedUserId)
        {
            var user = await _unitOfWork.Users.GetuserByIdAsync(followedUserId);
            if (user == null)
                return new ServiceResult(success: false, message: "not found user!", data: null!);

            var follow = new Follow
            {
                FollowerId = followerUserId,
                FollowingId = followedUserId
            };

            await _unitOfWork.Follows.AddAsync(follow);
            var result = await _unitOfWork.CompleteAsync();


            if (result <= 0)
                return new ServiceResult(success: false, message: "There was a problem while trackingn", data: null!);

            return new ServiceResult(success: true, message: "Successfully followed", data: follow);

        }

        public async Task<bool> IsFollowing(string followerUserId, string followedUserId)
        {
            var result= await _unitOfWork.Follows.IsFollowingAsync(followerUserId, followedUserId);

            if(!result) return false;

            return result;
        }

        public async Task<ServiceResult> UnfollowAsync(string followerUserId, string followedUserId)
        {
            var user = await _unitOfWork.Users.GetuserByIdAsync(followedUserId);
            if (user == null)
                return new ServiceResult(success: false, message: "not found user!", data: null!);

            var follow =  await _unitOfWork.Follows.GetFollowRelationAsync(followerUserId, followedUserId);
            if(follow==null)
                return new ServiceResult(success: false, message: "Follow relation not found!", data: null!);

            _unitOfWork.Follows.Remove(follow);
            var result= await _unitOfWork.CompleteAsync();

            if(result<=0)
                return new ServiceResult(success: false, message: "unexpected problem occurred", data: null!);

            return new ServiceResult(success: true, message: "Unfollowed successfully", data: null!);
        }
    }
}
