using ClinicBook.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace ClinicBook.Infrastructure.Identity;

/// <summary>
/// Development email sender that logs messages instead of delivering them.
/// </summary>
public sealed class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    /// <summary>Initializes a new instance.</summary>
    public LoggingEmailSender(ILogger<LoggingEmailSender> logger) => _logger = logger;

    /// <inheritdoc />
    public Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
#pragma warning disable CA1848
        _logger.LogInformation("Email to {ToEmail}. Subject: {Subject}. BodyLength: {BodyLength}", toEmail, subject, htmlBody?.Length ?? 0);
#pragma warning restore CA1848
        return Task.CompletedTask;
    }
}
