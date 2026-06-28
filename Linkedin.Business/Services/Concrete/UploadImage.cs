using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Linkedin.Business.Services.Interface;
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
            ".jpg", ".jpeg", ".png", ".webp"
        };

        private static readonly string[] AllowedVideoExtensions =
        {
            ".mp4", ".avi", ".mov", ".webm"
        };

        private const long OneMb = 1024L * 1024L;

        public UploadImage(
            IWebHostEnvironment env,
            Cloudinary cloudinary,
            ILogger<UploadImage> logger)
        {
            _env = env;
            _cloudinary = cloudinary;
            _logger = logger;
        }

        public async Task<string?> UploadFile(
            IFormFile file,
            string fileCategory = "default")
        {
            if (file == null || file.Length == 0)
                return null;

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            var isImage = AllowedImageExtensions.Contains(extension);
            var isVideo = AllowedVideoExtensions.Contains(extension);

            if (!isImage && !isVideo)
                return null;

            var category = fileCategory?.Trim().ToLowerInvariant() ?? "default";

            // Profil və background yalnız şəkil qəbul edir
            if ((category == "profile" || category == "background") && !isImage)
                return null;

            // video kateqoriyası yalnız video qəbul edir
            if (category == "video" && !isVideo)
                return null;

            var maxFileSize = GetMaxFileSize(category, isVideo);

            if (file.Length > maxFileSize)
            {
                _logger.LogWarning(
                    "Rejected file because it is too large. File: {FileName}, Size: {FileSize}",
                    file.FileName,
                    file.Length);

                return null;
            }

            var folder = GetCloudinaryFolder(category, isVideo);

            try
            {
                await using var stream = file.OpenReadStream();

                if (isVideo)
                {
                    var videoUploadParams = new VideoUploadParams
                    {
                        File = new FileDescription(file.FileName, stream),
                        Folder = folder,
                        PublicId = Guid.NewGuid().ToString("N"),
                        Overwrite = false
                    };

                    var videoResult =
                        await _cloudinary.UploadAsync(videoUploadParams);

                    return videoResult.SecureUrl?.ToString();
                }

                var imageUploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = folder,
                    PublicId = Guid.NewGuid().ToString("N"),
                    Overwrite = false
                };

                var imageResult =
                    await _cloudinary.UploadAsync(imageUploadParams);

                return imageResult.SecureUrl?.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Cloudinary upload failed. File: {FileName}",
                    file.FileName);

                return null;
            }
        }

        public async Task<bool> DeletePhysicalFileIfExists(string? relativeUrl)
        {
            if (string.IsNullOrWhiteSpace(relativeUrl))
                return false;

            // Yeni Cloudinary URL-ləri
            if (IsCloudinaryUrl(relativeUrl))
            {
                try
                {
                    var publicId = GetCloudinaryPublicId(relativeUrl);

                    if (string.IsNullOrWhiteSpace(publicId))
                        return false;

                    var resourceType =
                        relativeUrl.Contains("/video/upload/", StringComparison.OrdinalIgnoreCase)
                            ? ResourceType.Video
                            : ResourceType.Image;

                    var deleteParams = new DeletionParams(publicId)
                    {
                        ResourceType = resourceType,
                        Invalidate = true
                    };

                    await _cloudinary.DestroyAsync(deleteParams);

                    return true;
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

            // Köhnə /images/... və /uploads/... path-ləri üçün fallback.
            // Database sıfırlansa da, bu hissənin qalması zərər vermir.
            try
            {
                var trimmed = relativeUrl
                    .TrimStart('/')
                    .Replace("/", Path.DirectorySeparatorChar.ToString());

                var fullPath = Path.Combine(_env.WebRootPath, trimmed);

                if (File.Exists(fullPath))
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

        private static long GetMaxFileSize(string category, bool isVideo)
        {
            if (isVideo)
                return 25 * OneMb;

            return category switch
            {
                "profile" => 5 * OneMb,
                "background" => 8 * OneMb,
                _ => 10 * OneMb
            };
        }

        private static string GetCloudinaryFolder(string category, bool isVideo)
        {
            if (isVideo)
                return "lynq/videos";

            return category switch
            {
                "profile" => "lynq/profiles",
                "background" => "lynq/backgrounds",
                _ => "lynq/posts"
            };
        }

        private static bool IsCloudinaryUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var uri)
                   && uri.Host.EndsWith("cloudinary.com",
                       StringComparison.OrdinalIgnoreCase);
        }

        private static string? GetCloudinaryPublicId(string cloudinaryUrl)
        {
            if (!Uri.TryCreate(cloudinaryUrl, UriKind.Absolute, out var uri))
                return null;

            const string uploadPart = "/upload/";

            var uploadIndex = uri.AbsolutePath.IndexOf(
                uploadPart,
                StringComparison.OrdinalIgnoreCase);

            if (uploadIndex < 0)
                return null;

            var pathAfterUpload = uri.AbsolutePath[
                (uploadIndex + uploadPart.Length)..].Trim('/');

            if (string.IsNullOrWhiteSpace(pathAfterUpload))
                return null;

            var parts = pathAfterUpload
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            // Cloudinary URL-də v123456 kimi version hissəsi varsa, onu çıxarırıq
            if (parts.Count > 0 &&
                parts[0].StartsWith("v", StringComparison.OrdinalIgnoreCase) &&
                long.TryParse(parts[0][1..], out _))
            {
                parts.RemoveAt(0);
            }

            if (parts.Count == 0)
                return null;

            var publicIdWithExtension = string.Join("/", parts);

            var extension = Path.GetExtension(publicIdWithExtension);

            if (!string.IsNullOrWhiteSpace(extension))
            {
                publicIdWithExtension = publicIdWithExtension[
                    ..^extension.Length];
            }

            return Uri.UnescapeDataString(publicIdWithExtension);
        }
    }
}