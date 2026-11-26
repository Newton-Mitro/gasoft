namespace Gasoft.Domain.Entities;

public sealed class NotificationPreference : TenantOwnedEntity
{
    public long UserId { get; set; }
    public required string EventType { get; set; }
    public bool InAppEnabled { get; set; }
    public bool EmailEnabled { get; set; }
}
