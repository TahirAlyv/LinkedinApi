using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Linkedin.Business.Services.Interface;
using Linkedin.Core.Dtos;
using Linkedin.Core.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Linkedin.Business.Services.Concrete
{
    public class UploadImage : IUploadImage
    {
        private readonly IWebHostEnvironment _env;
        private readonly Cloudinary _cloudinary;
        private readonly ILogger<UploadImage> _logger;

        private static readonly string[] AllowedImageExtensions =
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };

        private static readonly string[] AllowedVideoExtensions =
        {
            ".mp4",
            ".avi",
            ".mov",
            ".webm"
        };

        private static readonly string[] AllowedChatDocumentExtensions =
        {
            ".pdf",
            ".doc",
            ".docx",
            ".xls",
            ".xlsx",
            ".ppt",
            ".pptx",
            ".txt",
            ".csv",
            ".zip"
        };

        private const long OneMb = 1024L * 1024L;

        private const long MaxChatFileSize = 10 * OneMb;

        public UploadImage(
            IWebHostEnvironment env,
            Cloudinary cloudinary,
            ILogger<UploadImage> logger)
        {
            _env = env;
            _cloudinary = cloudinary;
            _logger = logger;
        }

        // Profil, background, post şəkli və post videosu üçün
        public async Task<string?> UploadFile(
            IFormFile file,
            string fileCategory = "default")
        {
            if (file == null || file.Length == 0)
                return null;

            var originalFileName = Path.GetFileName(file.FileName);

            if (string.IsNullOrWhiteSpace(originalFileName))
                return null;

            var extension = Path
                .GetExtension(originalFileName)
                .ToLowerInvariant();

            var isImage = AllowedImageExtensions.Contains(extension);
            var isVideo = AllowedVideoExtensions.Contains(extension);

            if (!isImage && !isVideo)
                return null;

            var category =
                fileCategory?.Trim().ToLowerInvariant()
                ?? "default";

            // Profil və background yalnız şəkil qəbul edir
            if ((category == "profile" ||
                 category == "background") &&
                !isImage)
            {
                return null;
            }

            // Video kateqoriyası yalnız video qəbul edir
            if (category == "video" && !isVideo)
                return null;

            var maxFileSize =
                GetMaxFileSize(category, isVideo);

            if (file.Length > maxFileSize)
            {
                _logger.LogWarning(
                    "Rejected file because it is too large. " +
                    "File: {FileName}, Size: {FileSize}",
                    originalFileName,
                    file.Length);

                return null;
            }

            var folder =
                GetCloudinaryFolder(category, isVideo);

            try
            {
                await using var stream =
                    file.OpenReadStream();

                if (isVideo)
                {
                    var videoUploadParams =
                        new VideoUploadParams
                        {
                            File = new FileDescription(
                                originalFileName,
                                stream),

                            Folder = folder,

                            PublicId =
                                Guid.NewGuid().ToString("N"),

                            Overwrite = false
                        };

                    var videoResult =
                        await _cloudinary.UploadAsync(
                            videoUploadParams);

                    if (videoResult.Error != null)
                    {
                        _logger.LogError(
                            "Cloudinary video upload failed. " +
                            "File: {FileName}, Error: {Error}",
                            originalFileName,
                            videoResult.Error.Message);

                        return null;
                    }

                    return videoResult
                        .SecureUrl?
                        .ToString();
                }

                var imageUploadParams =
                    new ImageUploadParams
                    {
                        File = new FileDescription(
                            originalFileName,
                            stream),

                        Folder = folder,

                        PublicId =
                            Guid.NewGuid().ToString("N"),

                        Overwrite = false
                    };

                var imageResult =
                    await _cloudinary.UploadAsync(
                        imageUploadParams);

                if (imageResult.Error != null)
                {
                    _logger.LogError(
                        "Cloudinary image upload failed. " +
                        "File: {FileName}, Error: {Error}",
                        originalFileName,
                        imageResult.Error.Message);

                    return null;
                }

                return imageResult
                    .SecureUrl?
                    .ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Cloudinary upload failed. " +
                    "File: {FileName}",
                    originalFileName);

                return null;
            }
        }

        // Chat şəkli, PDF və digər fayllar üçün
        public async Task<ChatFileUploadResultDto?>
            UploadChatFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                _logger.LogWarning(
                    "Empty chat file upload attempt.");

                return null;
            }

            if (file.Length > MaxChatFileSize)
            {
                _logger.LogWarning(
                    "Chat file rejected because it is too large. " +
                    "File: {FileName}, Size: {FileSize}",
                    file.FileName,
                    file.Length);

                return null;
            }

            // Fayl adına path əlavə edilməsinin qarşısını alır
            var originalFileName =
                Path.GetFileName(file.FileName);

            if (string.IsNullOrWhiteSpace(originalFileName))
                return null;

            var extension = Path
                .GetExtension(originalFileName)
                .ToLowerInvariant();

            var isImage =
                AllowedImageExtensions.Contains(extension);

            var isDocument =
                AllowedChatDocumentExtensions.Contains(extension);

            if (!isImage && !isDocument)
            {
                _logger.LogWarning(
                    "Unsupported chat file type. " +
                    "File: {FileName}, Extension: {Extension}",
                    originalFileName,
                    extension);

                return null;
            }

            try
            {
                await using var stream =
                    file.OpenReadStream();

                /*
                 * CHAT ŞƏKİL UPLOAD
                 */
                if (isImage)
                {
                    var uploadParams =
                        new ImageUploadParams
                        {
                            File = new FileDescription(
                                originalFileName,
                                stream),

                            Folder = "lynq/chat/images",

                            PublicId =
                                Guid.NewGuid().ToString("N"),

                            Overwrite = false
                        };

                    var result =
                        await _cloudinary.UploadAsync(
                            uploadParams);

                    if (result.Error != null)
                    {
                        _logger.LogError(
                            "Cloudinary chat image upload failed. " +
                            "File: {FileName}, Error: {Error}",
                            originalFileName,
                            result.Error.Message);

                        return null;
                    }

                    var secureUrl =
                        result.SecureUrl?.ToString();

                    if (string.IsNullOrWhiteSpace(secureUrl) ||
                        string.IsNullOrWhiteSpace(result.PublicId))
                    {
                        return null;
                    }

                    return new ChatFileUploadResultDto
                    {
                        Url = secureUrl,

                        PublicId = result.PublicId,

                        ResourceType = "image",

                        OriginalFileName =
                            originalFileName,

                        ContentType =
                            string.IsNullOrWhiteSpace(
                                file.ContentType)
                                ? GetFallbackContentType(
                                    extension)
                                : file.ContentType,

                        SizeBytes = file.Length,

                        Type = ChatAttachmentType.Image
                    };
                }

                /*
                 * PDF, WORD, EXCEL, ZIP VƏ S.
                 *
                 * Cloudinary-də raw resource kimi saxlanılır.
                 * Raw public ID daxilində extension saxlanmalıdır.
                 */
                var rawPublicId =
                    $"{Guid.NewGuid():N}{extension}";

                var rawUploadParams =
                    new RawUploadParams
                    {
                        File = new FileDescription(
                            originalFileName,
                            stream),

                        Folder = "lynq/chat/files",

                        PublicId = rawPublicId,

                        Overwrite = false
                    };

                var rawResult =
                    await _cloudinary.UploadAsync(
                        rawUploadParams);

                if (rawResult.Error != null)
                {
                    _logger.LogError(
                        "Cloudinary chat file upload failed. " +
                        "File: {FileName}, Error: {Error}",
                        originalFileName,
                        rawResult.Error.Message);

                    return null;
                }

                var rawSecureUrl =
                    rawResult.SecureUrl?.ToString();

                if (string.IsNullOrWhiteSpace(rawSecureUrl) ||
                    string.IsNullOrWhiteSpace(
                        rawResult.PublicId))
                {
                    return null;
                }

                return new ChatFileUploadResultDto
                {
                    Url = rawSecureUrl,

                    PublicId = rawResult.PublicId,

                    ResourceType = "raw",

                    OriginalFileName =
                        originalFileName,

                    ContentType =
                        string.IsNullOrWhiteSpace(
                            file.ContentType)
                            ? GetFallbackContentType(
                                extension)
                            : file.ContentType,

                    SizeBytes = file.Length,

                    Type = extension == ".pdf"
                        ? ChatAttachmentType.Pdf
                        : ChatAttachmentType.File
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Chat file upload failed. " +
                    "File: {FileName}",
                    originalFileName);

                return null;
            }
        }

        public async Task<bool> DeleteCloudinaryFileAsync(
            string publicId,
            string resourceType)
        {
            if (string.IsNullOrWhiteSpace(publicId) ||
                string.IsNullOrWhiteSpace(resourceType))
            {
                return false;
            }

            ResourceType cloudinaryResourceType;

            switch (resourceType.Trim().ToLowerInvariant())
            {
                case "image":
                    cloudinaryResourceType = ResourceType.Image;
                    break;

                case "video":
                    cloudinaryResourceType = ResourceType.Video;
                    break;

                case "raw":
                    cloudinaryResourceType = ResourceType.Raw;
                    break;

                default:
                    _logger.LogWarning(
                        "Unsupported Cloudinary resource type. PublicId: {PublicId}, ResourceType: {ResourceType}",
                        publicId,
                        resourceType);

                    return false;
            }

            try
            {
                var deleteParams = new DeletionParams(publicId)
                {
                    ResourceType = cloudinaryResourceType,
                    Invalidate = true
                };

                var result = await _cloudinary.DestroyAsync(deleteParams);

                if (result.Error != null)
                {
                    _logger.LogError(
                        "Cloudinary delete failed. PublicId: {PublicId}, ResourceType: {ResourceType}, Error: {Error}",
                        publicId,
                        resourceType,
                        result.Error.Message);

                    return false;
                }

                return string.Equals(
                           result.Result,
                           "ok",
                           StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(
                           result.Result,
                           "not found",
                           StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Cloudinary delete failed. PublicId: {PublicId}, ResourceType: {ResourceType}",
                    publicId,
                    resourceType);

                return false;
            }
        }

        public async Task<bool>
            DeletePhysicalFileIfExists(string? relativeUrl)
        {
            if (string.IsNullOrWhiteSpace(relativeUrl))
                return false;

            /*
             * YENİ CLOUDINARY URL-LƏRİ
             */
            if (IsCloudinaryUrl(relativeUrl))
            {
                try
                {
                    var resourceType =
                        GetCloudinaryResourceType(relativeUrl);

                    var publicId =
                        GetCloudinaryPublicId(
                            relativeUrl,
                            resourceType);

                    if (string.IsNullOrWhiteSpace(publicId))
                        return false;

                    return await DeleteCloudinaryFileAsync(
                        publicId,
                        resourceType.ToString().ToLowerInvariant());
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Cloudinary delete failed. Url: {Url}",
                        relativeUrl);

                    return false;
                }
            }

            /*
             * KÖHNƏ LOCAL FAYLLAR ÜÇÜN FALLBACK
             */
            try
            {
                if (string.IsNullOrWhiteSpace(_env.WebRootPath))
                    return false;

                var trimmed = relativeUrl
                    .TrimStart('/', '\\')
                    .Replace(
                        '/',
                        Path.DirectorySeparatorChar)
                    .Replace(
                        '\\',
                        Path.DirectorySeparatorChar);

                var webRoot = Path.GetFullPath(_env.WebRootPath);

                var fullPath = Path.GetFullPath(
                    Path.Combine(webRoot, trimmed));

                var webRootPrefix =
                    webRoot.TrimEnd(
                        Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar) +
                    Path.DirectorySeparatorChar;

                if (!fullPath.StartsWith(
                        webRootPrefix,
                        StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning(
                        "Rejected local file deletion outside web root. Url: {Url}",
                        relativeUrl);

                    return false;
                }

                if (!File.Exists(fullPath))
                    return false;

                File.Delete(fullPath);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Local file delete failed. Url: {Url}",
                    relativeUrl);

                return false;
            }
        }

        private static long GetMaxFileSize(
            string category,
            bool isVideo)
        {
            if (isVideo)
                return 25 * OneMb;

            return category switch
            {
                "profile" => 5 * OneMb,
                "background" => 8 * OneMb,
                "event" => 8 * OneMb,
                _ => 10 * OneMb
            };
        }

        private static string GetCloudinaryFolder(
            string category,
            bool isVideo)
        {
            if (isVideo)
                return "lynq/videos";

            return category switch
            {
                "profile" => "lynq/profiles",
                "background" => "lynq/backgrounds",
                "event" => "lynq/events",
                _ => "lynq/posts"
            };
        }

        private static bool IsCloudinaryUrl(string url)
        {
            if (!Uri.TryCreate(
                    url,
                    UriKind.Absolute,
                    out var uri))
            {
                return false;
            }

            return uri.Host.Equals(
                       "cloudinary.com",
                       StringComparison.OrdinalIgnoreCase) ||
                   uri.Host.EndsWith(
                       ".cloudinary.com",
                       StringComparison.OrdinalIgnoreCase);
        }

        private static ResourceType
            GetCloudinaryResourceType(string url)
        {
            if (url.Contains(
                    "/video/upload/",
                    StringComparison.OrdinalIgnoreCase))
            {
                return ResourceType.Video;
            }

            if (url.Contains(
                    "/raw/upload/",
                    StringComparison.OrdinalIgnoreCase))
            {
                return ResourceType.Raw;
            }

            return ResourceType.Image;
        }

        private static string? GetCloudinaryPublicId(
            string cloudinaryUrl,
            ResourceType resourceType)
        {
            if (!Uri.TryCreate(
                    cloudinaryUrl,
                    UriKind.Absolute,
                    out var uri))
            {
                return null;
            }

            const string uploadPart = "/upload/";

            var uploadIndex =
                uri.AbsolutePath.IndexOf(
                    uploadPart,
                    StringComparison.OrdinalIgnoreCase);

            if (uploadIndex < 0)
                return null;

            var pathAfterUpload =
                uri.AbsolutePath[
                    (uploadIndex + uploadPart.Length)..]
                .Trim('/');

            if (string.IsNullOrWhiteSpace(pathAfterUpload))
                return null;

            var parts = pathAfterUpload
                .Split(
                    '/',
                    StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            // v123456789 kimi Cloudinary version hissəsini silir
            if (parts.Count > 0 &&
                parts[0].StartsWith(
                    "v",
                    StringComparison.OrdinalIgnoreCase) &&
                long.TryParse(parts[0][1..], out _))
            {
                parts.RemoveAt(0);
            }

            if (parts.Count == 0)
                return null;

            var publicId =
                string.Join("/", parts);

            /*
             * Image və video public ID-də extension olmur.
             * Raw fayllarda isə .pdf, .docx və s. qalmalıdır.
             */
            if (resourceType != ResourceType.Raw)
            {
                var extension =
                    Path.GetExtension(publicId);

                if (!string.IsNullOrWhiteSpace(extension))
                {
                    publicId =
                        publicId[..^extension.Length];
                }
            }

            return Uri.UnescapeDataString(publicId);
        }

        private static string GetFallbackContentType(
            string extension)
        {
            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",

                ".png" => "image/png",

                ".webp" => "image/webp",

                ".pdf" => "application/pdf",

                ".doc" => "application/msword",

                ".docx" =>
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",

                ".xls" =>
                    "application/vnd.ms-excel",

                ".xlsx" =>
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",

                ".ppt" =>
                    "application/vnd.ms-powerpoint",

                ".pptx" =>
                    "application/vnd.openxmlformats-officedocument.presentationml.presentation",

                ".txt" =>
                    "text/plain",

                ".csv" =>
                    "text/csv",

                ".zip" =>
                    "application/zip",

                _ =>
                    "application/octet-stream"
            };
        }
    }
}
