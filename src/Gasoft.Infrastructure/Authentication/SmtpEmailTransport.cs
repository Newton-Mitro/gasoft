using System.Net;
using System.Net.Mail;
using Gasoft.Application.Abstractions;

namespace Gasoft.Infrastructure.Authentication;

public sealed class SmtpEmailTransport : ISmtpEmailTransport
{
    public async Task SendAsync(
        SmtpDeliverySettings settings,
        string email,
        string subject,
        string htmlBody,
        string textBody,
        CancellationToken cancellationToken)
    {
        using var message = new MailMessage
        {
            From = new MailAddress(settings.FromEmail, settings.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true,
        };
        message.To.Add(new MailAddress(email));
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(
            textBody,
            null,
            "text/plain"));

        using var client = new SmtpClient(settings.Host, settings.Port)
        {
            EnableSsl = true,
            UseDefaultCredentials = false,
        };
        if (!string.IsNullOrWhiteSpace(settings.Username))
            client.Credentials = new NetworkCredential(settings.Username, settings.Password);

        await client.SendMailAsync(message, cancellationToken);
    }
}
