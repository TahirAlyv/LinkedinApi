using Azure.Core;
using Linkedin.Business.Services.Interface;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class UploadImage : IUploadImage
{
    public async Task<string?> UploadFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return null;
        }
        var extension = Path.GetExtension(file.FileName).ToLower();
        var folder = (extension == ".mp4" || extension == ".avi") ? "videos" : "uploads";

        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", folder);
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
}

