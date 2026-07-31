using Linkedin.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.Core.Dtos
{
    public class ChatFileUploadResultDto
    {
        public string Url { get; set; } = null!;

        public string PublicId { get; set; } = null!;

        // image və ya raw
        public string ResourceType { get; set; } = null!;

        public string OriginalFileName { get; set; } = null!;

        public string ContentType { get; set; } = null!;

        public long SizeBytes { get; set; }

        public ChatAttachmentType Type { get; set; }
    }
}
