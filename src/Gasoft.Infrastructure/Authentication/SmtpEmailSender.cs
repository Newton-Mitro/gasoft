using System.Net;
using System.Net.Mail;
using System.Text.Encodings.Web;
using Gasoft.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace Gasoft.Infrastructure.Authentication;

public sealed class SmtpEmailSender(IOptions<EmailOptions> options) : IEmailSender
{
    private readonly EmailOptions settings = options.Value;

    public bool IsConfigured => settings.IsConfigured;

    public async Task SendPasswordResetAsync(
        string email,
        string resetLink,
        CancellationToken cancellationToken)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("Password reset email settings are incomplete.");

        var safeLink = HtmlEncoder.Default.Encode(resetLink);
        using var message = new MailMessage
        {
            From = new MailAddress(settings.FromAddress, settings.FromName),
            Subject = "Reset your Gasoft password",
            Body = $"""
                <p>We received a request to reset the password for your Gasoft account.</p>
                <p><a href="{safeLink}">Reset your password</a></p>
                <p>This link expires in 30 minutes. If you did not request a reset, you can ignore this email.</p>
                """,
            IsBodyHtml = true,
        };
        message.To.Add(new MailAddress(email));
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(
            $"Reset your Gasoft password using this link: {resetLink}\n\nThis link expires in 30 minutes. If you did not request a reset, ignore this email.",
            null,
            "text/plain"));

        using var client = new SmtpClient(settings.SmtpHost, settings.SmtpPort)
        {
            EnableSsl = true,
            UseDefaultCredentials = false,
        };
        if (!string.IsNullOrWhiteSpace(settings.SmtpUsername))
            client.Credentials = new NetworkCredential(settings.SmtpUsername, settings.SmtpPassword);

        await client.SendMailAsync(message, cancellationToken);
    }
}
