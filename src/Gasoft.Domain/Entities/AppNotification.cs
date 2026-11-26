namespace Gasoft.Domain.Entities;

public sealed class AppNotification : TenantOwnedEntity
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public required string Title { get; set; }
    public required string Message { get; set; }
    public required string Category { get; set; }
    public required string EventType { get; set; }
    public string? Link { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
    public DateTimeOffset? DismissedAt { get; set; }
}
