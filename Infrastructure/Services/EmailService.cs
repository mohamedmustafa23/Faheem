using Application.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly MailSettings _mailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<MailSettings> mailSettings, ILogger<EmailService> logger)
        {
            _mailSettings = mailSettings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string to, string subject, string htmlBody, string? textBody = null, CancellationToken ct = default)
        {
            try
            {
                var email = new MimeMessage();
                // Explicit display name so recipients see "جوكو" as the sender,
                // not the raw address. From + Sender both set for deliverability.
                var from = new MailboxAddress(_mailSettings.DisplayName, _mailSettings.EmailFrom);
                email.From.Add(from);
                email.Sender = from;
                email.To.Add(MailboxAddress.Parse(to));
                email.Subject = subject;

                var builder = new BodyBuilder { HtmlBody = htmlBody };
                // Plain-text alternative: clean notification previews + better
                // deliverability (spam filters penalise HTML-only mail).
                if (!string.IsNullOrWhiteSpace(textBody)) builder.TextBody = textBody;
                email.Body = builder.ToMessageBody();

                using var smtp = new SmtpClient();

                await smtp.ConnectAsync(_mailSettings.SmtpHost, _mailSettings.SmtpPort, SecureSocketOptions.StartTls, ct);

                await smtp.AuthenticateAsync(_mailSettings.SmtpUser, _mailSettings.SmtpPass, ct);

                await smtp.SendAsync(email, ct);
                await smtp.DisconnectAsync(true, ct);

                _logger.LogInformation($"Email successfully sent to {to}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to send email to {to}. Error: {ex.Message}");
            }
        }
    }
}