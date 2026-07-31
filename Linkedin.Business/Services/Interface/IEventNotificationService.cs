namespace Linkedin.Business.Services.Interface
{
    public interface IEventNotificationService
    {
        Task NotifyMatchingUsersAsync(int eventId, string organizerId);
    }
}
