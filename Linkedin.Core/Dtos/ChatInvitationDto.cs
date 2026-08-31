namespace Linkedin.Core.Dtos
{
    public class ChatInvitationDto
    {
        public int? ChatId { get; set; }
        public string Status { get; set; } = "none";
        public bool RequiresAcceptance { get; set; }
        public bool InvitedByMe { get; set; }
        public bool CanRespond { get; set; }
        public bool CanSend { get; set; }
        public string? Message { get; set; }
    }

    public class ChatInvitationResponseDto
    {
        public bool Accept { get; set; }
    }
}
