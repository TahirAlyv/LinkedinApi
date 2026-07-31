using Linkedin.Business.Exceptions;
using Linkedin.Business.Services.Interface;
using Linkedin.Core.Dtos;
using Linkedin.Core.Entities;
using Linkedin.Core.Enums;
using Linkedin.DataAccess.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
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

        public ChatService(
            IUnitOfWork unitOfWork,
            IUploadImage uploadImage,
            ILogger<ChatService> logger)
        {
            _unitOfWork = unitOfWork;
            _uploadImage = uploadImage;
            _logger = logger;
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

            return messages
           .Select(message => MapMessage(message))
           .ToList();
        }

        public async Task<IEnumerable<Chat>> GetUserChatsAsync(string userId)
        {
            return await _unitOfWork.Chats.GetUserChatsAsync(userId);
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

            var areConnected = await _unitOfWork.Connections
                .AreConnectedAsync(senderId, receiverId);

            if (!areConnected)
            {
                throw new ChatMessageException(
                    ChatMessageError.NotConnected,
                    "You can message only connected users.");
            }

            var chat = await _unitOfWork.Chats
                .GetChatBetweenUsersAsync(senderId, receiverId);

            var isNewChat = chat == null;

            if (isNewChat)
            {
                chat = new Chat
                {
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    CreatedAt = DateTime.UtcNow
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
