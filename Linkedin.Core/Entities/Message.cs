namespace Linkedin.Core.Entities
{
    public class Message
    {
        public int Id { get; set; }

        public string SenderId { get; set; } = null!;

        // Yalnız fayl göndərmək mümkün olduğu üçün nullable olmalıdır
        public string? Content { get; set; }

        // Köhnə frontend pozulmasın deyə hələlik saxlayırıq
        public bool IsImage { get; set; }

        public int ChatId { get; set; }

        public DateTime DateTime { get; set; }

        public virtual ApplicationUser Sender { get; set; } = null!;

        public virtual Chat Chat { get; set; } = null!;

        public bool HasSeen { get; set; }
        public bool IsDeleted { get; set; }

        public DateTime? DeletedAt { get; set; }

        public string? DeletedByUserId { get; set; }
        public virtual ICollection<ChatAttachment> Attachments { get; set; }
            = new List<ChatAttachment>();
    }
}