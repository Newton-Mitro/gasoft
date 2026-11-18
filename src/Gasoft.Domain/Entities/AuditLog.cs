namespace Gasoft.Domain.Entities;

public sealed class AuditLog
{
    public long Id { get; set; }
    public long? TenantId { get; set; }
    public long? ActorUserId { get; set; }
    public required string Action { get; set; }
    public required string EntityType { get; set; }
    public string? EntityId { get; set; }
    public required string DetailsJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
