using Gasoft.Application.Abstractions;
using Gasoft.Application.Inventory;
using Gasoft.Application.Common;
using Gasoft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Gasoft.Infrastructure.Persistence;

public sealed partial class InventoryService
{
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
            .OrderBy(balance => balance.ItemId)
            .ThenBy(balance => balance.WarehouseId)
            .ToListAsync(cancellationToken);
        var names = await GetBalanceNamesAsync(
            balances.Select(balance => balance.ItemId),
            balances.Select(balance => balance.WarehouseId),
            cancellationToken);

        return balances.Select(balance => new StockBalanceDto(
                balance.ItemId,
                names.ItemNames[balance.ItemId],
                balance.WarehouseId,
                names.WarehouseNames[balance.WarehouseId],
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
            });
        var countQuery = balanceLayers
            .Select(layer => new { layer.ItemId, layer.WarehouseId })
            .Distinct();
        var totalCount = await countQuery.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var normalizedPage = totalPages == 0 ? 1 : Math.Min(page, totalPages);
        var rows = await balances
            .OrderBy(balance => balance.ItemId)
            .ThenBy(balance => balance.WarehouseId)
            .Skip((normalizedPage - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        var names = await GetBalanceNamesAsync(
            rows.Select(balance => balance.ItemId),
            rows.Select(balance => balance.WarehouseId),
            cancellationToken);
        return new PagedResultDto<StockBalanceDto>(
            rows.Select(balance => new StockBalanceDto(
                balance.ItemId,
                names.ItemNames[balance.ItemId],
                balance.WarehouseId,
                names.WarehouseNames[balance.WarehouseId],
                balance.Quantity,
                balance.Value)).ToArray(),
            normalizedPage,
            pageSize,
            totalCount,
            totalPages);
    }

    private async Task<(Dictionary<long, string> ItemNames, Dictionary<long, string> WarehouseNames)> GetBalanceNamesAsync(
        IEnumerable<long> itemIds,
        IEnumerable<long> warehouseIds,
        CancellationToken cancellationToken)
    {
        var requestedItemIds = itemIds.Distinct().ToArray();
        var requestedWarehouseIds = warehouseIds.Distinct().ToArray();
        var itemNames = await db.Items.AsNoTracking()
            .Where(item => requestedItemIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.Name, cancellationToken);
        var warehouseNames = await db.Warehouses.AsNoTracking()
            .Where(warehouse => requestedWarehouseIds.Contains(warehouse.Id))
            .ToDictionaryAsync(warehouse => warehouse.Id, warehouse => warehouse.Name, cancellationToken);
        return (itemNames, warehouseNames);
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
}
