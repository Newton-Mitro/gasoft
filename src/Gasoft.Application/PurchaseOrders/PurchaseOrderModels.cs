using Gasoft.Application.Inventory;

namespace Gasoft.Application.PurchaseOrders;

public sealed record PurchaseOrderRequest(
    long SupplierId,
    long WarehouseId,
    string? OrderNumber,
    DateOnly OrderDate,
    DateOnly? ExpectedDate,
    string? CurrencyCode,
    decimal ExchangeRate,
    IReadOnlyList<PurchaseOrderLineRequest>? Lines,
    int Revision = 0);

public sealed record PurchaseOrderLineRequest(long ItemId, string? Description, decimal Quantity, decimal UnitCost);

public sealed record PurchaseOrderRevisionRequest(int Revision);

public sealed record ReceivePurchaseOrderLineRequest(
    string? MovementNumber,
    long? WarehouseLocationId,
    long JournalId,
    long AccountingPeriodId,
    long InventoryAccountId,
    long OffsetAccountId,
    DateOnly MovementDate,
    decimal Quantity);

public sealed record PurchaseOrderLineDto(
    long Id,
    long ItemId,
    string Sku,
    string ItemName,
    string UnitCode,
    string Description,
    decimal Quantity,
    decimal ReceivedQuantity,
    decimal UnitCost);

public sealed record PurchaseOrderDto(
    long Id,
    long CompanyId,
    long SupplierId,
    string SupplierName,
    long WarehouseId,
    string OrderNumber,
    DateOnly OrderDate,
    DateOnly? ExpectedDate,
    string CurrencyCode,
    decimal ExchangeRate,
    string Status,
    int Revision,
    IReadOnlyList<PurchaseOrderLineDto> Lines);

public sealed record PurchaseOrderStatusDto(long Id, string Status, int Revision);

public sealed record PurchaseOrderReceiptDto(StockMovementDto Movement, string PurchaseOrderStatus);

public sealed class PurchaseOrderValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("The purchase order request is invalid.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}

public sealed class PurchaseOrderConflictException(string message) : Exception(message);
