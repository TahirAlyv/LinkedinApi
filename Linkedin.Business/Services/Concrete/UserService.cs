using Linkedin.Business.Services.Interface;
using Linkedin.Core.Common;
using Linkedin.DataAccess.Repositories.Concrete;
using Linkedin.DataAccess.Repositories.Interfaces;
using LinkedIn.Core.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Business.Services.Concrete
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserService(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
        {
            _unitOfWork = unitOfWork;
            _httpContextAccessor = httpContextAccessor;
        }


        public async Task<ApplicationUser?> GetAuthenticatedUserAsync(ClaimsPrincipal user)
        {

            var userId = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return null;
            }

            return await _unitOfWork.Users.GetuserByIdAsync(userId);
        }

        public async Task<ServiceResult> GetSearchUser(string query,string username)
        {
             var users= await _unitOfWork.Users.GetSearchUsers(query, username);

            if (users == null)
                return new ServiceResult(success: false, message: "not found users!", null!);

            return new ServiceResult(success: true, message: "successfull", users!);

        }

        public async Task<ServiceResult> GetUserByUserName(string username)
        {
             var targetUser = await _unitOfWork.Users.GetUserByUsername(username);


            if(targetUser == null)
                return new ServiceResult(success: false,message:"user not found!",data:null!);

            var user= await _unitOfWork.Users.GetUserWithFollowersAsync(username);

            return new ServiceResult(success: true, message: "successfull", data: user!);
        }
    }
}
