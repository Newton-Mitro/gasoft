using Gasoft.Application.Abstractions;
using Gasoft.Application.Inventory;
using Gasoft.Application.Common;
using Gasoft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Gasoft.Infrastructure.Persistence;

public sealed partial class InventoryService
{
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
            .Select(item => new ItemDto(
                item.Id, item.CategoryId, item.Sku, item.Name, item.BaseUnitCode, item.CostingMethod, null, null))
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
            item => new ItemDto(
                item.Id, item.CategoryId, item.Sku, item.Name, item.BaseUnitCode, item.CostingMethod, null, null),
            cancellationToken);
    }

    public async Task<ItemDto?> GetItemAsync(long itemId, CancellationToken cancellationToken)
    {
        EnsureTenant();
        var item = await db.Items.AsNoTracking().SingleOrDefaultAsync(value => value.Id == itemId, cancellationToken);
        return item is null ? null : ToDto(item);
    }

    public async Task<IReadOnlyList<ItemDto>> GetCompanyItemsAsync(
        long companyId,
        CancellationToken cancellationToken)
    {
        var company = await EnsureCompanyAsync(companyId, cancellationToken);
        return await CompanyItemsQuery(companyId, company.FunctionalCurrencyCode)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResultDto<ItemDto>> GetCompanyItemsPageAsync(
        long companyId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var company = await EnsureCompanyAsync(companyId, cancellationToken);
        return await PagedQuery.ToPageAsync(
            CompanyItemsQuery(companyId, company.FunctionalCurrencyCode),
            page,
            pageSize,
            item => item,
            cancellationToken);
    }

    public async Task<ItemDto?> GetCompanyItemAsync(
        long companyId,
        long itemId,
        CancellationToken cancellationToken)
    {
        var company = await EnsureCompanyAsync(companyId, cancellationToken);
        return await CompanyItemsQuery(companyId, company.FunctionalCurrencyCode, itemId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<ItemDto> CreateCompanyItemAsync(
        long companyId,
        CreateItemRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        var company = await EnsureCompanyAsync(companyId, cancellationToken);
        var fields = await ValidateItemFieldsAsync(
            request.CategoryId,
            request.Sku,
            request.Name,
            request.BaseUnitCode,
            request.CostingMethod,
            request.UnitSellingPrice,
            cancellationToken);
        var item = new Item
        {
            TenantId = tenantId,
            CategoryId = request.CategoryId,
            Sku = fields.Sku,
            Name = fields.Name,
            BaseUnitCode = fields.UnitCode,
            CostingMethod = fields.CostingMethod
        };
        var price = new CompanyItemPrice
        {
            TenantId = tenantId,
            CompanyId = companyId,
            Item = item,
            UnitSellingPrice = request.UnitSellingPrice,
            CurrencyCode = company.FunctionalCurrencyCode
        };
        db.Items.Add(item);
        db.CompanyItemPrices.Add(price);
        await SaveAsync(cancellationToken);
        return ToCompanyDto(item, price, company.FunctionalCurrencyCode);
    }

    public async Task<ItemDto?> UpdateCompanyItemAsync(
        long companyId,
        long itemId,
        UpdateItemRequest request,
        CancellationToken cancellationToken)
    {
        var company = await EnsureCompanyAsync(companyId, cancellationToken);
        var item = await db.Items.SingleOrDefaultAsync(value => value.Id == itemId, cancellationToken);
        if (item is null) return null;

        var fields = await ValidateItemFieldsAsync(
            request.CategoryId,
            request.Sku,
            request.Name,
            request.BaseUnitCode,
            request.CostingMethod,
            request.UnitSellingPrice,
            cancellationToken);
        if ((item.BaseUnitCode != fields.UnitCode || item.CostingMethod != fields.CostingMethod) &&
            await db.StockMovements.AnyAsync(movement => movement.ItemId == itemId, cancellationToken))
            throw new InventoryConflictException("The unit and costing method cannot change after stock movements exist.");

        item.CategoryId = request.CategoryId;
        item.Sku = fields.Sku;
        item.Name = fields.Name;
        item.BaseUnitCode = fields.UnitCode;
        item.CostingMethod = fields.CostingMethod;

        var price = await db.CompanyItemPrices.SingleOrDefaultAsync(
            value => value.CompanyId == companyId && value.ItemId == itemId,
            cancellationToken);
        if (price is null)
        {
            price = new CompanyItemPrice
            {
                TenantId = company.TenantId,
                CompanyId = companyId,
                ItemId = itemId,
                CurrencyCode = company.FunctionalCurrencyCode
            };
            db.CompanyItemPrices.Add(price);
        }
        price.UnitSellingPrice = request.UnitSellingPrice;
        price.CurrencyCode = company.FunctionalCurrencyCode;
        await SaveAsync(cancellationToken);
        return ToCompanyDto(item, price, company.FunctionalCurrencyCode);
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
}
