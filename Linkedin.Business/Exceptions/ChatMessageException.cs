using System;

namespace Linkedin.Business.Exceptions
{
    public enum ChatMessageError
    {
        InvalidRequest = 1,
        CannotMessageSelf = 2,
        UserNotFound = 3,
        NotConnected = 4,
        UploadFailed = 5,
        SaveFailed = 6,
        MessageNotFound = 7,
        NotMessageOwner = 8
    }

    public sealed class ChatMessageException : Exception
    {
        public ChatMessageError Error { get; }

        public ChatMessageException(
            ChatMessageError error,
            string message)
            : base(message)
        {
            Error = error;
        }

        public ChatMessageException(
            ChatMessageError error,
            string message,
            Exception innerException)
            : base(message, innerException)
        {
            Error = error;
        }
    }
}
