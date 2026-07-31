using System.Threading;
using System.Threading.Tasks;

namespace Linkedin.Business.Services.Interface
{
    public interface IEmailService
    {
        Task SendAsync(
            string recipientEmail,
            string recipientName,
            string subject,
            string htmlContent,
            CancellationToken cancellationToken = default);
    }
}
