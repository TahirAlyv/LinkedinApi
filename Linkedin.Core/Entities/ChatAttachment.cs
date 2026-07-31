using Linkedin.Core.Enums;

namespace Linkedin.Core.Entities
{
    public class ChatAttachment
    {
        public int Id { get; set; }

        public int MessageId { get; set; }

        public virtual Message Message { get; set; } = null!;

        // Cloudinary-dəki tam HTTPS URL
        public string Url { get; set; } = null!;

        // Cloudinary-dən sonradan silmək üçün
        public string PublicId { get; set; } = null!;

        // image və ya raw
        public string ResourceType { get; set; } = null!;

        // İstifadəçinin kompüterindəki orijinal fayl adı
        public string OriginalFileName { get; set; } = null!;

        // image/png, application/pdf və s.
        public string ContentType { get; set; } = null!;

        // Fayl ölçüsü byte ilə
        public long SizeBytes { get; set; }

        public ChatAttachmentType Type { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}