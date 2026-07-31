using Linkedin.Core.Entities;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Linkedin.Api.Identity
{
    public sealed class EmailConfirmationTokenProviderOptions
        : DataProtectionTokenProviderOptions
    {
    }

    public sealed class PasswordResetTokenProviderOptions
        : DataProtectionTokenProviderOptions
    {
    }

    public sealed class EmailConfirmationTokenProvider
        : DataProtectorTokenProvider<ApplicationUser>
    {
        public EmailConfirmationTokenProvider(
            IDataProtectionProvider dataProtectionProvider,
            IOptions<EmailConfirmationTokenProviderOptions> options,
            ILogger<DataProtectorTokenProvider<ApplicationUser>> logger)
            : base(dataProtectionProvider, options, logger)
        {
        }
    }

    public sealed class PasswordResetTokenProvider
        : DataProtectorTokenProvider<ApplicationUser>
    {
        public PasswordResetTokenProvider(
            IDataProtectionProvider dataProtectionProvider,
            IOptions<PasswordResetTokenProviderOptions> options,
            ILogger<DataProtectorTokenProvider<ApplicationUser>> logger)
            : base(dataProtectionProvider, options, logger)
        {
        }
    }
}
