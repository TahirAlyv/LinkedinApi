using Linkedin.Api.Hubs;
using Linkedin.Business.Services.Interface;
using Linkedin.Core.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;



namespace Linkedin.Api.Hubs
{
    [Authorize]
    public class CommentHub : Hub
    {
        private readonly ICommentService _commentService;
 
 

       public  CommentHub(ICommentService commentService)
        {
            _commentService = commentService;
        }

        public override async Task OnConnectedAsync()
        {
            Console.WriteLine($"User connected to CommentHub: {Context.UserIdentifier}");
            await base.OnConnectedAsync();
        }


        // Comment əlavə edildikdən sonra, Commentin Sayini Gonderirik Herkese
        public async Task JoinPostCounter(int postId)
        {
            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                $"post-count-{postId}"
            );
        }

        public async Task LeavePostCounter(int postId)
        {
            await Groups.RemoveFromGroupAsync(
                Context.ConnectionId,
                $"post-count-{postId}"
            );
        }

        /* 🔔 COMMENT PANEL AÇILANDA */
        public async Task JoinPost(int postId)
        {
            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                $"post-{postId}"
            );
        }

        /* 🔕 COMMENT PANEL BAĞLANANDA */
        public async Task LeavePost(int postId)
        {
            await Groups.RemoveFromGroupAsync(
                Context.ConnectionId,
                $"post-{postId}"
            );
        }

        /* ✍️ COMMENT GÖNDƏR */
        public async Task SendComment(int postId, string content)
        {
            var userId = Context.UserIdentifier;
            if (userId == null || string.IsNullOrWhiteSpace(content))
                return;

            var comment = await _commentService.AddComment(
                new CreateCommentDto
                {
                    PostId = postId,
                    Text = content
                },
                userId
            );
 
            if (comment == null)
                return;


            await Clients.Group($"post-{postId}")
            .SendAsync("ReceiveComment", new
            {
                CommentId = comment.CommentId,
                PostId = comment.PostId,
                Content = comment.Content,
                CreatedAt = comment.CreatedAt,
                Username = comment.Username,
                UserProfileUrl = comment.UserPhoto,
                UserId = comment.UserId
            });

            var commentCount = await _commentService.GetCommentCountByPostIdAsync(postId);

            await Clients.Group($"post-count-{postId}")
                .SendAsync("ReceiveCommentCountUpdated", postId, commentCount);
        }

    }
}
