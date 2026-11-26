using System.Text.Encodings.Web;
using Gasoft.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace Gasoft.Infrastructure.Authentication;

public sealed class SmtpEmailSender(
    IOptions<EmailOptions> options,
    ISmtpEmailTransport transport) : IEmailSender
{
    private readonly EmailOptions settings = options.Value;

    public bool IsConfigured => settings.IsConfigured;
    public bool CanSendEmail => settings.IsSmtpConfigured;

    public async Task SendPasswordResetAsync(
        string email,
        string resetLink,
        CancellationToken cancellationToken)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("Password reset email settings are incomplete.");

        var safeLink = HtmlEncoder.Default.Encode(resetLink);
        await SendEmailAsync(
            email,
            "Reset your Gasoft password",
            $"""
                <p>We received a request to reset the password for your Gasoft account.</p>
                <p><a href="{safeLink}">Reset your password</a></p>
                <p>This link expires in 30 minutes. If you did not request a reset, you can ignore this email.</p>
                """,
            $"Reset your Gasoft password using this link: {resetLink}\n\nThis link expires in 30 minutes. If you did not request a reset, ignore this email.",
            cancellationToken);
    }

    public async Task SendEmailAsync(
        string email,
        string subject,
        string htmlBody,
        string textBody,
        CancellationToken cancellationToken)
    {
        if (!CanSendEmail)
            throw new InvalidOperationException("SMTP email settings are incomplete.");

        await transport.SendAsync(
            new SmtpDeliverySettings(
                settings.SmtpHost,
                settings.SmtpPort,
                settings.SmtpUsername,
                settings.SmtpPassword,
                settings.FromAddress,
                settings.FromName),
            email,
            subject,
            htmlBody,
            textBody,
            cancellationToken);
    }
}
