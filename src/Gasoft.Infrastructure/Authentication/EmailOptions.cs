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
        !string.IsNullOrWhiteSpace(SmtpHost) &&
        SmtpPort is > 0 and <= 65535 &&
        !string.IsNullOrWhiteSpace(FromAddress);
}
