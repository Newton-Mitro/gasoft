namespace Gasoft.Domain.Entities;

public sealed class TenantEmailConfiguration : TenantOwnedEntity
{
    public required string SmtpHost { get; set; }
    public int SmtpPort { get; set; }
    public string? SmtpUsername { get; set; }
    public string? ProtectedSmtpPassword { get; set; }
    public required string FromEmail { get; set; }
    public required string FromName { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
