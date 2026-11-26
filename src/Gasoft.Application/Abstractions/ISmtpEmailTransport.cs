namespace Gasoft.Application.Abstractions;

public sealed record SmtpDeliverySettings(
    string Host,
    int Port,
    string? Username,
    string? Password,
    string FromEmail,
    string FromName);

public interface ISmtpEmailTransport
{
    Task SendAsync(
        SmtpDeliverySettings settings,
        string email,
        string subject,
        string htmlBody,
        string textBody,
        CancellationToken cancellationToken);
}
