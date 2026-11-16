namespace Gasoft.Domain.Entities;

public sealed class FixedAsset : TenantOwnedEntity
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public required string AssetNumber { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public DateOnly AcquisitionDate { get; set; }
    public DateOnly InServiceDate { get; set; }
    public decimal AcquisitionCost { get; set; }
    public decimal ResidualValue { get; set; }
    public int UsefulLifeMonths { get; set; }
    public long AssetAccountId { get; set; }
    public long AccumulatedDepreciationAccountId { get; set; }
    public long DepreciationExpenseAccountId { get; set; }
    public long AcquisitionJournalEntryId { get; set; }
    public string Status { get; set; } = "Active";
    public DateOnly? DisposalDate { get; set; }
    public decimal? DisposalProceeds { get; set; }
    public long? DisposalJournalEntryId { get; set; }
}
