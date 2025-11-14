using Dotnet.GenAI.MyCareerAssistant.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dotnet.GenAI.MyCareerAssistant.Services
{
    public class EmailSender
    {
        private readonly EmailSenderSettings _emailSenderSettings;
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(
            EmailSenderSettings emailSenderSettings,
            ILogger<EmailSender> logger)
        {
            _emailSenderSettings = emailSenderSettings;
            _logger = logger;
        }

        public async Task SendEmailAsync(
            string toEmail,
            string subject,
            string message,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(
                _emailSenderSettings.ApiKey))
            {
                throw new ArgumentException("Null or whitespace ApiKey.");
            }

            await ExecuteAsync(
                _emailSenderSettings.ApiKey,
                subject,
                message,
                toEmail,
                ct);
        }

        private async Task ExecuteAsync(
            string apiKey,
            string subject,
            string message,
            string toEmail,
            CancellationToken ct = default)
        {
            var client = new SendGridClient(apiKey);

            var msg = new SendGridMessage
            {
                From = new EmailAddress(
                    _emailSenderSettings.SenderEmail,
                    _emailSenderSettings.SenderName),
                Subject = subject,
                PlainTextContent = message,
                HtmlContent = message
            };
            msg.AddTo(new EmailAddress(toEmail));

            // Disable click tracking.
            // See https://sendgrid.com/docs/User_Guide/Settings/tracking.html
            msg.SetClickTracking(false, false);

            var response = await client.SendEmailAsync(msg, ct);

            if (!response.IsSuccessStatusCode)
            {
                var responseContentStr = await response
                    .Body
                    .ReadAsStringAsync(ct);

                _logger.LogWarning(
                    "Failure Email to {toEmail} with message: {responseContentStr}",
                    toEmail,
                    responseContentStr);
            }
        }
    }
}
