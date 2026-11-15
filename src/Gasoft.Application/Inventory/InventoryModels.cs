namespace Gasoft.Application.Inventory;

public sealed record CreateItemCategoryRequest(string Code, string Name);
public sealed record UpdateItemCategoryRequest(string Code, string Name);
public sealed record CreateItemRequest(
    long CategoryId,
    string Sku,
    string Name,
    string BaseUnitCode,
    string CostingMethod);
public sealed record UpdateItemRequest(
    long CategoryId,
    string Sku,
    string Name,
    string BaseUnitCode,
    string CostingMethod);
public sealed record CreateWarehouseRequest(string Code, string Name);
public sealed record UpdateWarehouseRequest(string Code, string Name);
public sealed record CreateWarehouseLocationRequest(string Code, string Name);
public sealed record ReceiveStockRequest(
    string MovementNumber,
    long ItemId,
    long WarehouseId,
    long? WarehouseLocationId,
    long JournalId,
    long AccountingPeriodId,
    long InventoryAccountId,
    long OffsetAccountId,
    DateOnly MovementDate,
    decimal Quantity,
    decimal UnitCost);
public sealed record IssueStockRequest(
    string MovementNumber,
    long ItemId,
    long WarehouseId,
    long? WarehouseLocationId,
    long JournalId,
    long AccountingPeriodId,
    long InventoryAccountId,
    long CostOfGoodsSoldAccountId,
    DateOnly MovementDate,
    decimal Quantity);
public sealed record CreateInventoryCountRequest(
    long WarehouseId,
    string CountNumber,
    DateOnly CountDate,
    IReadOnlyList<CountInventoryItemRequest> Items);
public sealed record UpdateInventoryCountRequest(
    long WarehouseId,
    string CountNumber,
    DateOnly CountDate,
    IReadOnlyList<CountInventoryItemRequest> Items);
public sealed record CountInventoryItemRequest(long ItemId, decimal CountedQuantity, decimal? AdjustmentUnitCost);
public sealed record PostInventoryCountRequest(
    long JournalId,
    long AccountingPeriodId,
    long InventoryAccountId,
    long AdjustmentAccountId);

public sealed record ItemCategoryDto(long Id, string Code, string Name);
public sealed record UnitOfMeasureDto(string Code, string Name, byte DecimalPlaces);
public sealed record ItemDto(long Id, long CategoryId, string Sku, string Name, string BaseUnitCode, string CostingMethod);
public sealed record WarehouseDto(long Id, long CompanyId, string Code, string Name);
public sealed record WarehouseLocationDto(long Id, long WarehouseId, string Code, string Name);
public sealed record StockMovementDto(
    long Id,
    long ItemId,
    long WarehouseId,
    long? WarehouseLocationId,
    long? JournalEntryId,
    string MovementNumber,
    string MovementType,
    DateOnly MovementDate,
    decimal QuantityIn,
    decimal QuantityOut,
    decimal UnitCost,
    decimal TotalCost);
public sealed record StockBalanceDto(long ItemId, long WarehouseId, decimal Quantity, decimal Value);
public sealed record InventoryCountLineDto(
    long ItemId,
    decimal SystemQuantity,
    decimal SystemValue,
    decimal CountedQuantity,
    decimal? AdjustmentUnitCost);
public sealed record InventoryCountDto(
    long Id,
    long WarehouseId,
    string CountNumber,
    DateOnly CountDate,
    string Status,
    long? JournalEntryId,
    IReadOnlyList<InventoryCountLineDto> Items);

public sealed class InventoryValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("The inventory request is invalid.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}

public sealed class InventoryConflictException(string message) : Exception(message);
