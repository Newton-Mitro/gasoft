using Gasoft.Application.Abstractions;
using Gasoft.Application.Inventory;
using Gasoft.Application.Common;
using Gasoft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Gasoft.Infrastructure.Persistence;

public sealed class InventoryService(AppDbContext db, ICurrentTenant currentTenant) : IInventoryService
{
    private const decimal MaximumAmount = 99_999_999_999_999.999999m;

    public async Task<IReadOnlyList<UnitOfMeasureDto>> GetUnitsOfMeasureAsync(CancellationToken cancellationToken) =>
        await db.UnitsOfMeasure.AsNoTracking()
            .OrderBy(unit => unit.Code)
            .Select(unit => new UnitOfMeasureDto(unit.Code, unit.Name, unit.DecimalPlaces))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ItemCategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken)
    {
        EnsureTenant();
        return await db.ItemCategories.AsNoTracking()
            .OrderBy(category => category.Code)
            .Select(category => new ItemCategoryDto(category.Id, category.Code, category.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResultDto<ItemCategoryDto>> GetCategoriesPageAsync(
        int page, int pageSize, CancellationToken cancellationToken)
    {
        EnsureTenant();
        return await PagedQuery.ToPageAsync(
            db.ItemCategories.AsNoTracking().OrderBy(category => category.Code).ThenBy(category => category.Id),
            page,
            pageSize,
            category => new ItemCategoryDto(category.Id, category.Code, category.Name),
            cancellationToken);
    }

    public async Task<ItemCategoryDto?> GetCategoryAsync(long categoryId, CancellationToken cancellationToken)
    {
        EnsureTenant();
        var category = await db.ItemCategories.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == categoryId, cancellationToken);
        return category is null ? null : new ItemCategoryDto(category.Id, category.Code, category.Name);
    }

    public async Task<ItemCategoryDto> CreateCategoryAsync(
        CreateItemCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        var code = Normalize(request.Code).ToUpperInvariant();
        var name = Normalize(request.Name);
        ValidateCodeAndName(code, name, 30, 120);
        var category = new ItemCategory { TenantId = tenantId, Code = code, Name = name };
        db.ItemCategories.Add(category);
        await SaveAsync(cancellationToken);
        return new ItemCategoryDto(category.Id, category.Code, category.Name);
    }

    public async Task<ItemCategoryDto?> UpdateCategoryAsync(
        long categoryId,
        UpdateItemCategoryRequest request,
        CancellationToken cancellationToken)
    {
        EnsureTenant();
        var category = await db.ItemCategories.SingleOrDefaultAsync(item => item.Id == categoryId, cancellationToken);
        if (category is null) return null;
        var code = Normalize(request.Code).ToUpperInvariant();
        var name = Normalize(request.Name);
        ValidateCodeAndName(code, name, 30, 120);
        category.Code = code;
        category.Name = name;
        await SaveAsync(cancellationToken);
        return new ItemCategoryDto(category.Id, category.Code, category.Name);
    }

    public async Task<IReadOnlyList<ItemDto>> GetItemsAsync(CancellationToken cancellationToken)
    {
        EnsureTenant();
        return await db.Items.AsNoTracking()
            .OrderBy(item => item.Sku)
            .Select(item => new ItemDto(item.Id, item.CategoryId, item.Sku, item.Name, item.BaseUnitCode, item.CostingMethod))
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResultDto<ItemDto>> GetItemsPageAsync(
        int page, int pageSize, CancellationToken cancellationToken)
    {
        EnsureTenant();
        return await PagedQuery.ToPageAsync(
            db.Items.AsNoTracking().OrderBy(item => item.Sku).ThenBy(item => item.Id),
            page,
            pageSize,
            item => new ItemDto(item.Id, item.CategoryId, item.Sku, item.Name, item.BaseUnitCode, item.CostingMethod),
            cancellationToken);
    }

    public async Task<ItemDto?> GetItemAsync(long itemId, CancellationToken cancellationToken)
    {
        EnsureTenant();
        var item = await db.Items.AsNoTracking().SingleOrDefaultAsync(value => value.Id == itemId, cancellationToken);
        return item is null ? null : ToDto(item);
    }

    public async Task<ItemDto> CreateItemAsync(CreateItemRequest request, CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        var sku = Normalize(request.Sku).ToUpperInvariant();
        var name = Normalize(request.Name);
        var unitCode = Normalize(request.BaseUnitCode).ToUpperInvariant();
        var costingMethod = Normalize(request.CostingMethod).ToUpperInvariant();
        ValidateCodeAndName(sku, name, 60, 200);
        if (costingMethod is not ("FIFO" or "AVCO"))
        {
            throw Validation("costingMethod", "Costing method must be FIFO or AVCO.");
        }

        if (!await db.ItemCategories.AnyAsync(category => category.Id == request.CategoryId, cancellationToken))
        {
            throw Validation("categoryId", "Choose an item category in this tenant.");
        }

        if (!await db.UnitsOfMeasure.AnyAsync(unit => unit.Code == unitCode, cancellationToken))
        {
            throw Validation("baseUnitCode", "Choose a supported unit of measure.");
        }

        var item = new Item
        {
            TenantId = tenantId,
            CategoryId = request.CategoryId,
            Sku = sku,
            Name = name,
            BaseUnitCode = unitCode,
            CostingMethod = costingMethod
        };
        db.Items.Add(item);
        await SaveAsync(cancellationToken);
        return ToDto(item);
    }

    public async Task<ItemDto?> UpdateItemAsync(
        long itemId,
        UpdateItemRequest request,
        CancellationToken cancellationToken)
    {
        EnsureTenant();
        var item = await db.Items.SingleOrDefaultAsync(value => value.Id == itemId, cancellationToken);
        if (item is null) return null;

        var sku = Normalize(request.Sku).ToUpperInvariant();
        var name = Normalize(request.Name);
        var unitCode = Normalize(request.BaseUnitCode).ToUpperInvariant();
        var costingMethod = Normalize(request.CostingMethod).ToUpperInvariant();
        ValidateCodeAndName(sku, name, 60, 200);
        if (costingMethod is not ("FIFO" or "AVCO"))
            throw Validation("costingMethod", "Costing method must be FIFO or AVCO.");
        if (!await db.ItemCategories.AnyAsync(category => category.Id == request.CategoryId, cancellationToken))
            throw Validation("categoryId", "Choose an item category in this tenant.");
        if (!await db.UnitsOfMeasure.AnyAsync(unit => unit.Code == unitCode, cancellationToken))
            throw Validation("baseUnitCode", "Choose a supported unit of measure.");

        if ((item.BaseUnitCode != unitCode || item.CostingMethod != costingMethod) &&
            await db.StockMovements.AnyAsync(movement => movement.ItemId == itemId, cancellationToken))
            throw new InventoryConflictException("The unit and costing method cannot change after stock movements exist.");

        item.CategoryId = request.CategoryId;
        item.Sku = sku;
        item.Name = name;
        item.BaseUnitCode = unitCode;
        item.CostingMethod = costingMethod;
        await SaveAsync(cancellationToken);
        return ToDto(item);
    }

    public async Task<IReadOnlyList<WarehouseDto>> GetWarehousesAsync(
        long companyId,
        CancellationToken cancellationToken)
    {
        await EnsureCompanyAsync(companyId, cancellationToken);
        return await db.Warehouses.AsNoTracking()
            .Where(warehouse => warehouse.CompanyId == companyId)
            .OrderBy(warehouse => warehouse.Code)
            .Select(warehouse => new WarehouseDto(
                warehouse.Id, warehouse.CompanyId, warehouse.Code, warehouse.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResultDto<WarehouseDto>> GetWarehousesPageAsync(
        long companyId, int page, int pageSize, CancellationToken cancellationToken)
    {
        await EnsureCompanyAsync(companyId, cancellationToken);
        return await PagedQuery.ToPageAsync(
            db.Warehouses.AsNoTracking()
                .Where(warehouse => warehouse.CompanyId == companyId)
                .OrderBy(warehouse => warehouse.Code)
                .ThenBy(warehouse => warehouse.Id),
            page,
            pageSize,
            warehouse => new WarehouseDto(
                warehouse.Id, warehouse.CompanyId, warehouse.Code, warehouse.Name),
            cancellationToken);
    }

    public async Task<WarehouseDto> CreateWarehouseAsync(
        long companyId,
        CreateWarehouseRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        await EnsureCompanyAsync(companyId, cancellationToken);
        var code = Normalize(request.Code).ToUpperInvariant();
        var name = Normalize(request.Name);
        ValidateCodeAndName(code, name, 30, 120);
        var warehouse = new Warehouse
        {
            TenantId = tenantId,
            CompanyId = companyId,
            Code = code,
            Name = name
        };
        db.Warehouses.Add(warehouse);
        await SaveAsync(cancellationToken);
        return new WarehouseDto(warehouse.Id, companyId, warehouse.Code, warehouse.Name);
    }

    public async Task<WarehouseDto?> GetWarehouseAsync(
        long companyId,
        long warehouseId,
        CancellationToken cancellationToken)
    {
        await EnsureCompanyAsync(companyId, cancellationToken);
        var warehouse = await db.Warehouses.AsNoTracking()
            .SingleOrDefaultAsync(item => item.CompanyId == companyId && item.Id == warehouseId, cancellationToken);
        return warehouse is null ? null : new WarehouseDto(
            warehouse.Id, warehouse.CompanyId, warehouse.Code, warehouse.Name);
    }

    public async Task<WarehouseDto?> UpdateWarehouseAsync(
        long companyId,
        long warehouseId,
        UpdateWarehouseRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureCompanyAsync(companyId, cancellationToken);
        var warehouse = await db.Warehouses.SingleOrDefaultAsync(
            item => item.CompanyId == companyId && item.Id == warehouseId, cancellationToken);
        if (warehouse is null) return null;
        var code = Normalize(request.Code).ToUpperInvariant();
        var name = Normalize(request.Name);
        ValidateCodeAndName(code, name, 30, 120);
        warehouse.Code = code;
        warehouse.Name = name;
        await SaveAsync(cancellationToken);
        return new WarehouseDto(warehouse.Id, companyId, warehouse.Code, warehouse.Name);
    }

    public async Task<WarehouseLocationDto> CreateLocationAsync(
        long companyId,
        long warehouseId,
        CreateWarehouseLocationRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        if (!await db.Warehouses.AnyAsync(
                warehouse => warehouse.Id == warehouseId && warehouse.CompanyId == companyId,
                cancellationToken))
        {
            throw new KeyNotFoundException("Warehouse was not found in this company.");
        }

        var code = Normalize(request.Code).ToUpperInvariant();
        var name = Normalize(request.Name);
        ValidateCodeAndName(code, name, 30, 120);
        var location = new WarehouseLocation
        {
            TenantId = tenantId,
            CompanyId = companyId,
            WarehouseId = warehouseId,
            Code = code,
            Name = name
        };
        db.WarehouseLocations.Add(location);
        await SaveAsync(cancellationToken);
        return new WarehouseLocationDto(location.Id, warehouseId, location.Code, location.Name);
    }

    public async Task<WarehouseLocationDto?> GetLocationAsync(
        long companyId,
        long warehouseId,
        long locationId,
        CancellationToken cancellationToken)
    {
        await EnsureCompanyAsync(companyId, cancellationToken);
        return await db.WarehouseLocations.AsNoTracking()
            .Where(location =>
                location.CompanyId == companyId &&
                location.WarehouseId == warehouseId &&
                location.Id == locationId)
            .Select(location => new WarehouseLocationDto(
                location.Id, location.WarehouseId, location.Code, location.Name))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<WarehouseLocationDto?> UpdateLocationAsync(
        long companyId,
        long warehouseId,
        long locationId,
        UpdateWarehouseLocationRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureCompanyAsync(companyId, cancellationToken);
        var location = await db.WarehouseLocations.SingleOrDefaultAsync(
            item => item.CompanyId == companyId &&
                item.WarehouseId == warehouseId &&
                item.Id == locationId,
            cancellationToken);
        if (location is null) return null;

        var code = Normalize(request.Code).ToUpperInvariant();
        var name = Normalize(request.Name);
        ValidateCodeAndName(code, name, 30, 120);
        location.Code = code;
        location.Name = name;
        await SaveAsync(cancellationToken);
        return new WarehouseLocationDto(location.Id, warehouseId, location.Code, location.Name);
    }

    public async Task<IReadOnlyList<WarehouseLocationDto>> GetLocationsAsync(
        long companyId,
        long warehouseId,
        CancellationToken cancellationToken)
    {
        await EnsureCompanyAsync(companyId, cancellationToken);
        if (!await db.Warehouses.AnyAsync(
                warehouse => warehouse.Id == warehouseId && warehouse.CompanyId == companyId,
                cancellationToken))
        {
            throw new KeyNotFoundException("Warehouse was not found in this company.");
        }

        return await db.WarehouseLocations.AsNoTracking()
            .Where(location => location.CompanyId == companyId && location.WarehouseId == warehouseId)
            .OrderBy(location => location.Code)
            .ThenBy(location => location.Id)
            .Select(location => new WarehouseLocationDto(
                location.Id, location.WarehouseId, location.Code, location.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StockBalanceDto>> GetStockBalancesAsync(
        long companyId,
        CancellationToken cancellationToken)
    {
        await EnsureCompanyAsync(companyId, cancellationToken);
        var balances = await db.InventoryValuationLayers.AsNoTracking()
            .Where(layer => layer.CompanyId == companyId && layer.RemainingQuantity > 0)
            .GroupBy(layer => new { layer.ItemId, layer.WarehouseId })
            .Select(group => new
            {
                group.Key.ItemId,
                group.Key.WarehouseId,
                Quantity = group.Sum(layer => layer.RemainingQuantity),
                Value = group.Sum(layer => layer.RemainingQuantity * layer.UnitCost),
            })
            .ToListAsync(cancellationToken);

        return balances
            .OrderBy(balance => balance.ItemId)
            .ThenBy(balance => balance.WarehouseId)
            .Select(balance => new StockBalanceDto(
                balance.ItemId,
                balance.WarehouseId,
                balance.Quantity,
                balance.Value))
            .ToArray();
    }

    public async Task<PagedResultDto<StockBalanceDto>> GetStockBalancesPageAsync(
        long companyId, int page, int pageSize, CancellationToken cancellationToken)
    {
        await EnsureCompanyAsync(companyId, cancellationToken);
        if (page < 1 || pageSize is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(page), "Page must be positive and page size must be between 1 and 100.");
        }

        var balanceLayers = db.InventoryValuationLayers.AsNoTracking()
            .Where(layer => layer.CompanyId == companyId && layer.RemainingQuantity > 0);
        var balanceGroups = balanceLayers
            .GroupBy(layer => new { layer.ItemId, layer.WarehouseId });
        var balances = balanceGroups
            .Select(group => new
            {
                group.Key.ItemId,
                group.Key.WarehouseId,
                Quantity = group.Sum(layer => layer.RemainingQuantity),
                Value = group.Sum(layer => layer.RemainingQuantity * layer.UnitCost),
            })
            .OrderBy(balance => balance.ItemId)
            .ThenBy(balance => balance.WarehouseId);
        var countQuery = balanceLayers
            .Select(layer => new { layer.ItemId, layer.WarehouseId })
            .Distinct();
        var totalCount = await countQuery.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var normalizedPage = totalPages == 0 ? 1 : Math.Min(page, totalPages);
        var rows = await balances
            .Skip((normalizedPage - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new PagedResultDto<StockBalanceDto>(
            rows.Select(balance => new StockBalanceDto(
                balance.ItemId, balance.WarehouseId, balance.Quantity, balance.Value)).ToArray(),
            normalizedPage,
            pageSize,
            totalCount,
            totalPages);
    }

    public async Task<IReadOnlyList<StockMovementDto>> GetStockMovementsAsync(
        long companyId,
        CancellationToken cancellationToken)
    {
        await EnsureCompanyAsync(companyId, cancellationToken);
        var movements = await db.StockMovements.AsNoTracking()
            .Where(movement => movement.CompanyId == companyId)
            .OrderByDescending(movement => movement.MovementDate)
            .ThenByDescending(movement => movement.Id)
            .ToListAsync(cancellationToken);
        return movements.Select(ToDto).ToArray();
    }

    public async Task<PagedResultDto<StockMovementDto>> GetStockMovementsPageAsync(
        long companyId, int page, int pageSize, CancellationToken cancellationToken)
    {
        await EnsureCompanyAsync(companyId, cancellationToken);
        var result = await PagedQuery.ToPageAsync(
            db.StockMovements.AsNoTracking()
                .Where(movement => movement.CompanyId == companyId)
                .OrderByDescending(movement => movement.MovementDate)
                .ThenByDescending(movement => movement.Id),
            page,
            pageSize,
            movement => movement,
            cancellationToken);
        return new PagedResultDto<StockMovementDto>(
            result.Items.Select(ToDto).ToArray(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages);
    }

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

    private async Task<Company> EnsureCompanyAsync(long companyId, CancellationToken cancellationToken)
    {
        EnsureTenant();
        return await db.Companies.SingleOrDefaultAsync(
                   company => company.Id == companyId,
                   cancellationToken)
               ?? throw new KeyNotFoundException("Company was not found in this tenant.");
    }

    private async Task<List<InventoryValuationLayer>> LoadLayersAsync(
        long companyId,
        long warehouseId,
        long itemId,
        CancellationToken cancellationToken) =>
        await (
            from layer in db.InventoryValuationLayers
            join stockMovement in db.StockMovements on new
            {
                layer.TenantId,
                layer.CompanyId,
                StockMovementId = layer.StockMovementId
            } equals new
            {
                stockMovement.TenantId,
                stockMovement.CompanyId,
                StockMovementId = stockMovement.Id
            }
            where layer.CompanyId == companyId &&
                  layer.WarehouseId == warehouseId &&
                  layer.ItemId == itemId &&
                  layer.RemainingQuantity > 0
            orderby stockMovement.MovementDate, layer.Id
            select layer)
            .ToListAsync(cancellationToken);

    private async Task EnsureAccountAsync(
        long companyId,
        long accountId,
        string accountType,
        CancellationToken cancellationToken)
    {
        var account = await (
            from chartAccount in db.ChartOfAccounts
            join accountGroup in db.AccountGroups on chartAccount.AccountGroupId equals accountGroup.Id
            where chartAccount.Id == accountId && chartAccount.CompanyId == companyId
            select new { chartAccount.AllowPosting, chartAccount.IsActive, accountGroup.AccountType })
            .SingleOrDefaultAsync(cancellationToken);
        if (account is null || !account.AllowPosting || !account.IsActive ||
            account.AccountType != accountType)
        {
            throw Validation("accountId", $"Choose an active, postable {accountType.ToLowerInvariant()} account in this company.");
        }
    }

    private async Task EnsureOneOfAccountTypesAsync(
        long companyId,
        long accountId,
        string[] allowedTypes,
        CancellationToken cancellationToken)
    {
        var account = await (
            from chartAccount in db.ChartOfAccounts
            join accountGroup in db.AccountGroups on chartAccount.AccountGroupId equals accountGroup.Id
            where chartAccount.Id == accountId && chartAccount.CompanyId == companyId
            select new { chartAccount.AllowPosting, chartAccount.IsActive, accountGroup.AccountType })
            .SingleOrDefaultAsync(cancellationToken);
        if (account is null || !account.AllowPosting || !account.IsActive ||
            !allowedTypes.Contains(account.AccountType, StringComparer.Ordinal))
        {
            throw Validation("offsetAccountId", "Choose an active, postable asset or liability account in this company.");
        }
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            throw new InventoryConflictException("A record with the same code already exists.");
        }
    }

    private long EnsureTenant() =>
        currentTenant.TenantId ?? throw new UnauthorizedAccessException("A tenant-scoped access token is required.");

    private static void ValidateCodeAndName(string code, string name, int codeLength, int nameLength)
    {
        if (code.Length is 0 || code.Length > codeLength ||
            name.Length is 0 || name.Length > nameLength)
        {
            throw Validation("record", $"Code and name are required; maximum lengths are {codeLength} and {nameLength}.");
        }
    }

    private static InventoryValidationException Validation(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });

    private static string Normalize(string? value) => value?.Trim() ?? string.Empty;

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    private static ItemDto ToDto(Item item) =>
        new(item.Id, item.CategoryId, item.Sku, item.Name, item.BaseUnitCode, item.CostingMethod);

    private static InventoryCountDto ToCountDto(
        InventoryCount count,
        IEnumerable<InventoryCountLine> lines) =>
        new(
            count.Id,
            count.WarehouseId,
            count.CountNumber,
            count.CountDate,
            count.Status,
            count.JournalEntryId,
            lines.OrderBy(line => line.ItemId)
                .Select(line => new InventoryCountLineDto(
                    line.ItemId,
                    line.SystemQuantity,
                    line.SystemValue,
                    line.CountedQuantity,
                    line.AdjustmentUnitCost))
                .ToArray());

    private static StockMovementDto ToDto(StockMovement movement) =>
        new(movement.Id, movement.ItemId, movement.WarehouseId, movement.WarehouseLocationId,
            movement.JournalEntryId, movement.MovementNumber, movement.MovementType, movement.MovementDate,
            movement.QuantityIn, movement.QuantityOut, movement.UnitCost, movement.TotalCost);
}
