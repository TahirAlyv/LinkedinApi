using Linkedin.DataAccess.Repositories.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Linkedin.Api.BackgroundServices
{
    public class RefreshTokenCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RefreshTokenCleanupService> _logger;

        public RefreshTokenCleanupService(
            IServiceScopeFactory scopeFactory,
            ILogger<RefreshTokenCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await CleanupExpiredTokensAsync(stoppingToken);

            using var timer = new PeriodicTimer(TimeSpan.FromDays(1));

            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    await CleanupExpiredTokensAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {

            }
        }

        private async Task CleanupExpiredTokensAsync(CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                var unitOfWork =
                    scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                var expiredTokens = await unitOfWork.RefreshTokens
                    .GetExpiredTokensAsync(DateTime.UtcNow);

                if (!expiredTokens.Any())
                {
                    return;
                }

                foreach (var token in expiredTokens)
                {
                    unitOfWork.RefreshTokens.Remove(token);
                }

                var deletedCount = await unitOfWork.CompleteAsync();

                _logger.LogInformation(
                    "Expired refresh token cleanup completed. Deleted {DeletedCount} tokens.",
                    deletedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "An error occurred while cleaning expired refresh tokens.");
            }
        }
    }
}