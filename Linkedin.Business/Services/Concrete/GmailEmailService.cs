using Linkedin.Business.Services.Interface;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Linkedin.Business.Services.Concrete
{
    public sealed class GmailEmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public GmailEmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendAsync(
            string recipientEmail,
            string recipientName,
            string subject,
            string htmlContent,
            CancellationToken cancellationToken = default)
        {
            var smtpEmail = _configuration["Gmail:SmtpEmail"]?.Trim();
            var appPassword = _configuration["Gmail:AppPassword"]?
                .Replace(" ", string.Empty)
                .Trim();
            var senderName = _configuration["Gmail:SenderName"]?.Trim() ?? "Nexora";

            if (string.IsNullOrWhiteSpace(smtpEmail) ||
                string.IsNullOrWhiteSpace(appPassword))
            {
                throw new InvalidOperationException(
                    "Gmail SMTP configuration is missing. Check Gmail:SmtpEmail and Gmail:AppPassword in User Secrets.");
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(senderName, smtpEmail));
            message.To.Add(new MailboxAddress(
                string.IsNullOrWhiteSpace(recipientName) ? recipientEmail : recipientName,
                recipientEmail));
            message.Subject = subject;
            message.Body = new BodyBuilder
            {
                HtmlBody = htmlContent,
                TextBody = "This message contains HTML content. Open it in an HTML-compatible email client."
            }.ToMessageBody();

            using var smtpClient = new SmtpClient();

            try
            {
                await smtpClient.ConnectAsync(
                    "smtp.gmail.com",
                    587,
                    SecureSocketOptions.StartTls,
                    cancellationToken);

                await smtpClient.AuthenticateAsync(
                    smtpEmail,
                    appPassword,
                    cancellationToken);

                await smtpClient.SendAsync(message, cancellationToken);
            }
            finally
            {
                if (smtpClient.IsConnected)
                    await smtpClient.DisconnectAsync(true, CancellationToken.None);
            }
        }
    }
}
