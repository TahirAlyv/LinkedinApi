using Linkedin.Business.Services.Interface;
using Linkedin.Core.Data;
using Linkedin.Core.Dtos.Ai;
using Linkedin.Core.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Security.Claims;

namespace Linkedin.Business.Services.Concrete
{
    public class AiRateLimiterService : IAiRateLimiterService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AiRateLimiterService(
            AppDbContext context,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<AiRateLimitResult> TryAcquireAsync(string projectKey)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                var maxRequestsPerMinute =
                    _configuration.GetValue<int?>("AiRateLimits:MaxRequestsPerMinute") ?? 5;

                var minSecondsBetweenRequests =
                    _configuration.GetValue<int?>("AiRateLimits:MinSecondsBetweenRequests") ?? 10;

                var maxRequestsPerDay =
                    _configuration.GetValue<int?>("AiRateLimits:MaxRequestsPerDay") ?? 60;

                var now = DateTime.UtcNow;

                var minuteStart = now.AddSeconds(-60);
                var dayStart = now.AddHours(-24);

                var userId = _httpContextAccessor
                    .HttpContext?
                    .User?
                    .FindFirst(ClaimTypes.NameIdentifier)?
                    .Value;

                await using var transaction =
                    await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

                var lastRequestAt = await _context.AiRequestLogs
                    .Where(x => x.ProjectKey == projectKey)
                    .OrderByDescending(x => x.CreatedAt)
                    .Select(x => (DateTime?)x.CreatedAt)
                    .FirstOrDefaultAsync();

                if (lastRequestAt.HasValue)
                {
                    var secondsFromLastRequest =
                        (now - lastRequestAt.Value).TotalSeconds;

                    if (secondsFromLastRequest < minSecondsBetweenRequests)
                    {
                        var waitSeconds =
                            (int)Math.Ceiling(minSecondsBetweenRequests - secondsFromLastRequest);

                        await transaction.RollbackAsync();

                        return new AiRateLimitResult
                        {
                            Allowed = false,
                            RetryAfterSeconds = waitSeconds,
                            Message = $"Please wait {waitSeconds} seconds before using AI again."
                        };
                    }
                }

                var minuteRequestCount = await _context.AiRequestLogs
                    .CountAsync(x =>
                        x.ProjectKey == projectKey &&
                        x.CreatedAt >= minuteStart);

                if (minuteRequestCount >= maxRequestsPerMinute)
                {
                    var oldestMinuteRequest = await _context.AiRequestLogs
                        .Where(x =>
                            x.ProjectKey == projectKey &&
                            x.CreatedAt >= minuteStart)
                        .OrderBy(x => x.CreatedAt)
                        .Select(x => x.CreatedAt)
                        .FirstAsync();

                    var waitSeconds =
                        (int)Math.Ceiling(60 - (now - oldestMinuteRequest).TotalSeconds);

                    if (waitSeconds < 1)
                        waitSeconds = 1;

                    await transaction.RollbackAsync();

                    return new AiRateLimitResult
                    {
                        Allowed = false,
                        RetryAfterSeconds = waitSeconds,
                        Message = $"AI minute limit reached. Please try again in {waitSeconds} seconds."
                    };
                }

                var dailyRequestCount = await _context.AiRequestLogs
                    .CountAsync(x =>
                        x.ProjectKey == projectKey &&
                        x.CreatedAt >= dayStart);

                if (dailyRequestCount >= maxRequestsPerDay)
                {
                    var oldestDailyRequest = await _context.AiRequestLogs
                        .Where(x =>
                            x.ProjectKey == projectKey &&
                            x.CreatedAt >= dayStart)
                        .OrderBy(x => x.CreatedAt)
                        .Select(x => x.CreatedAt)
                        .FirstAsync();

                    var waitSeconds =
                        (int)Math.Ceiling(
                            TimeSpan.FromHours(24).TotalSeconds -
                            (now - oldestDailyRequest).TotalSeconds);

                    if (waitSeconds < 1)
                        waitSeconds = 1;

                    await transaction.RollbackAsync();

                    return new AiRateLimitResult
                    {
                        Allowed = false,
                        RetryAfterSeconds = waitSeconds,
                        Message = "AI daily limit reached. Please try again later."
                    };
                }

                var log = new AiRequestLog
                {
                    ProjectKey = projectKey,
                    UserId = userId,
                    CreatedAt = now
                };

                await _context.AiRequestLogs.AddAsync(log);

                var cleanupBefore = now.AddHours(-25);

                var oldLogs = await _context.AiRequestLogs
                    .Where(x => x.CreatedAt < cleanupBefore)
                    .ToListAsync();

                if (oldLogs.Any())
                {
                    _context.AiRequestLogs.RemoveRange(oldLogs);
                }

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return new AiRateLimitResult
                {
                    Allowed = true,
                    RetryAfterSeconds = 0,
                    Message = "AI request allowed."
                };
            });
        }
    }
}