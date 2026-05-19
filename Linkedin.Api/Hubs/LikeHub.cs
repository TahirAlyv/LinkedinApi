using Linkedin.Business.Services.Interface;
using Linkedin.Core.Dtos;
using Linkedin.Core.Entities;
using Linkedin.DataAccess.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Linkedin.Api.Hubs
{
    [Authorize]
    public class LikeHub : Hub
    {
        private readonly ILikeService _likeService;


        public LikeHub(ILikeService likeService)
        {
            _likeService = likeService;

        }

        public override Task OnConnectedAsync()
        {
            Console.WriteLine($"User connected to LikeHub: {Context.UserIdentifier}");
            return base.OnConnectedAsync();
        }

        public async Task ToggleLike(int postId)
        {
            var userId = Context.UserIdentifier;
            if (userId == null) return;

            var result = await _likeService.ToggleLikeAsync(postId, userId);
            if (!result.Success) return;

            await Clients.All.SendAsync(
                "UpdateLikeCount",
                postId,
                result.Data 
            );
        }
    }
}