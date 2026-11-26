using System.Net.Mail;

namespace Gasoft.Infrastructure.Authentication;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string FrontendBaseUrl { get; set; } = string.Empty;
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string SmtpUsername { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "Gasoft";

    public bool IsConfigured =>
        Uri.TryCreate(FrontendBaseUrl, UriKind.Absolute, out var frontendUri) &&
        frontendUri.Scheme is "http" or "https" &&
        IsSmtpConfigured;

    public bool IsSmtpConfigured =>
        !string.IsNullOrWhiteSpace(SmtpHost) &&
        SmtpHost.Length <= 253 &&
        Uri.CheckHostName(SmtpHost) != UriHostNameType.Unknown &&
        !SmtpHost.Any(char.IsWhiteSpace) &&
        SmtpPort is > 0 and <= 65535 &&
        MailAddress.TryCreate(FromAddress, out var fromAddress) &&
        string.Equals(fromAddress.Address, FromAddress, StringComparison.OrdinalIgnoreCase) &&
        (string.IsNullOrWhiteSpace(SmtpUsername) || !string.IsNullOrEmpty(SmtpPassword));
}
