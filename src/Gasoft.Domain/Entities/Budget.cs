namespace Gasoft.Domain.Entities;

public sealed class Budget : TenantOwnedEntity
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public long FiscalYearId { get; set; }
    public required string Name { get; set; }
    public ICollection<BudgetLine> Lines { get; set; } = new List<BudgetLine>();
}
