using System.Net;
using System.Net.Mail;
using KollectorScum.Api.Interfaces;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// Email service implementation using SMTP.
    /// When <c>Email:SmtpHost</c> is not configured the magic link is logged to the console
    /// instead of being sent, which is convenient for local development without an SMTP server.
    /// </summary>
    public class SmtpEmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task SendMagicLinkEmailAsync(string toEmail, string magicLink)
        {
            var emailSection = _configuration.GetSection("Email");
            var smtpHost = emailSection["SmtpHost"];
            var smtpPortStr = emailSection["SmtpPort"];
            var smtpUsername = emailSection["SmtpUsername"];
            var smtpPassword = emailSection["SmtpPassword"];
            var fromAddress = emailSection["FromAddress"] ?? "noreply@kollector.app";
            var fromName = emailSection["FromName"] ?? "Kollector Scum";
            var enableSslStr = emailSection["EnableSsl"] ?? "true";

            if (string.IsNullOrWhiteSpace(smtpHost))
            {
                _logger.LogWarning("SMTP host is not configured. Magic link for {Email}: {MagicLink}", toEmail, magicLink);
                return;
            }

            if (!int.TryParse(smtpPortStr, out var smtpPort))
            {
                smtpPort = 587;
            }

            bool.TryParse(enableSslStr, out var enableSsl);

            var subject = "Your Kollector Scum Sign-In Link";
            var body = BuildEmailBody(magicLink);

            using var message = new MailMessage
            {
                From = new MailAddress(fromAddress, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = enableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            if (!string.IsNullOrWhiteSpace(smtpUsername))
            {
                client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
            }

            await client.SendMailAsync(message);
            _logger.LogInformation("Magic link email sent to {Email}", toEmail);
        }

        /// <summary>
        /// Builds the HTML body for the magic link email
        /// </summary>
        /// <param name="magicLink">The magic link URL</param>
        /// <returns>HTML email body</returns>
        private static string BuildEmailBody(string magicLink)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset=""utf-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
</head>
<body style=""font-family: Arial, sans-serif; background-color: #0A0A10; color: #e5e7eb; margin: 0; padding: 0;"">
  <div style=""max-width: 480px; margin: 40px auto; background-color: #13131F; border: 1px solid #1C1C28; border-radius: 16px; padding: 40px; text-align: center;"">
    <h1 style=""color: #8B5CF6; font-size: 24px; margin-bottom: 8px;"">Kollector Scüm</h1>
    <p style=""color: #9ca3af; font-size: 14px; margin-bottom: 32px;"">Your Ultimate Physical Media Hub</p>
    <h2 style=""color: #f3f4f6; font-size: 18px; margin-bottom: 16px;"">Sign in to your account</h2>
    <p style=""color: #9ca3af; font-size: 14px; margin-bottom: 32px;"">
      Click the button below to sign in. This link is valid for 15 minutes and can only be used once.
    </p>
    <a href=""{magicLink}"" 
       style=""display: inline-block; background-color: #8B5CF6; color: #ffffff; text-decoration: none; 
              font-weight: 600; font-size: 15px; padding: 14px 32px; border-radius: 10px; margin-bottom: 24px;"">
      Sign In to Kollector Scüm
    </a>
    <p style=""color: #6b7280; font-size: 12px; margin-top: 24px;"">
      If you did not request this email, you can safely ignore it.<br />
      This link will expire in 15 minutes.
    </p>
  </div>
</body>
</html>";
        }
    }
}
