using Gasoft.Application.Abstractions;
using Gasoft.Application.Inventory;
using Gasoft.Application.Common;
using Gasoft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Gasoft.Infrastructure.Persistence;

public sealed partial class StockMovementService
{
    public Task<StockMovementDto> ReceiveAsync(
        long companyId,
        ReceiveStockRequest request,
        CancellationToken cancellationToken) =>
        RecordMovementAsync(
            companyId,
            request.MovementNumber,
            request.ItemId,
            request.WarehouseId,
            request.WarehouseLocationId,
            request.JournalId,
            request.AccountingPeriodId,
            request.InventoryAccountId,
            request.OffsetAccountId,
            request.MovementDate,
            request.Quantity,
            request.UnitCost,
            receipt: true,
            cancellationToken,
            request.PurchaseOrderLineId);

    public Task<StockMovementDto> IssueAsync(
        long companyId,
        IssueStockRequest request,
        CancellationToken cancellationToken) =>
        RecordMovementAsync(
            companyId,
            request.MovementNumber,
            request.ItemId,
            request.WarehouseId,
            request.WarehouseLocationId,
            request.JournalId,
            request.AccountingPeriodId,
            request.InventoryAccountId,
            request.CostOfGoodsSoldAccountId,
            request.MovementDate,
            request.Quantity,
            unitCost: null,
            receipt: false,
            cancellationToken);

