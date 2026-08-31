using Linkedin.Business.Exceptions;
using Linkedin.Business.Services.Interface;
using Linkedin.Core.Dtos;
using Linkedin.Core.Data;
using Linkedin.Core.Entities;
using Linkedin.Core.Enums;
using Linkedin.DataAccess.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Linkedin.Business.Services.Concrete
{
    public class ChatService : IChatService
    {
        private const int MaxFilesPerMessage = 5;
        private const long MaxTotalFileSize = 25L * 1024L * 1024L;

        private readonly IUnitOfWork _unitOfWork;
        private readonly IUploadImage _uploadImage;
        private readonly ILogger<ChatService> _logger;
        private readonly AppDbContext _context;

        public ChatService(
            IUnitOfWork unitOfWork,
            IUploadImage uploadImage,
            ILogger<ChatService> logger,
            AppDbContext context)
        {
            _unitOfWork = unitOfWork;
            _uploadImage = uploadImage;
            _logger = logger;
            _context = context;
        }

        public async Task<Chat> GetOrCreateChatAsync(
            string senderId,
            string receiverId)
        {
            var chat = await _unitOfWork.Chats
                .GetChatBetweenUsersAsync(senderId, receiverId);

            if (chat != null)
                return chat;

            var newChat = new Chat
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Chats.AddAsync(newChat);
            await _unitOfWork.CompleteAsync();

            return newChat;
        }

        public async Task<IEnumerable<MessageDto>> GetChatMessagesAsync(
            string senderId,
            string receiverId)
        {
            var chat = await _unitOfWork.Chats
                .GetChatBetweenUsersAsync(senderId, receiverId);

            if (chat == null)
                return Array.Empty<MessageDto>();

            var messages = await _unitOfWork.Messages
                .GetMessagesByChatIdAsync(chat.Id);

            var hiddenAt = chat.SenderId == senderId
                ? chat.SenderHiddenAt
                : chat.ReceiverHiddenAt;

            return messages
           .Where(message => !hiddenAt.HasValue || message.DateTime > hiddenAt.Value)
           .Select(message => MapMessage(message))
           .ToList();
        }

        public async Task<IEnumerable<Chat>> GetUserChatsAsync(string userId)
        {
            var chats = (await _unitOfWork.Chats.GetUserChatsAsync(userId)).ToList();
            foreach (var chat in chats)
            {
                var hiddenAt = chat.SenderId == userId
                    ? chat.SenderHiddenAt
                    : chat.ReceiverHiddenAt;
                if (hiddenAt.HasValue)
                {
                    chat.Messages = chat.Messages
                        .Where(message => message.DateTime > hiddenAt.Value)
                        .ToList();
                }
            }

            return chats.Where(chat => chat.Messages.Count > 0).ToList();
        }

        public async Task DeleteChatForUserAsync(int chatId, string currentUserId)
        {
            if (chatId <= 0 || string.IsNullOrWhiteSpace(currentUserId))
                throw new ChatMessageException(ChatMessageError.InvalidRequest, "A valid chat is required.");

            var chat = await _unitOfWork.Chats.GetByIdAsync(chatId);
            if (chat == null)
                throw new ChatMessageException(ChatMessageError.MessageNotFound, "Conversation not found.");
            if (chat.SenderId != currentUserId && chat.ReceiverId != currentUserId)
                throw new ChatMessageException(ChatMessageError.NotMessageOwner, "You cannot delete this conversation.");

            var now = DateTime.UtcNow;
            if (chat.SenderId == currentUserId) chat.SenderHiddenAt = now;
            else chat.ReceiverHiddenAt = now;

            try { await _unitOfWork.CompleteAsync(); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to hide chat {ChatId} for user {UserId}.", chatId, currentUserId);
                throw new ChatMessageException(ChatMessageError.SaveFailed, "The conversation could not be deleted.", ex);
            }
        }

        public async Task<MessageDto> SendMessageAsync(
            string senderId,
            string receiverId,
            SendMessageDto dto)
        {
            ValidateSendRequest(senderId, receiverId, dto);

            var content = string.IsNullOrWhiteSpace(dto.Content)
                ? null
                : dto.Content.Trim();

            var files = dto.Files ?? new List<IFormFile>();

            var sender = await _unitOfWork.Users.GetByIdAsync(senderId);
            var receiver = await _unitOfWork.Users.GetByIdAsync(receiverId);

            if (sender == null || receiver == null)
            {
                throw new ChatMessageException(
                    ChatMessageError.UserNotFound,
                    "Sender or receiver was not found.");
            }

            if (await IsBlockedEitherWayAsync(senderId, receiverId))
            {
                throw new ChatMessageException(
                    ChatMessageError.UserBlocked,
                    "Messaging is unavailable because one of these accounts has blocked the other.");
            }

            var chat = await _unitOfWork.Chats
                .GetChatBetweenUsersAsync(senderId, receiverId);

            var isEmployerInvitation =
                sender.UserType == UserType.Employer &&
                receiver.UserType == UserType.JobSeeker;

            var isMemberReplyingToEmployer =
                sender.UserType == UserType.JobSeeker &&
                receiver.UserType == UserType.Employer;

            if (isEmployerInvitation)
            {
                if (chat == null)
                {
                    if (files.Count > 0 || string.IsNullOrWhiteSpace(content))
                    {
                        throw new ChatMessageException(
                            ChatMessageError.InvitationRequired,
                            "The first company message must be a text invitation.");
                    }
                }
                else if (chat.RequiresAcceptance &&
                         chat.InvitationStatus == ChatInvitationStatus.Pending)
                {
                    throw new ChatMessageException(
                        ChatMessageError.InvitationPending,
                        "Wait for the member to accept your invitation before sending another message.");
                }
                else if (chat.RequiresAcceptance &&
                         chat.InvitationStatus == ChatInvitationStatus.Rejected)
                {
                    throw new ChatMessageException(
                        ChatMessageError.InvitationRejected,
                        "This messaging invitation was declined.");
                }
            }
            else if (isMemberReplyingToEmployer)
            {
                if (chat == null ||
                    !chat.RequiresAcceptance ||
                    chat.InvitationStatus != ChatInvitationStatus.Accepted)
                {
                    throw new ChatMessageException(
                        ChatMessageError.InvitationRequired,
                        "Accept the company's invitation before replying.");
                }
            }
            else
            {
                var areConnected = await _unitOfWork.Connections
                    .AreConnectedAsync(senderId, receiverId);

                if (!areConnected)
                {
                    throw new ChatMessageException(
                        ChatMessageError.NotConnected,
                        "You can message only connected users.");
                }
            }

            var isNewChat = chat == null;

            if (isNewChat)
            {
                chat = new Chat
                {
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    CreatedAt = DateTime.UtcNow,
                    RequiresAcceptance = isEmployerInvitation,
                    InvitationStatus = isEmployerInvitation
                        ? ChatInvitationStatus.Pending
                        : ChatInvitationStatus.None,
                    InvitedByUserId = isEmployerInvitation ? senderId : null
                };
            }

            var uploadedFiles = new List<ChatFileUploadResultDto>();

            foreach (var file in files)
            {
                var upload = await _uploadImage.UploadChatFileAsync(file);

                if (upload == null)
                {
                    await CleanupUploadsAsync(uploadedFiles);

                    throw new ChatMessageException(
                        ChatMessageError.UploadFailed,
                        "One or more files could not be uploaded. Check the file type and 10 MB size limit.");
                }

                uploadedFiles.Add(upload);
            }

            var message = new Message
            {
                SenderId = senderId,
                Content = content,
                IsImage = uploadedFiles.Any(file =>
                    file.Type == ChatAttachmentType.Image),
                DateTime = DateTime.UtcNow,
                HasSeen = false,
                Attachments = uploadedFiles.Select(file => new ChatAttachment
                {
                    Url = file.Url,
                    PublicId = file.PublicId,
                    ResourceType = file.ResourceType,
                    OriginalFileName = file.OriginalFileName,
                    ContentType = file.ContentType,
                    SizeBytes = file.SizeBytes,
                    Type = file.Type
                }).ToList()
            };

            try
            {
                if (isNewChat)
                {
                    message.Chat = chat!;
                    chat!.Messages.Add(message);

                    // New chat, first message and attachments are persisted
                    // together in one SaveChangesAsync call.
                    await _unitOfWork.Chats.AddAsync(chat);
                }
                else
                {
                    message.ChatId = chat!.Id;
                    await _unitOfWork.Messages.AddAsync(message);
                }

                await _unitOfWork.CompleteAsync();
            }
            catch (Exception ex)
            {
                await CleanupUploadsAsync(uploadedFiles);

                _logger.LogError(
                    ex,
                    "Failed to save chat message. SenderId: {SenderId}, ReceiverId: {ReceiverId}",
                    senderId,
                    receiverId);

                throw new ChatMessageException(
                    ChatMessageError.SaveFailed,
                    "The message could not be saved.",
                    ex);
            }

            message.Sender = sender;
            message.Chat = chat!;

            return MapMessage(message, receiverId, receiver);
        }

        public async Task<ChatInvitationDto> GetInvitationStatusAsync(
            string currentUserId,
            string otherUserId)
        {
            if (await IsBlockedEitherWayAsync(currentUserId, otherUserId))
            {
                return new ChatInvitationDto
                {
                    Status = "blocked",
                    CanSend = false,
                    Message = "Messaging is unavailable between these accounts."
                };
            }

            var chat = await _unitOfWork.Chats
                .GetChatBetweenUsersAsync(currentUserId, otherUserId);

            if (chat == null)
            {
                var current = await _unitOfWork.Users.GetByIdAsync(currentUserId);
                var other = await _unitOfWork.Users.GetByIdAsync(otherUserId);
                var canInvite = current?.UserType == UserType.Employer &&
                                other?.UserType == UserType.JobSeeker;
                var areConnected = await _unitOfWork.Connections
                    .AreConnectedAsync(currentUserId, otherUserId);

                return new ChatInvitationDto
                {
                    Status = "none",
                    CanSend = canInvite || areConnected,
                    Message = canInvite
                        ? "Send one invitation message. More messages unlock after acceptance."
                        : null
                };
            }

            return MapInvitation(chat, currentUserId);
        }

        public async Task<ChatInvitationDto> RespondToInvitationAsync(
            string currentUserId,
            string otherUserId,
            bool accept)
        {
            if (await IsBlockedEitherWayAsync(currentUserId, otherUserId))
                throw new ChatMessageException(ChatMessageError.UserBlocked, "This invitation is unavailable.");

            var chat = await _unitOfWork.Chats
                .GetChatBetweenUsersAsync(currentUserId, otherUserId);

            if (chat == null ||
                !chat.RequiresAcceptance ||
                chat.InvitationStatus != ChatInvitationStatus.Pending ||
                chat.InvitedByUserId == currentUserId)
            {
                throw new ChatMessageException(
                    ChatMessageError.InvalidRequest,
                    "This invitation can no longer be answered.");
            }

            chat.InvitationStatus = accept
                ? ChatInvitationStatus.Accepted
                : ChatInvitationStatus.Rejected;
            chat.InvitationRespondedAt = DateTime.UtcNow;
            await _unitOfWork.CompleteAsync();

            return MapInvitation(chat, currentUserId);
        }

        private async Task<bool> IsBlockedEitherWayAsync(string firstUserId, string secondUserId)
        {
            return await _context.UserBlocks.AsNoTracking().AnyAsync(item =>
                (item.BlockerId == firstUserId && item.BlockedUserId == secondUserId) ||
                (item.BlockerId == secondUserId && item.BlockedUserId == firstUserId));
        }

        private static ChatInvitationDto MapInvitation(Chat chat, string currentUserId)
        {
            var invitedByMe = chat.InvitedByUserId == currentUserId;
            var status = chat.InvitationStatus.ToString().ToLowerInvariant();
            return new ChatInvitationDto
            {
                ChatId = chat.Id,
                Status = status,
                RequiresAcceptance = chat.RequiresAcceptance,
                InvitedByMe = invitedByMe,
                CanRespond = chat.RequiresAcceptance &&
                             chat.InvitationStatus == ChatInvitationStatus.Pending &&
                             !invitedByMe,
                CanSend = !chat.RequiresAcceptance ||
                          chat.InvitationStatus == ChatInvitationStatus.Accepted,
                Message = status switch
                {
                    "pending" when invitedByMe => "Invitation sent. Wait for the member to accept it.",
                    "pending" => "This company invited you to continue the conversation.",
                    "rejected" => "This invitation was declined.",
                    _ => null
                }
            };
        }

        public async Task MarkAsSeenAsync(int messageId)
        {
            var message = await _unitOfWork.Messages
                .GetMessageByIdAsync(messageId);

            if (message != null &&
                !message.IsDeleted &&
                !message.HasSeen)
            {
                message.HasSeen = true;

                await _unitOfWork.CompleteAsync();
            }
        }

        public async Task MarkChatAsSeenAsync(
            string currentUserId,
            string otherUserId)
        {
            var chat = await _unitOfWork.Chats
                .GetChatBetweenUsersAsync(currentUserId, otherUserId);

            if (chat == null)
                return;

            var messages = await _unitOfWork.Messages
                .GetMessagesByChatIdAsync(chat.Id);

            var unreadIncomingMessages = messages
                .Where(message =>
                    message.SenderId != currentUserId &&
                    !message.HasSeen)
                .ToList();

            if (unreadIncomingMessages.Count == 0)
                return;

            foreach (var message in unreadIncomingMessages)
            {
                message.HasSeen = true;
            }

            await _unitOfWork.CompleteAsync();
        }

        public async Task<MessageDeleteResultDto> DeleteMessageAsync(
    int messageId,
    string currentUserId)
        {
            if (messageId <= 0)
            {
                throw new ChatMessageException(
                    ChatMessageError.InvalidRequest,
                    "A valid message ID is required.");
            }

            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                throw new ChatMessageException(
                    ChatMessageError.InvalidRequest,
                    "Current user ID is required.");
            }

            var message = await _unitOfWork.Messages
                .GetMessageByIdAsync(messageId);

            if (message == null)
            {
                throw new ChatMessageException(
                    ChatMessageError.MessageNotFound,
                    "The message was not found or has already been deleted.");
            }

            /*
             * Yalnız mesajı göndərən user
             * həmin mesajı hər iki tərəfdən silə bilər.
             */
            if (!string.Equals(
                    message.SenderId,
                    currentUserId,
                    StringComparison.Ordinal))
            {
                throw new ChatMessageException(
                    ChatMessageError.NotMessageOwner,
                    "You can delete only your own messages.");
            }

            if (message.Chat == null)
            {
                throw new ChatMessageException(
                    ChatMessageError.SaveFailed,
                    "The message chat could not be resolved.");
            }

            var deletedAt = DateTime.UtcNow;

            var receiverId =
                message.Chat.SenderId == currentUserId
                    ? message.Chat.ReceiverId
                    : message.Chat.SenderId;

            /*
             * Əvvəl database-də soft delete edilir.
             */
            message.IsDeleted = true;
            message.DeletedAt = deletedAt;
            message.DeletedByUserId = currentUserId;

            try
            {
                await _unitOfWork.CompleteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to delete chat message. MessageId: {MessageId}, UserId: {UserId}",
                    messageId,
                    currentUserId);

                throw new ChatMessageException(
                    ChatMessageError.SaveFailed,
                    "The message could not be deleted.",
                    ex);
            }

            /*
             * DB save uğurlu olduqdan sonra
             * Cloudinary faylları silinir.
             *
             * Cloudinary silinməsi uğursuz olsa belə
             * mesaj artıq istifadəçilərə görünməyəcək.
             */
            await CleanupDeletedMessageAttachmentsAsync(
                message.Attachments);

            return new MessageDeleteResultDto
            {
                MessageId = message.Id,
                ChatId = message.ChatId,
                SenderId = currentUserId,
                ReceiverId = receiverId,
                DeletedAt = deletedAt
            };
        }

        private async Task CleanupDeletedMessageAttachmentsAsync(
        IEnumerable<ChatAttachment>? attachments)
        {
            if (attachments == null)
                return;

            foreach (var attachment in attachments)
            {
                try
                {
                    var deleted =
                        await _uploadImage.DeleteCloudinaryFileAsync(
                            attachment.PublicId,
                            attachment.ResourceType);

                    if (!deleted)
                    {
                        _logger.LogWarning(
                            "Deleted message attachment could not be removed from Cloudinary. " +
                            "AttachmentId: {AttachmentId}, PublicId: {PublicId}, ResourceType: {ResourceType}",
                            attachment.Id,
                            attachment.PublicId,
                            attachment.ResourceType);
                    }
                }
                catch (Exception ex)
                {
                    /*
                     * Cloudinary xətası DB-dəki soft delete-i
                     * geri qaytarmamalıdır.
                     */
                    _logger.LogError(
                        ex,
                        "Failed to remove deleted message attachment from Cloudinary. " +
                        "AttachmentId: {AttachmentId}, PublicId: {PublicId}",
                        attachment.Id,
                        attachment.PublicId);
                }
            }
        }




        private static void ValidateSendRequest(
            string senderId,
            string receiverId,
            SendMessageDto dto)
        {
            if (dto == null)
            {
                throw new ChatMessageException(
                    ChatMessageError.InvalidRequest,
                    "Message request is required.");
            }

            if (string.IsNullOrWhiteSpace(senderId) ||
                string.IsNullOrWhiteSpace(receiverId))
            {
                throw new ChatMessageException(
                    ChatMessageError.InvalidRequest,
                    "Sender and receiver are required.");
            }

            var hasContent = !string.IsNullOrWhiteSpace(dto.Content);
            var files = dto.Files ?? new List<IFormFile>();

            if (!hasContent && files.Count == 0)
            {
                throw new ChatMessageException(
                    ChatMessageError.InvalidRequest,
                    "Message content or at least one file is required.");
            }

            if (files.Count > MaxFilesPerMessage)
            {
                throw new ChatMessageException(
                    ChatMessageError.InvalidRequest,
                    $"A message can contain a maximum of {MaxFilesPerMessage} files.");
            }

            var totalFileSize = files.Sum(file => file?.Length ?? 0);

            if (totalFileSize > MaxTotalFileSize)
            {
                throw new ChatMessageException(
                    ChatMessageError.InvalidRequest,
                    "The total size of files in one message cannot exceed 25 MB.");
            }

            if (string.Equals(
                    senderId,
                    receiverId,
                    StringComparison.Ordinal))
            {
                throw new ChatMessageException(
                    ChatMessageError.CannotMessageSelf,
                    "You cannot send a message to yourself.");
            }
        }

        private static MessageDto MapMessage(
            Message message,
            string? receiverId = null,
            ApplicationUser? receiver = null)
        {
            return new MessageDto
            {
                Id = message.Id,
                ChatId = message.ChatId,
                Sender = message.Sender?.UserName ?? string.Empty,
                SenderId = message.SenderId,
                SenderProfileImage = message.Sender?.ProfileImage,
                Receiver = receiver?.UserName,
                ReceiverId = receiver?.Id ?? receiverId,
                Content = message.Content,
                IsImage = message.IsImage,
                DateTime = message.DateTime,
                HasSeen = message.HasSeen,
                Attachments = message.Attachments
                    .Select(attachment => new ChatAttachmentDto
                    {
                        Id = attachment.Id,
                        Url = attachment.Url,
                        OriginalFileName = attachment.OriginalFileName,
                        ContentType = attachment.ContentType,
                        SizeBytes = attachment.SizeBytes,
                        Type = attachment.Type
                    })
                    .ToList()
            };
        }

        private async Task CleanupUploadsAsync(
            IEnumerable<ChatFileUploadResultDto> uploadedFiles)
        {
            foreach (var uploadedFile in uploadedFiles)
            {
                try
                {
                    var deleted = await _uploadImage.DeleteCloudinaryFileAsync(
                        uploadedFile.PublicId,
                        uploadedFile.ResourceType);

                    if (!deleted)
                    {
                        _logger.LogWarning(
                            "Chat upload cleanup did not delete file. PublicId: {PublicId}, ResourceType: {ResourceType}",
                            uploadedFile.PublicId,
                            uploadedFile.ResourceType);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Chat upload cleanup failed. PublicId: {PublicId}, ResourceType: {ResourceType}",
                        uploadedFile.PublicId,
                        uploadedFile.ResourceType);
                }
            }
        }
    }
}
