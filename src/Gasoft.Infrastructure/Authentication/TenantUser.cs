namespace Gasoft.Infrastructure.Authentication;

public sealed class TenantUser
{
    public long TenantId { get; set; }
    public long UserId { get; set; }
    public bool IsActive { get; set; }
    public required string Role { get; set; }
}
