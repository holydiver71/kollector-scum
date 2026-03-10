namespace KollectorScum.Api.Interfaces
{
    /// <summary>
    /// Service interface for sending email messages
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Sends a magic link email to the specified address
        /// </summary>
        /// <param name="toEmail">The recipient email address</param>
        /// <param name="magicLink">The full magic link URL</param>
        Task SendMagicLinkEmailAsync(string toEmail, string magicLink);
    }
}
