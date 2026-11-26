using Gasoft.Application.Abstractions;
using Gasoft.Application.Inventory;
using Gasoft.Application.Common;
using Gasoft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Gasoft.Infrastructure.Persistence;

public sealed partial class InventoryService
{
    public async Task<IReadOnlyList<InventoryCountDto>> GetCountsAsync(
        long companyId,
        CancellationToken cancellationToken)
    {
        await EnsureCompanyAsync(companyId, cancellationToken);
        var counts = await db.InventoryCounts.AsNoTracking()
            .Where(count => count.CompanyId == companyId)
            .OrderByDescending(count => count.Id)
            .ToListAsync(cancellationToken);
        var countIds = counts.Select(count => count.Id).ToArray();
        var lines = await db.InventoryCountLines.AsNoTracking()
            .Where(line => line.CompanyId == companyId && countIds.Contains(line.InventoryCountId))
            .OrderBy(line => line.ItemId)
            .ToListAsync(cancellationToken);
        return counts.Select(count => ToCountDto(
            count,
            lines.Where(line => line.InventoryCountId == count.Id))).ToArray();
    }

    public async Task<PagedResultDto<InventoryCountDto>> GetCountsPageAsync(
        long companyId, int page, int pageSize, CancellationToken cancellationToken)
    {
        await EnsureCompanyAsync(companyId, cancellationToken);
        var result = await PagedQuery.ToPageAsync(
            db.InventoryCounts.AsNoTracking()
                .Where(count => count.CompanyId == companyId)
                .OrderByDescending(count => count.Id),
            page,
            pageSize,
            count => count,
            cancellationToken);
        var countIds = result.Items.Select(count => count.Id).ToArray();
        var lines = await db.InventoryCountLines.AsNoTracking()
            .Where(line => line.CompanyId == companyId && countIds.Contains(line.InventoryCountId))
            .OrderBy(line => line.ItemId)
            .ToListAsync(cancellationToken);
        var items = result.Items.Select(count => ToCountDto(
            count,
            lines.Where(line => line.InventoryCountId == count.Id))).ToArray();
        return new PagedResultDto<InventoryCountDto>(
            items, result.Page, result.PageSize, result.TotalCount, result.TotalPages);
    }

    public async Task<InventoryCountDto?> GetCountAsync(
        long companyId,
        long countId,
        CancellationToken cancellationToken)
    {
        await EnsureCompanyAsync(companyId, cancellationToken);
        var count = await db.InventoryCounts.AsNoTracking().SingleOrDefaultAsync(
            item => item.CompanyId == companyId && item.Id == countId, cancellationToken);
        if (count is null) return null;
        var lines = await db.InventoryCountLines.AsNoTracking()
            .Where(item => item.CompanyId == companyId && item.InventoryCountId == countId)
            .ToListAsync(cancellationToken);
        return ToCountDto(count, lines);
    }

