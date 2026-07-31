using Linkedin.Core.Common;
using Linkedin.Core.Dtos.Ai;

namespace Linkedin.Business.Services.Interface
{
    public interface IAiService
    {
        Task<ServiceResult> ImproveProfessionalTextAsync(ImproveTextRequestDto dto);

        Task<ServiceResult> ModeratePostAsync(string? text);
    }
}