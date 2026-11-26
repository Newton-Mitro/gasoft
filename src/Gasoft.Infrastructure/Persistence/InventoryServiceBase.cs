using Gasoft.Application.Abstractions;
using Gasoft.Application.Inventory;
using Gasoft.Application.Common;
using Gasoft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Gasoft.Infrastructure.Persistence;

public abstract class InventoryServiceBase
{
    protected readonly AppDbContext db;
    protected readonly ICurrentTenant currentTenant;

    protected InventoryServiceBase(AppDbContext db, ICurrentTenant currentTenant)
    {
        this.db = db;
        this.currentTenant = currentTenant;
    }

    protected const decimal MaximumAmount = 99_999_999_999_999.999999m;

    protected IQueryable<ItemDto> CompanyItemsQuery(
        long companyId,
        string functionalCurrencyCode,
        long? itemId = null)
    {
        var items = db.Items.AsNoTracking();
        if (itemId.HasValue)
            items = items.Where(item => item.Id == itemId.Value);

        return
            from item in items
            join price in db.CompanyItemPrices.Where(value => value.CompanyId == companyId)
                on new { item.TenantId, ItemId = item.Id }
                equals new { price.TenantId, price.ItemId } into prices
            from price in prices.DefaultIfEmpty()
            orderby item.Sku, item.Id
            select new ItemDto(
                item.Id,
                item.CategoryId,
                item.Sku,
                item.Name,
                item.BaseUnitCode,
                item.CostingMethod,
                price == null || price.CurrencyCode != functionalCurrencyCode
                    ? null
                    : price.UnitSellingPrice,
                functionalCurrencyCode);
    }

    protected async Task<ValidatedItemFields> ValidateItemFieldsAsync(
        long categoryId,
        string skuValue,
        string nameValue,
        string baseUnitCodeValue,
        string costingMethodValue,
        decimal? unitSellingPrice,
        CancellationToken cancellationToken)
    {
        var sku = Normalize(skuValue).ToUpperInvariant();
        var name = Normalize(nameValue);
        var unitCode = Normalize(baseUnitCodeValue).ToUpperInvariant();
        var costingMethod = Normalize(costingMethodValue).ToUpperInvariant();
        ValidateCodeAndName(sku, name, 60, 200);
        if (costingMethod is not ("FIFO" or "AVCO"))
            throw Validation("costingMethod", "Costing method must be FIFO or AVCO.");
        if (unitSellingPrice is < 0 or > MaximumAmount ||
            unitSellingPrice.HasValue && decimal.Round(unitSellingPrice.Value, 6) != unitSellingPrice)
            throw Validation("unitSellingPrice", "Enter a valid selling price with up to 6 decimal places.");
        if (!await db.ItemCategories.AnyAsync(category => category.Id == categoryId, cancellationToken))
            throw Validation("categoryId", "Choose an item category in this tenant.");
        if (!await db.UnitsOfMeasure.AnyAsync(unit => unit.Code == unitCode, cancellationToken))
            throw Validation("baseUnitCode", "Choose a supported unit of measure.");
        return new ValidatedItemFields(sku, name, unitCode, costingMethod);
    }

    protected static ItemDto ToCompanyDto(Item item, CompanyItemPrice? price, string defaultCurrencyCode) =>
        new(
            item.Id,
            item.CategoryId,
            item.Sku,
            item.Name,
            item.BaseUnitCode,
            item.CostingMethod,
            price is not null && price.CurrencyCode == defaultCurrencyCode ? price.UnitSellingPrice : null,
            defaultCurrencyCode);

    protected async Task<Company> EnsureCompanyAsync(long companyId, CancellationToken cancellationToken)
    {
        EnsureTenant();
        return await db.Companies.SingleOrDefaultAsync(
                   company => company.Id == companyId,
                   cancellationToken)
               ?? throw new KeyNotFoundException("Company was not found in this tenant.");
    }

    protected async Task<List<InventoryValuationLayer>> LoadLayersAsync(
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

    protected async Task EnsureAccountAsync(
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

    protected async Task EnsureOneOfAccountTypesAsync(
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

    protected async Task SaveAsync(CancellationToken cancellationToken)
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

    protected long EnsureTenant() =>
        currentTenant.TenantId ?? throw new UnauthorizedAccessException("A tenant-scoped access token is required.");

    protected static void ValidateCodeAndName(string code, string name, int codeLength, int nameLength)
    {
        if (code.Length is 0 || code.Length > codeLength ||
            name.Length is 0 || name.Length > nameLength)
        {
            throw Validation("record", $"Code and name are required; maximum lengths are {codeLength} and {nameLength}.");
        }
    }

    protected static InventoryValidationException Validation(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });

    protected static string Normalize(string? value) => value?.Trim() ?? string.Empty;

    protected static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    protected static ItemDto ToDto(Item item) =>
        new(item.Id, item.CategoryId, item.Sku, item.Name, item.BaseUnitCode, item.CostingMethod, null, null);

    protected sealed record ValidatedItemFields(string Sku, string Name, string UnitCode, string CostingMethod);

    protected static InventoryCountDto ToCountDto(
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

    protected static StockMovementDto ToDto(StockMovement movement) =>
        new(movement.Id, movement.ItemId, movement.WarehouseId, movement.WarehouseLocationId,
            movement.JournalEntryId, movement.MovementNumber, movement.MovementType, movement.MovementDate,
            movement.QuantityIn, movement.QuantityOut, movement.UnitCost, movement.TotalCost);
}
