namespace Gasoft.Domain.Entities;

public sealed class ChartOfAccount : TenantOwnedEntity
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public long AccountGroupId { get; set; }
    public long? ParentId { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public bool AllowPosting { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public string? CashFlowCategory { get; set; }
}
