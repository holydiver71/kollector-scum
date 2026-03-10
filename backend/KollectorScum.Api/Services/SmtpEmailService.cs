using System.Net;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using KollectorScum.Api.Interfaces;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// Email service implementation that supports both standard SMTP and the Resend HTTP API.
    /// When <c>Email:SmtpHost</c> is <c>smtp.resend.com</c> the Resend REST API is used instead
    /// of raw SMTP (cloud hosts such as Render block outbound SMTP ports).
    /// When <c>Email:SmtpHost</c> is not configured at all the magic link is logged to the
    /// console, which is convenient for local development without an email provider.
    /// </summary>
    public class SmtpEmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmtpEmailService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
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

            var subject = "Your Kollector Scum Sign-In Link";
            var body = BuildEmailBody(magicLink);

            // Resend's SMTP endpoint blocks on cloud providers; use their HTTP API instead.
            if (smtpHost.Equals("smtp.resend.com", StringComparison.OrdinalIgnoreCase))
            {
                var apiKey = emailSection["SmtpPassword"];
                await SendViaResendApiAsync(toEmail, fromAddress, fromName, subject, body, apiKey);
                return;
            }

            if (!int.TryParse(smtpPortStr, out var smtpPort))
            {
                smtpPort = 587;
            }

            bool.TryParse(enableSslStr, out var enableSsl);

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
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 15_000 // 15 seconds — prevents long hangs on unreachable hosts
            };

            if (!string.IsNullOrWhiteSpace(smtpUsername))
            {
                client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
            }

            try
            {
                await client.SendMailAsync(message);
                _logger.LogInformation("Magic link email sent to {Email}", toEmail);
            }
            catch (Exception ex)
            {
                // Log the magic link so it is recoverable from server logs (useful during staging
                // when SMTP may not be fully configured).
                _logger.LogError(ex,
                    "Failed to send magic link email to {Email} via {Host}:{Port}. " +
                    "Magic link (use for manual testing): {MagicLink}",
                    toEmail, smtpHost, smtpPort, magicLink);
                // Re-throw so the caller knows delivery failed.
                throw;
            }
        }

        /// <summary>
        /// Sends the magic link email via the Resend REST API (HTTPS port 443).
        /// Used in preference to SMTP when the configured host is smtp.resend.com, because
        /// cloud providers commonly block outbound connections on SMTP ports.
        /// </summary>
        private async Task SendViaResendApiAsync(
            string toEmail, string fromAddress, string fromName,
            string subject, string htmlBody, string? apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException(
                    "Resend API key (Email:SmtpPassword) is not configured.");
            }

            var payload = new
            {
                from = $"{fromName} <{fromAddress}>",
                to = new[] { toEmail },
                subject,
                html = htmlBody
            };

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);
            client.Timeout = TimeSpan.FromSeconds(15);

            var response = await client.PostAsync("https://api.resend.com/emails", content);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Resend API returned {(int)response.StatusCode}: {body}");
            }

            _logger.LogInformation("Magic link email sent to {Email} via Resend API", toEmail);
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
