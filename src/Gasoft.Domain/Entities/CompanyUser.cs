namespace Gasoft.Domain.Entities;

public sealed class CompanyUser : TenantOwnedEntity
{
    public long CompanyId { get; set; }
    public long UserId { get; set; }
    public string Role { get; set; } = "Member";
    public bool IsActive { get; set; } = true;
}
