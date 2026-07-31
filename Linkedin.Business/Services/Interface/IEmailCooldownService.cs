namespace Linkedin.Business.Services.Interface
{
    public interface IEmailCooldownService
    {
        bool TryAcquire(string purpose, string email, out int retryAfterSeconds);
        void Release(string purpose, string email);
    }
}
