namespace Gasoft.Domain.Entities;

public sealed class BudgetLine : TenantOwnedEntity
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public long BudgetId { get; set; }
    public long AccountingPeriodId { get; set; }
    public long AccountId { get; set; }
    public decimal Amount { get; set; }
}
