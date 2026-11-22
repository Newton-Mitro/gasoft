namespace Gasoft.Application.Pos;

public sealed record PosSettingsDto(
    long? WarehouseId,
    long? JournalId,
    long? AccountingPeriodId,
    long? RevenueAccountId,
    long? ReceivableAccountId,
    long? SettlementAccountId,
    long? TaxCodeId,
    long? TaxAccountId,
    long? InventoryAccountId,
    long? CostOfGoodsSoldAccountId);

public sealed record SavePosSettingsRequest(
    long? WarehouseId,
    long? JournalId,
    long? AccountingPeriodId,
    long? RevenueAccountId,
    long? ReceivableAccountId,
    long? SettlementAccountId,
    long? TaxCodeId,
    long? TaxAccountId,
    long? InventoryAccountId,
    long? CostOfGoodsSoldAccountId);

public sealed class PosSettingsValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("The POS settings request is invalid.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
