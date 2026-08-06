using Linkedin.Business.Services.Interface;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Linkedin.Business.Services.Concrete
{
    public sealed class MailjetEmailService : IEmailService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public MailjetEmailService(
            HttpClient httpClient,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task SendAsync(
            string recipientEmail,
            string recipientName,
            string subject,
            string htmlContent,
            CancellationToken cancellationToken = default)
        {
            var apiKey = _configuration["Mailjet:ApiKey"]?.Trim();
            var secretKey = _configuration["Mailjet:SecretKey"]?.Trim();
            var senderEmail = _configuration["Mailjet:SenderEmail"]?.Trim();
            var senderName =
                _configuration["Mailjet:SenderName"]?.Trim() ?? "Nexora";

            if (string.IsNullOrWhiteSpace(apiKey) ||
                string.IsNullOrWhiteSpace(secretKey) ||
                string.IsNullOrWhiteSpace(senderEmail))
            {
                throw new InvalidOperationException(
                    "Mailjet configuration is missing. Check User Secrets.");
            }

            var authenticationValue = Convert.ToBase64String(
                Encoding.ASCII.GetBytes($"{apiKey}:{secretKey}")
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "send"
            );

            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Basic",
                    authenticationValue
                );

            request.Content = JsonContent.Create(new
            {
                Messages = new[]
                {
                    new
                    {
                        From = new
                        {
                            Email = senderEmail,
                            Name = senderName
                        },
                        To = new[]
                        {
                            new
                            {
                                Email = recipientEmail,
                                Name = string.IsNullOrWhiteSpace(recipientName)
                                    ? recipientEmail
                                    : recipientName
                            }
                        },
                        Subject = subject,
                        HTMLPart = htmlContent,
                        TextPart =
                            "Please open this email in an HTML-compatible email client."
                    }
                }
            });

            using var response = await _httpClient.SendAsync(
                request,
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                var details = await response.Content.ReadAsStringAsync(
                    cancellationToken
                );

                throw new InvalidOperationException(
                    $"Mailjet rejected the email request " +
                    $"({(int)response.StatusCode}): {details}"
                );
            }
        }
    }
}