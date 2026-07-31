using Linkedin.Core.Dtos;
using Microsoft.AspNetCore.Http;

namespace Linkedin.Business.Services.Interface
{
    public interface IUploadImage
    {
        Task<string?> UploadFile(
            IFormFile file,
            string fileCategory = "default");

        Task<ChatFileUploadResultDto?> UploadChatFileAsync(
            IFormFile file);

        Task<bool> DeleteCloudinaryFileAsync(
            string publicId,
            string resourceType);

        Task<bool> DeletePhysicalFileIfExists(
            string? relativeUrl);
    }
}
