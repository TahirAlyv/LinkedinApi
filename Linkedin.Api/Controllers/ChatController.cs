using Linkedin.Business.Services.Interface;
using Linkedin.Core.Dtos;
using Linkedin.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Linkedin.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private IUserService _userService;

        public ChatController(IChatService chatService, IUserService userService)
        {
            _chatService = chatService;
            _userService = userService;
        }

        [HttpPost("send")]

        public async Task<IActionResult> SendMessage( string receiverId, [FromBody] MessageDto dto)
        {
            var user= await _userService.GetAuthenticatedUserAsync(User);

            if(user == null)
            {
                return Unauthorized("User not found or unauthorized!");
            }

            if(!ModelState.IsValid)
            {
                return BadRequest("Invalid message data");
            }


            var message = await _chatService.SendMessageAsync(user.Id, receiverId, dto);
            return Ok(message);
        }


        //[HttpGet("messages/{username}")]
        //public async Task<IActionResult> GetChatMessages([FromRoute] string username)
        //{
        //    var sender= await _userService.GetAuthenticatedUserAsync(User);
        //    var result= await _userService.GetUserByUserName(username);

        //    var receiver = result.Data as ApplicationUser;

        //    var messages = await _chatService.GetChatMessagesAsync(sender.Id, receiver.Id);
        //    if (messages == null)
        //    {
        //        return NotFound("No messages found for this chat");
        //    }
        //    var messagesList = messages.Select(m => new MessageDto { Content = m.Content, DateTime = m.DateTime,Sender=m.Sender.UserName }).ToList();
        //    return Ok(messagesList);
        //}

        [HttpGet("user-chats")]

        public async Task<IActionResult> GetUserChats()
        {
            var user = await _userService.GetAuthenticatedUserAsync(User);
            if (user == null)
            {
                return Unauthorized("User not found or unauthorized!");
            }
            var chats = await _chatService.GetUserChatsAsync(user.Id);

            var chatList = chats.Select(c => new ChatDto
            {
                Sender=c.Sender.UserName,
                Receiver = c.Receiver.UserName,
                SenderProfilImage=c.Sender.ProfileImage,
                ReveiverProfilImage=c.Receiver.ProfileImage,
                CreatedAt = c.CreatedAt,
                Message = c.Messages.Select(m => new MessageDto 
                { 
                    Sender=c.Sender.UserName,
                    Content = m.Content, 
                    DateTime = m.DateTime, 
                    HasSeen = m.HasSeen, 
                    IsImage = false }).ToList(),

            }).ToList();
            
                
           return Ok(chatList);
        }

    }
}
