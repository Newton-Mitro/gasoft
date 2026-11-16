namespace Gasoft.Domain.Entities;

public sealed class BankAccount : TenantOwnedEntity
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public long ChartOfAccountId { get; set; }
    public required string Name { get; set; }
    public required string BankName { get; set; }
    public required string AccountNumberLastFour { get; set; }
    public required string CurrencyCode { get; set; }
    public bool IsActive { get; set; } = true;
}
