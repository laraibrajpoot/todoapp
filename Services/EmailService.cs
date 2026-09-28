using System;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace todolist.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        {
            var hostConfig = _configuration["Smtp:Host"];
            var smtpHost = string.IsNullOrWhiteSpace(hostConfig) ? "smtp.gmail.com" : hostConfig;

            var portConfig = _configuration["Smtp:Port"];
            var smtpPort = int.TryParse(portConfig, out var parsedPort) ? parsedPort : 587;

            var smtpUser = _configuration["Smtp:Username"] ?? "";
            var smtpPass = _configuration["Smtp:Password"] ?? "";
            var fromAddress = _configuration["Smtp:FromAddress"] ?? smtpUser;

            if (string.IsNullOrWhiteSpace(fromAddress) || string.IsNullOrWhiteSpace(smtpPass))
            {
                _logger.LogError("[EmailService] Missing SMTP credentials or Password in appsettings.json.");
                return false;
            }

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("TaskFlow Support", fromAddress));
                message.To.Add(MailboxAddress.Parse(toEmail));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder { HtmlBody = body };
                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();

                // Allow up to 10 seconds for connection
                client.Timeout = 10000;

                // SecureSocketOptions.Auto automatically matches Port 587 (StartTls) or Port 465 (SslOnConnect)
                await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.Auto);
                await client.AuthenticateAsync(smtpUser, smtpPass);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation($"[EmailService] OTP successfully delivered to {toEmail}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[EmailService] Failed to send email via MailKit to {toEmail}");

                // Fallback debug output in Visual Studio Output window
                System.Diagnostics.Debug.WriteLine("\n==================================================");
                System.Diagnostics.Debug.WriteLine($"[FALLBACK OTP CODE] To: {toEmail}");
                System.Diagnostics.Debug.WriteLine($"Content:\n{body}");
                System.Diagnostics.Debug.WriteLine("==================================================\n");

                return false;
            }
        }
    }
}