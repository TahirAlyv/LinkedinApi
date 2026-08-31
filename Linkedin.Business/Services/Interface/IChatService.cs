using Linkedin.Core.Dtos;
using Linkedin.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Business.Services.Interface
{
    public interface IChatService
    {
        Task<IEnumerable<MessageDto>> GetChatMessagesAsync(string senderId, string receiverId);
        Task<MessageDto> SendMessageAsync(string senderId, string receiverId, SendMessageDto dto);
        Task<MessageDeleteResultDto> DeleteMessageAsync(
         int messageId,
         string currentUserId);
        Task<IEnumerable<Chat>> GetUserChatsAsync(string userId);
        Task<Chat> GetOrCreateChatAsync(string senderId, string receiverId);
        Task MarkAsSeenAsync(int messageId);
        Task MarkChatAsSeenAsync(string currentUserId, string otherUserId);
        Task DeleteChatForUserAsync(int chatId, string currentUserId);
        Task<ChatInvitationDto> GetInvitationStatusAsync(string currentUserId, string otherUserId);
        Task<ChatInvitationDto> RespondToInvitationAsync(string currentUserId, string otherUserId, bool accept);

    }
}
