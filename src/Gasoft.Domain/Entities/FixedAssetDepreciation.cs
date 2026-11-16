namespace Gasoft.Domain.Entities;

public sealed class FixedAssetDepreciation : TenantOwnedEntity
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public long FixedAssetId { get; set; }
    public long AccountingPeriodId { get; set; }
    public long JournalEntryId { get; set; }
    public DateOnly DepreciationDate { get; set; }
    public decimal Amount { get; set; }
}
