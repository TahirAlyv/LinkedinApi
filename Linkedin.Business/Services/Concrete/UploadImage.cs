using Linkedin.Business.Services.Interface;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;

public class UploadImage : IUploadImage
{
    private readonly IWebHostEnvironment _env;

    public UploadImage(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<string?> UploadFile(IFormFile file, string fileCategory = "default")
    {
        if (file == null || file.Length == 0)
            return null;

        var extension = Path.GetExtension(file.FileName).ToLower();

        var allowedImageExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var allowedVideoExtensions = new[] { ".mp4", ".avi", ".mov", ".webm" };

        string folder;

        switch (fileCategory.ToLower())
        {
            case "profile":
                if (!allowedImageExtensions.Contains(extension))
                    return null;

                folder = "images/profiles";
                break;

            case "background":
                if (!allowedImageExtensions.Contains(extension))
                    return null;

                folder = "images/backgrounds";
                break;

            case "video":
                if (!allowedVideoExtensions.Contains(extension))
                    return null;

                folder = "videos";
                break;

            default:
                if (allowedImageExtensions.Contains(extension))
                    folder = "uploads";
                else if (allowedVideoExtensions.Contains(extension))
                    folder = "videos";
                else
                    return null;
                break;
        }

        var uploadsFolder = Path.Combine(_env.WebRootPath, folder);

        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return $"/{folder}/{fileName}";
    }

    public async Task<bool> DeletePhysicalFileIfExists(string? relativeUrl)
    {
        if (string.IsNullOrWhiteSpace(relativeUrl))
            return false;

        var trimmed = relativeUrl
            .TrimStart('/')
            .Replace("/", Path.DirectorySeparatorChar.ToString());

        var fullPath = Path.Combine(_env.WebRootPath, trimmed);

        if (File.Exists(fullPath))
            File.Delete(fullPath);

        return true;
    }
}