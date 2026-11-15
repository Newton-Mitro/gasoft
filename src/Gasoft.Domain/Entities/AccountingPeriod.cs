namespace Gasoft.Domain.Entities;

public sealed class AccountingPeriod : TenantOwnedEntity
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public long FiscalYearId { get; set; }
    public DateOnly StartsOn { get; set; }
    public DateOnly EndsOn { get; set; }
    public string Status { get; set; } = "Open";
}