    private async Task<StockMovementDto> RecordMovementAsync(
        long companyId,
        string movementNumber,
        long itemId,
        long warehouseId,
        long? locationId,
        long journalId,
        long periodId,
        long inventoryAccountId,
        long offsetAccountId,
        DateOnly movementDate,
        decimal quantity,
        decimal? unitCost,
        bool receipt,
        CancellationToken cancellationToken,
        long? purchaseOrderLineId = null)
    {
        var tenantId = EnsureTenant();
        var company = await EnsureCompanyAsync(companyId, cancellationToken);
        var item = await db.Items.SingleOrDefaultAsync(
            candidate => candidate.Id == itemId && candidate.IsActive,
            cancellationToken);
        if (item is null)
        {
            throw Validation("itemId", "Choose an active item in this tenant.");
        }

        if (!await db.Warehouses.AnyAsync(
                warehouse => warehouse.Id == warehouseId && warehouse.CompanyId == companyId,
                cancellationToken))
        {
            throw Validation("warehouseId", "Choose a warehouse in this company.");
        }

        if (locationId.HasValue && !await db.WarehouseLocations.AnyAsync(
                location => location.Id == locationId &&
                            location.WarehouseId == warehouseId &&
                            location.CompanyId == companyId,
                cancellationToken))
        {
            throw Validation("warehouseLocationId", "Choose a location in the selected warehouse.");
        }

        var normalizedNumber = Normalize(movementNumber).ToUpperInvariant();
        if (normalizedNumber.Length is 0 or > 40)
        {
            throw Validation("movementNumber", "Movement number is required and must be at most 40 characters.");
        }

        if (quantity <= 0 || quantity > 9_999_999_999.999999m || decimal.Round(quantity, 6) != quantity)
        {
            throw Validation("quantity", "Quantity must be positive and support at most 6 decimal places.");
        }

        var period = await db.AccountingPeriods.SingleOrDefaultAsync(
            candidate => candidate.Id == periodId && candidate.CompanyId == companyId,
            cancellationToken);
        if (period is null || movementDate < period.StartsOn || movementDate > period.EndsOn)
        {
            throw Validation("accountingPeriodId", "Choose an accounting period in this company that contains the movement date.");
        }

        if (period.Status != "Open")
        {
            throw new InventoryConflictException("Stock cannot be posted to a closed accounting period.");
        }

        if (!await db.Journals.AnyAsync(
                journal => journal.Id == journalId && journal.CompanyId == companyId && journal.IsActive,
                cancellationToken))
        {
            throw Validation("journalId", "Choose an active journal in this company.");
        }

        await EnsureAccountAsync(companyId, inventoryAccountId, "Asset", cancellationToken);
        var functionalPlaces = await db.Currencies
            .Where(currency => currency.Code == company.FunctionalCurrencyCode)
            .Select(currency => (int)currency.DecimalPlaces)
            .SingleAsync(cancellationToken);
        decimal calculatedUnitCost;
        decimal totalCost;
        List<InventoryValuationLayer> consumedLayers = [];
        if (receipt)
        {
            calculatedUnitCost = unitCost ?? 0;
            if (calculatedUnitCost <= 0 || calculatedUnitCost > MaximumAmount ||
                decimal.Round(calculatedUnitCost, 6) != calculatedUnitCost)
            {
                throw Validation("unitCost", "Unit cost must be positive and support at most 6 decimal places.");
            }

            if (offsetAccountId == inventoryAccountId)
            {
                throw Validation("offsetAccountId", "Offset account must differ from the inventory account.");
            }

            await EnsureOneOfAccountTypesAsync(
                companyId, offsetAccountId, ["Asset", "Liability"], cancellationToken);
            totalCost = decimal.Round(
                quantity * calculatedUnitCost,
                functionalPlaces,
                MidpointRounding.AwayFromZero);
        }
        else
        {
            await EnsureAccountAsync(companyId, offsetAccountId, "Expense", cancellationToken);
            var layers = await (
                from layer in db.InventoryValuationLayers
                join stockMovementRecord in db.StockMovements on new
                {
                    layer.TenantId,
                    layer.CompanyId,
                    StockMovementId = layer.StockMovementId
                } equals new
                {
                    stockMovementRecord.TenantId,
                    stockMovementRecord.CompanyId,
                    StockMovementId = stockMovementRecord.Id
                }
                where layer.ItemId == itemId &&
                      layer.WarehouseId == warehouseId &&
                      layer.RemainingQuantity > 0
                orderby stockMovementRecord.MovementDate, layer.Id
                select layer)
                .ToListAsync(cancellationToken);
            var available = layers.Sum(layer => layer.RemainingQuantity);
            if (quantity > available)
            {
                throw Validation("quantity", $"Requested issue {quantity} exceeds available stock {available}.");
            }

            if (item.CostingMethod == "AVCO")
            {
                var average = decimal.Round(
                    layers.Sum(layer => layer.RemainingQuantity * layer.UnitCost) / available,
                    6,
                    MidpointRounding.AwayFromZero);
                foreach (var layer in layers)
                {
                    layer.UnitCost = average;
                    layer.Revision++;
                }
            }

            var remainingToIssue = quantity;
            foreach (var layer in layers)
            {
                if (remainingToIssue == 0)
                {
                    break;
                }

                var quantityFromLayer = Math.Min(remainingToIssue, layer.RemainingQuantity);
                layer.RemainingQuantity -= quantityFromLayer;
                layer.Revision++;
                remainingToIssue -= quantityFromLayer;
                consumedLayers.Add(layer);
            }

            var issueCost = item.CostingMethod == "AVCO"
                ? quantity * layers[0].UnitCost
                : 0m;
            if (item.CostingMethod == "FIFO")
            {
                var originalLayers = await db.InventoryValuationLayers.AsNoTracking()
                    .Where(layer => consumedLayers.Select(consumed => consumed.Id).Contains(layer.Id))
                    .ToDictionaryAsync(layer => layer.Id, cancellationToken);
                issueCost = consumedLayers.Sum(layer =>
                    (originalLayers[layer.Id].RemainingQuantity - layer.RemainingQuantity) * layer.UnitCost);
            }

            var roundedCost = decimal.Round(issueCost, functionalPlaces, MidpointRounding.AwayFromZero);
            if (roundedCost <= 0 || roundedCost > MaximumAmount)
            {
                throw Validation("quantity", "Issued stock has no positive supported carrying value.");
            }

            calculatedUnitCost = decimal.Round(roundedCost / quantity, 6, MidpointRounding.AwayFromZero);
            totalCost = roundedCost;
        }

        if (totalCost <= 0 || totalCost > MaximumAmount)
        {
            throw Validation("unitCost", "Movement total cost is outside the supported functional-currency range.");
        }

        PurchaseOrderLine? purchaseOrderLine = null;
        PurchaseOrder? purchaseOrder = null;
        if (purchaseOrderLineId.HasValue)
        {
            if (!receipt)
                throw Validation("purchaseOrderLineId", "Purchase order lines can only be linked to stock receipts.");
            purchaseOrderLine = await db.PurchaseOrderLines.SingleOrDefaultAsync(
                line => line.Id == purchaseOrderLineId.Value &&
                        line.CompanyId == companyId &&
                        line.ItemId == itemId,
                cancellationToken);
            if (purchaseOrderLine is null)
                throw Validation("purchaseOrderLineId", "Choose a purchase order line for this item in this company.");
            purchaseOrder = await db.PurchaseOrders.SingleOrDefaultAsync(
                order => order.Id == purchaseOrderLine.PurchaseOrderId &&
                         order.CompanyId == companyId,
                cancellationToken);
            if (purchaseOrder is null ||
                purchaseOrder.Status is not ("Ordered" or "PartiallyReceived") ||
                purchaseOrder.WarehouseId != warehouseId)
                throw Validation("purchaseOrderLineId", "The order must be open for receiving and use the selected warehouse.");
            if (purchaseOrderLine.ReceivedQuantity + quantity > purchaseOrderLine.Quantity)
                throw Validation("quantity", "Receipt quantity exceeds the remaining quantity on the purchase order line.");
        }

        var journalEntry = new JournalEntry
        {
            TenantId = tenantId,
            CompanyId = companyId,
            JournalId = journalId,
            AccountingPeriodId = periodId,
            EntryDate = movementDate,
            Description = $"{(receipt ? "Stock receipt" : "Stock issue")} {normalizedNumber}",
            Status = "Posted",
            TotalDebit = totalCost,
            TotalCredit = totalCost,
            Lines =
            [
                new JournalLine
                {
                    TenantId = tenantId,
                    CompanyId = companyId,
                    AccountId = receipt ? inventoryAccountId : offsetAccountId,
                    TransactionCurrencyCode = company.FunctionalCurrencyCode,
                    Debit = receipt ? totalCost : 0,
                    Credit = receipt ? 0 : totalCost,
                    FunctionalDebit = receipt ? totalCost : 0,
                    FunctionalCredit = receipt ? 0 : totalCost,
                    ExchangeRate = 1,
                    Description = receipt ? "Inventory received" : "Cost of goods sold"
                },
                new JournalLine
                {
                    TenantId = tenantId,
                    CompanyId = companyId,
                    AccountId = receipt ? offsetAccountId : inventoryAccountId,
                    TransactionCurrencyCode = company.FunctionalCurrencyCode,
                    Debit = receipt ? 0 : totalCost,
                    Credit = receipt ? totalCost : 0,
                    FunctionalDebit = receipt ? 0 : totalCost,
                    FunctionalCredit = receipt ? totalCost : 0,
                    ExchangeRate = 1,
                    Description = receipt ? "Stock clearing" : "Inventory relieved"
                }
            ]
        };

        var movement = new StockMovement
        {
            TenantId = tenantId,
            CompanyId = companyId,
            ItemId = itemId,
            WarehouseId = warehouseId,
            WarehouseLocationId = locationId,
            MovementNumber = normalizedNumber,
            PurchaseOrderLineId = purchaseOrderLineId,
            MovementType = receipt ? "Receipt" : "Issue",
            MovementDate = movementDate,
            QuantityIn = receipt ? quantity : 0,
            QuantityOut = receipt ? 0 : quantity,
            UnitCost = calculatedUnitCost,
            TotalCost = totalCost
        };

        await using var transaction = db.Database.CurrentTransaction is null
            ? await db.Database.BeginTransactionAsync(cancellationToken)
            : null;
        db.JournalEntries.Add(journalEntry);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            movement.JournalEntryId = journalEntry.Id;
            db.StockMovements.Add(movement);
            await db.SaveChangesAsync(cancellationToken);
            if (receipt)
            {
                var layers = await db.InventoryValuationLayers
                    .Where(layer => layer.ItemId == itemId &&
                                    layer.WarehouseId == warehouseId &&
                                    layer.RemainingQuantity > 0)
                    .ToListAsync(cancellationToken);
                if (item.CostingMethod == "AVCO" && layers.Count > 0)
                {
                    var existingQuantity = layers.Sum(layer => layer.RemainingQuantity);
                    var existingValue = layers.Sum(layer => layer.RemainingQuantity * layer.UnitCost);
                    var averageCost = decimal.Round(
                        (existingValue + quantity * calculatedUnitCost) / (existingQuantity + quantity),
                        6,
                        MidpointRounding.AwayFromZero);
                    foreach (var layer in layers)
                    {
                        layer.UnitCost = averageCost;
                        layer.Revision++;
                    }

                    calculatedUnitCost = averageCost;
                }

                db.InventoryValuationLayers.Add(new InventoryValuationLayer
                {
                    TenantId = tenantId,
                    CompanyId = companyId,
                    StockMovementId = movement.Id,
                    ItemId = itemId,
                    WarehouseId = warehouseId,
                    RemainingQuantity = quantity,
                    UnitCost = calculatedUnitCost
                });
                if (purchaseOrderLine is not null && purchaseOrder is not null)
                {
                    purchaseOrderLine.ReceivedQuantity += quantity;
                    purchaseOrderLine.Revision++;
                    var allLines = await db.PurchaseOrderLines
                        .Where(line => line.PurchaseOrderId == purchaseOrder.Id)
                        .ToListAsync(cancellationToken);
                    purchaseOrder.Status = allLines.All(line =>
                        line.ReceivedQuantity >= line.Quantity)
                        ? "Received"
                        : "PartiallyReceived";
                    purchaseOrder.Revision++;
                }
            }
            else
            {
                if (transaction is not null) await transaction.CommitAsync(cancellationToken);
                return ToDto(movement);
            }

            await db.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InventoryConflictException("Stock changed concurrently. Reload the balance and retry.");
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            throw new InventoryConflictException("A movement with that number already exists in this company.");
        }

        return ToDto(movement);
    }
}