    public async Task<InventoryCountDto> CreateCountAsync(
        long companyId,
        CreateInventoryCountRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        await EnsureCompanyAsync(companyId, cancellationToken);
        var countNumber = Normalize(request.CountNumber).ToUpperInvariant();
        if (countNumber.Length is 0 or > 40)
        {
            throw Validation("countNumber", "Count number is required and must be at most 40 characters.");
        }

        if (request.Items is null || request.Items.Count is 0 ||
            request.Items.Select(line => line.ItemId).Distinct().Count() != request.Items.Count)
        {
            throw Validation("items", "Provide at least one item and do not repeat item IDs.");
        }

        if (!await db.Warehouses.AnyAsync(
                warehouse => warehouse.Id == request.WarehouseId && warehouse.CompanyId == companyId,
                cancellationToken))
        {
            throw Validation("warehouseId", "Choose a warehouse in this company.");
        }

        var itemIds = request.Items.Select(line => line.ItemId).ToArray();
        var activeItems = await db.Items.AsNoTracking()
            .Where(item => itemIds.Contains(item.Id) && item.IsActive)
            .Select(item => item.Id)
            .ToListAsync(cancellationToken);
        if (activeItems.Count != itemIds.Length)
        {
            throw Validation("items", "Every counted item must be active and belong to this tenant.");
        }

        foreach (var item in request.Items)
        {
            if (item.CountedQuantity < 0 || item.CountedQuantity > 9_999_999_999.999999m ||
                decimal.Round(item.CountedQuantity, 6) != item.CountedQuantity)
            {
                throw Validation("items", "Counted quantities must be non-negative with at most 6 decimal places.");
            }

            if (item.AdjustmentUnitCost is <= 0 or > MaximumAmount ||
                item.AdjustmentUnitCost.HasValue && decimal.Round(item.AdjustmentUnitCost.Value, 6) != item.AdjustmentUnitCost)
            {
                throw Validation("items", "Adjustment unit costs, when provided, must be positive with at most 6 decimal places.");
            }
        }

        var snapshotMovementId = await db.StockMovements
            .Where(movement => movement.CompanyId == companyId && movement.WarehouseId == request.WarehouseId)
            .Select(movement => (long?)movement.Id)
            .MaxAsync(cancellationToken) ?? 0;
        var snapshot = await db.InventoryValuationLayers.AsNoTracking()
            .Where(layer => layer.CompanyId == companyId && layer.WarehouseId == request.WarehouseId &&
                            itemIds.Contains(layer.ItemId) && layer.RemainingQuantity > 0)
            .GroupBy(layer => layer.ItemId)
            .Select(group => new
            {
                ItemId = group.Key,
                Quantity = group.Sum(layer => layer.RemainingQuantity),
                Value = group.Sum(layer => layer.RemainingQuantity * layer.UnitCost)
            })
            .ToDictionaryAsync(result => result.ItemId, cancellationToken);

        var count = new InventoryCount
        {
            TenantId = tenantId,
            CompanyId = companyId,
            WarehouseId = request.WarehouseId,
            SnapshotMovementId = snapshotMovementId,
            CountNumber = countNumber,
            CountDate = request.CountDate
        };
        db.InventoryCounts.Add(count);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            var lines = request.Items.Select(item =>
            {
                var balance = snapshot.GetValueOrDefault(item.ItemId);
                return new InventoryCountLine
                {
                    TenantId = tenantId,
                    CompanyId = companyId,
                    InventoryCountId = count.Id,
                    ItemId = item.ItemId,
                    SystemQuantity = balance?.Quantity ?? 0,
                    SystemValue = balance?.Value ?? 0,
                    CountedQuantity = item.CountedQuantity,
                    AdjustmentUnitCost = item.AdjustmentUnitCost
                };
            }).ToArray();
            db.InventoryCountLines.AddRange(lines);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return ToCountDto(count, lines);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            throw new InventoryConflictException("A count with that number already exists in this company.");
        }
    }

    public async Task<InventoryCountDto?> UpdateCountAsync(
            long companyId,
            long countId,
            UpdateInventoryCountRequest request,
            CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        await EnsureCompanyAsync(companyId, cancellationToken);
        var count = await db.InventoryCounts.SingleOrDefaultAsync(
            item => item.CompanyId == companyId && item.Id == countId, cancellationToken);
        if (count is null) return null;
        if (count.Status != "Draft")
            throw new InventoryConflictException("Only draft inventory counts can be edited.");

        var countNumber = Normalize(request.CountNumber).ToUpperInvariant();
        if (countNumber.Length is 0 or > 40)
            throw Validation("countNumber", "Count number is required and must be at most 40 characters.");
        if (request.Items is null || request.Items.Count == 0 ||
            request.Items.Select(line => line.ItemId).Distinct().Count() != request.Items.Count)
            throw Validation("items", "Provide at least one item and do not repeat item IDs.");
        if (!await db.Warehouses.AnyAsync(
                item => item.Id == request.WarehouseId && item.CompanyId == companyId, cancellationToken))
            throw Validation("warehouseId", "Choose a warehouse in this company.");

        var itemIds = request.Items.Select(line => line.ItemId).ToArray();
        var activeItems = await db.Items.AsNoTracking()
            .Where(item => itemIds.Contains(item.Id) && item.IsActive)
            .Select(item => item.Id)
            .ToListAsync(cancellationToken);
        if (activeItems.Count != itemIds.Length)
            throw Validation("items", "Every counted item must be active and belong to this tenant.");
        foreach (var item in request.Items)
        {
            if (item.CountedQuantity < 0 || item.CountedQuantity > 9_999_999_999.999999m ||
                decimal.Round(item.CountedQuantity, 6) != item.CountedQuantity)
                throw Validation("items", "Counted quantities must be non-negative with at most 6 decimal places.");
            if (item.AdjustmentUnitCost is <= 0 or > MaximumAmount ||
                item.AdjustmentUnitCost.HasValue && decimal.Round(item.AdjustmentUnitCost.Value, 6) != item.AdjustmentUnitCost)
                throw Validation("items", "Adjustment unit costs, when provided, must be positive with at most 6 decimal places.");
        }

        var snapshotMovementId = await db.StockMovements
            .Where(movement => movement.CompanyId == companyId && movement.WarehouseId == request.WarehouseId)
            .Select(movement => (long?)movement.Id)
            .MaxAsync(cancellationToken) ?? 0;
        var snapshot = await db.InventoryValuationLayers.AsNoTracking()
            .Where(layer => layer.CompanyId == companyId && layer.WarehouseId == request.WarehouseId &&
                            itemIds.Contains(layer.ItemId) && layer.RemainingQuantity > 0)
            .GroupBy(layer => layer.ItemId)
            .Select(group => new
            {
                ItemId = group.Key,
                Quantity = group.Sum(layer => layer.RemainingQuantity),
                Value = group.Sum(layer => layer.RemainingQuantity * layer.UnitCost)
            })
            .ToDictionaryAsync(result => result.ItemId, cancellationToken);

        var currentLines = await db.InventoryCountLines.Where(
            line => line.CompanyId == companyId && line.InventoryCountId == countId)
            .ToListAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.InventoryCountLines.RemoveRange(currentLines);
        count.WarehouseId = request.WarehouseId;
        count.CountNumber = countNumber;
        count.CountDate = request.CountDate;
        count.SnapshotMovementId = snapshotMovementId;
        foreach (var item in request.Items)
        {
            var balance = snapshot.GetValueOrDefault(item.ItemId);
            db.InventoryCountLines.Add(new InventoryCountLine
            {
                TenantId = tenantId,
                CompanyId = companyId,
                InventoryCountId = countId,
                ItemId = item.ItemId,
                SystemQuantity = balance?.Quantity ?? 0,
                SystemValue = balance?.Value ?? 0,
                CountedQuantity = item.CountedQuantity,
                AdjustmentUnitCost = item.AdjustmentUnitCost
            });
        }
        await SaveAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetCountAsync(companyId, countId, cancellationToken);
    }

    public async Task<InventoryCountDto?> PostCountAsync(
        long companyId,
        long countId,
        PostInventoryCountRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        var company = await EnsureCompanyAsync(companyId, cancellationToken);
        var count = await db.InventoryCounts.SingleOrDefaultAsync(
            candidate => candidate.Id == countId && candidate.CompanyId == companyId,
            cancellationToken);
        if (count is null)
        {
            return null;
        }

        var countLines = await db.InventoryCountLines
            .Where(line => line.InventoryCountId == countId && line.CompanyId == companyId)
            .OrderBy(line => line.ItemId)
            .ToListAsync(cancellationToken);
        if (count.Status != "Draft")
        {
            throw new InventoryConflictException("Only a draft inventory count can be posted.");
        }

        var period = await db.AccountingPeriods.SingleOrDefaultAsync(
            candidate => candidate.Id == request.AccountingPeriodId && candidate.CompanyId == companyId,
            cancellationToken);
        if (period is null || count.CountDate < period.StartsOn || count.CountDate > period.EndsOn)
        {
            throw Validation("accountingPeriodId", "Choose an accounting period in this company that contains the count date.");
        }

        if (period.Status != "Open")
        {
            throw new InventoryConflictException("Inventory counts cannot be posted to a closed accounting period.");
        }

        if (!await db.Journals.AnyAsync(
                journal => journal.Id == request.JournalId && journal.CompanyId == companyId && journal.IsActive,
                cancellationToken))
        {
            throw Validation("journalId", "Choose an active journal in this company.");
        }

        await EnsureAccountAsync(companyId, request.InventoryAccountId, "Asset", cancellationToken);
        await EnsureAccountAsync(companyId, request.AdjustmentAccountId, "Expense", cancellationToken);
        var functionalPlaces = await db.Currencies
            .Where(currency => currency.Code == company.FunctionalCurrencyCode)
            .Select(currency => (int)currency.DecimalPlaces)
            .SingleAsync(cancellationToken);

        await using var transaction = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, cancellationToken);
        try
        {
            if (await db.StockMovements.AnyAsync(
                    movement => movement.CompanyId == companyId &&
                                movement.WarehouseId == count.WarehouseId &&
                                movement.Id > count.SnapshotMovementId,
                    cancellationToken))
            {
                throw new InventoryConflictException(
                    "Stock moved after this count was created. Create a fresh count before posting.");
            }

            var itemIds = countLines.Select(line => line.ItemId).ToArray();
            var itemMethods = await db.Items.AsNoTracking()
                .Where(item => itemIds.Contains(item.Id))
                .ToDictionaryAsync(item => item.Id, item => item.CostingMethod, cancellationToken);
            var movements = new List<StockMovement>();
            var journalLines = new List<JournalLine>();

            foreach (var line in countLines)
            {
                var layers = await LoadLayersAsync(companyId, count.WarehouseId, line.ItemId, cancellationToken);
                var currentQuantity = layers.Sum(layer => layer.RemainingQuantity);
                var currentValue = layers.Sum(layer => layer.RemainingQuantity * layer.UnitCost);
                if (currentQuantity != line.SystemQuantity || currentValue != line.SystemValue)
                {
                    throw new InventoryConflictException(
                        "The stock snapshot no longer matches current valuation. Create a fresh count before posting.");
                }

                var delta = line.CountedQuantity - line.SystemQuantity;
                if (delta == 0)
                {
                    continue;
                }

                var isReceipt = delta > 0;
                var quantity = Math.Abs(delta);
                decimal unitCost;
                decimal totalCost;
                if (isReceipt)
                {
                    if (line.AdjustmentUnitCost.HasValue)
                    {
                        unitCost = line.AdjustmentUnitCost.Value;
                    }
                    else if (line.SystemQuantity > 0)
                    {
                        unitCost = decimal.Round(
                            line.SystemValue / line.SystemQuantity, 6, MidpointRounding.AwayFromZero);
                    }
                    else
                    {
                        throw Validation(
                            "items",
                            $"Provide adjustmentUnitCost for item {line.ItemId} because it has no existing stock value.");
                    }

                    totalCost = decimal.Round(quantity * unitCost, functionalPlaces, MidpointRounding.AwayFromZero);
                    if (itemMethods[line.ItemId] == "AVCO" && layers.Count > 0)
                    {
                        var averageCost = decimal.Round(
                            (currentValue + totalCost) / (currentQuantity + quantity),
                            6, MidpointRounding.AwayFromZero);
                        foreach (var layer in layers)
                        {
                            layer.UnitCost = averageCost;
                            layer.Revision++;
                        }

                        unitCost = averageCost;
                        totalCost = decimal.Round(quantity * unitCost, functionalPlaces, MidpointRounding.AwayFromZero);
                    }
                }
                else
                {
                    if (quantity > currentQuantity)
                    {
                        throw Validation("items", $"Counted quantity for item {line.ItemId} cannot be negative.");
                    }

                    if (itemMethods[line.ItemId] == "AVCO")
                    {
                        unitCost = decimal.Round(currentValue / currentQuantity, 6, MidpointRounding.AwayFromZero);
                        foreach (var layer in layers)
                        {
                            layer.UnitCost = unitCost;
                            layer.Revision++;
                        }
                    }

                    var remaining = quantity;
                    var issueValue = 0m;
                    foreach (var layer in layers)
                    {
                        if (remaining == 0)
                        {
                            break;
                        }

                        var take = Math.Min(remaining, layer.RemainingQuantity);
                        layer.RemainingQuantity -= take;
                        layer.Revision++;
                        issueValue += take * layer.UnitCost;
                        remaining -= take;
                    }

                    totalCost = decimal.Round(issueValue, functionalPlaces, MidpointRounding.AwayFromZero);
                    unitCost = decimal.Round(totalCost / quantity, 6, MidpointRounding.AwayFromZero);
                }

                if (totalCost <= 0 || totalCost > MaximumAmount)
                {
                    throw Validation("items", $"Count adjustment value for item {line.ItemId} is outside the supported range.");
                }

                var movement = new StockMovement
                {
                    TenantId = tenantId,
                    CompanyId = companyId,
                    ItemId = line.ItemId,
                    WarehouseId = count.WarehouseId,
                    MovementNumber = $"C{count.Id}-{line.ItemId}",
                    MovementType = "CountAdjustment",
                    MovementDate = count.CountDate,
                    QuantityIn = isReceipt ? quantity : 0,
                    QuantityOut = isReceipt ? 0 : quantity,
                    UnitCost = unitCost,
                    TotalCost = totalCost
                };
                movements.Add(movement);
                journalLines.AddRange(
                [
                    new JournalLine
                    {
                        TenantId = tenantId,
                        CompanyId = companyId,
                        AccountId = isReceipt ? request.InventoryAccountId : request.AdjustmentAccountId,
                        TransactionCurrencyCode = company.FunctionalCurrencyCode,
                        Debit = isReceipt ? totalCost : 0,
                        Credit = isReceipt ? 0 : totalCost,
                        FunctionalDebit = isReceipt ? totalCost : 0,
                        FunctionalCredit = isReceipt ? 0 : totalCost,
                        ExchangeRate = 1,
                        Description = $"Inventory count {count.CountNumber}"
                    },
                    new JournalLine
                    {
                        TenantId = tenantId,
                        CompanyId = companyId,
                        AccountId = isReceipt ? request.AdjustmentAccountId : request.InventoryAccountId,
                        TransactionCurrencyCode = company.FunctionalCurrencyCode,
                        Debit = isReceipt ? 0 : totalCost,
                        Credit = isReceipt ? totalCost : 0,
                        FunctionalDebit = isReceipt ? 0 : totalCost,
                        FunctionalCredit = isReceipt ? totalCost : 0,
                        ExchangeRate = 1,
                        Description = $"Inventory count {count.CountNumber}"
                    }
                ]);
            }

            JournalEntry? journalEntry = null;
            if (journalLines.Count > 0)
            {
                var total = journalLines.Sum(line => line.Debit);
                if (total <= 0 || total > MaximumAmount)
                {
                    throw Validation("items", "Total count adjustment exceeds the supported journal-entry amount.");
                }

                journalEntry = new JournalEntry
                {
                    TenantId = tenantId,
                    CompanyId = companyId,
                    JournalId = request.JournalId,
                    AccountingPeriodId = request.AccountingPeriodId,
                    EntryDate = count.CountDate,
                    Description = $"Inventory count {count.CountNumber}",
                    Status = "Posted",
                    TotalDebit = total,
                    TotalCredit = total,
                    Lines = journalLines
                };
                db.JournalEntries.Add(journalEntry);
            }

            count.Status = "Posted";
            count.Revision++;
            await db.SaveChangesAsync(cancellationToken);

            if (journalEntry is not null)
            {
                count.JournalEntryId = journalEntry.Id;
                foreach (var movement in movements)
                {
                    movement.JournalEntryId = journalEntry.Id;
                }
            }

            db.StockMovements.AddRange(movements);
            await db.SaveChangesAsync(cancellationToken);

            foreach (var movement in movements.Where(movement => movement.QuantityIn > 0))
            {
                db.InventoryValuationLayers.Add(new InventoryValuationLayer
                {
                    TenantId = tenantId,
                    CompanyId = companyId,
                    StockMovementId = movement.Id,
                    ItemId = movement.ItemId,
                    WarehouseId = movement.WarehouseId,
                    RemainingQuantity = movement.QuantityIn,
                    UnitCost = movement.UnitCost
                });
            }

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return ToCountDto(count, countLines);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InventoryConflictException("Stock or count changed concurrently. Reload the count and retry.");
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is Npgsql.PostgresException
            {
                SqlState: PostgresErrorCodes.SerializationFailure
            })
        {
            throw new InventoryConflictException("Stock changed concurrently. Create a fresh count and retry.");
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            throw new InventoryConflictException("A count adjustment movement number already exists.");
        }
        catch (Npgsql.PostgresException exception) when (exception.SqlState == PostgresErrorCodes.SerializationFailure)
        {
            throw new InventoryConflictException("Stock changed concurrently. Create a fresh count and retry.");
        }
    }
}
