using Linkedin.Business.Services.Interface;
using System.Collections.Concurrent;
using System;

namespace Linkedin.Business.Services.Concrete
{
    public sealed class EmailCooldownService : IEmailCooldownService
    {
        private static readonly TimeSpan Cooldown = TimeSpan.FromMinutes(1);
        private readonly ConcurrentDictionary<string, DateTimeOffset> _lastRequests = new();

        public bool TryAcquire(string purpose, string email, out int retryAfterSeconds)
        {
            var key = $"{purpose}:{email.Trim().ToLowerInvariant()}";
            var now = DateTimeOffset.UtcNow;

            while (true)
            {
                if (_lastRequests.TryGetValue(key, out var lastRequest))
                {
                    var remaining = Cooldown - (now - lastRequest);
                    if (remaining > TimeSpan.Zero)
                    {
                        retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(remaining.TotalSeconds));
                        return false;
                    }

                    if (_lastRequests.TryUpdate(key, now, lastRequest))
                    {
                        retryAfterSeconds = 0;
                        return true;
                    }

                    continue;
                }

                if (_lastRequests.TryAdd(key, now))
                {
                    retryAfterSeconds = 0;
                    return true;
                }
            }
        }

        public void Release(string purpose, string email)
        {
            var key = $"{purpose}:{email.Trim().ToLowerInvariant()}";
            _lastRequests.TryRemove(key, out _);
        }
    }
}
