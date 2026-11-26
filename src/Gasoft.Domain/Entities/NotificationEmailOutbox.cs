namespace Gasoft.Domain.Entities;

public sealed class NotificationEmailOutbox : TenantOwnedEntity
{
    public long Id { get; set; }
    public long NotificationId { get; set; }
    public long UserId { get; set; }
    public required string EventType { get; set; }
    public required string Title { get; set; }
    public required string Message { get; set; }
    public string Status { get; set; } = "Pending";
    public int AttemptCount { get; set; }
    public DateTimeOffset NextAttemptAt { get; set; }
    public DateTimeOffset? LeaseExpiresAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public string? LastError { get; set; }
    public AppNotification Notification { get; set; } = null!;
}
