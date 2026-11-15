namespace Gasoft.Domain.Entities;

public sealed class FiscalYear : TenantOwnedEntity
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public DateOnly StartsOn { get; set; }
    public DateOnly EndsOn { get; set; }
    public string Status { get; set; } = "Open";
}
