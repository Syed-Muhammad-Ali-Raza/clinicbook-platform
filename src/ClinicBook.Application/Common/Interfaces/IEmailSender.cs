namespace ClinicBook.Application.Common.Interfaces;

/// <summary>
/// Sends transactional emails.
/// </summary>
public interface IEmailSender
{
    /// <summary>Sends an email message.</summary>
    Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default);
}
