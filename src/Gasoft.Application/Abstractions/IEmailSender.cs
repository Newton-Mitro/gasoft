namespace Gasoft.Application.Abstractions;

public interface IEmailSender
{
    bool IsConfigured { get; }
    bool CanSendEmail { get; }

    Task SendPasswordResetAsync(string email, string resetLink, CancellationToken cancellationToken);
    Task SendEmailAsync(
        string email,
        string subject,
        string htmlBody,
        string textBody,
        CancellationToken cancellationToken);
}
