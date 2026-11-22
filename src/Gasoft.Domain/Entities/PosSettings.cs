namespace Gasoft.Domain.Entities;

public sealed class PosSettings : TenantOwnedEntity
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public long WarehouseId { get; set; }
    public long JournalId { get; set; }
    public long AccountingPeriodId { get; set; }
    public long RevenueAccountId { get; set; }
    public long ReceivableAccountId { get; set; }
    public long SettlementAccountId { get; set; }
    public long? TaxCodeId { get; set; }
    public long? TaxAccountId { get; set; }
    public long InventoryAccountId { get; set; }
    public long CostOfGoodsSoldAccountId { get; set; }
}
