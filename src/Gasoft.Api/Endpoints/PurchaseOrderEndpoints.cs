using Gasoft.Application.Abstractions;
using Gasoft.Application.Inventory;
using Gasoft.Domain.Entities;
using Gasoft.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Gasoft.Api.Endpoints;

public static class PurchaseOrderEndpoints
{
    public static IEndpointRouteBuilder MapPurchaseOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var companies = app.MapGroup("/api/v1/companies/{companyId:long}/purchase-orders")
            .RequireAuthorization();

        companies.MapGet("", async (
            long companyId,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            if (!await db.Companies.AnyAsync(company => company.Id == companyId, cancellationToken))
                return Results.NotFound(new { error = "Company was not found in this workspace." });
            var orders = await db.PurchaseOrders.AsNoTracking()
                .Include(order => order.Lines)
                .Where(order => order.CompanyId == companyId)
                .OrderByDescending(order => order.OrderDate)
                .ThenByDescending(order => order.Id)
                .Take(500)
                .ToListAsync(cancellationToken);
            return Results.Ok(await ToResponsesAsync(db, orders, cancellationToken));
        })
        .WithName("GetPurchaseOrders");

        companies.MapGet("/{orderId:long}", async (
            long companyId,
            long orderId,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var order = await db.PurchaseOrders.AsNoTracking()
                .Include(item => item.Lines)
                .SingleOrDefaultAsync(item => item.Id == orderId && item.CompanyId == companyId, cancellationToken);
            if (order is null) return Results.NotFound(new { error = "Purchase order was not found." });
            return Results.Ok((await ToResponsesAsync(db, [order], cancellationToken))[0]);
        })
        .WithName("GetPurchaseOrder");

        companies.MapPost("", async (
            long companyId,
            PurchaseOrderRequest request,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var tenantId = db.CurrentTenantId;
            if (!tenantId.HasValue || !await db.Companies.AnyAsync(company => company.Id == companyId, cancellationToken))
                return Results.NotFound(new { error = "Company was not found in this workspace." });
            var validation = await ValidateRequestAsync(db, companyId, request, cancellationToken);
            if (validation.Errors.Count != 0) return Results.ValidationProblem(validation.Errors);
            var order = new PurchaseOrder
            {
                TenantId = tenantId.Value,
                CompanyId = companyId,
                SupplierId = request.SupplierId,
                WarehouseId = request.WarehouseId,
                OrderNumber = validation.OrderNumber,
                OrderDate = request.OrderDate,
                ExpectedDate = request.ExpectedDate,
                CurrencyCode = validation.CurrencyCode,
                ExchangeRate = request.ExchangeRate,
                Status = "Draft",
                Lines = validation.Lines.Select(line => new PurchaseOrderLine
                {
                    TenantId = tenantId.Value,
                    CompanyId = companyId,
                    ItemId = line.ItemId,
                    Description = line.Description,
                    Quantity = line.Quantity,
                    UnitCost = line.UnitCost
                }).ToList()
            };
            db.PurchaseOrders.Add(order);
            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
            {
                return Results.Conflict(new { error = $"Purchase order '{validation.OrderNumber}' already exists." });
            }
            return Results.Created(
                $"/api/v1/companies/{companyId}/purchase-orders/{order.Id}",
                (await ToResponsesAsync(db, [order], cancellationToken))[0]);
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner", "Admin"))
        .WithName("CreatePurchaseOrder");

        companies.MapPut("/{orderId:long}", async (
            long companyId,
            long orderId,
            PurchaseOrderRequest request,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var order = await db.PurchaseOrders.Include(item => item.Lines)
                .SingleOrDefaultAsync(item => item.Id == orderId && item.CompanyId == companyId, cancellationToken);
            if (order is null) return Results.NotFound(new { error = "Purchase order was not found." });
            if (order.Status != "Draft")
                return Results.Conflict(new { error = "Only draft purchase orders can be edited." });
            if (request.Revision != order.Revision)
                return Results.Conflict(new { error = "This purchase order changed. Reload it before saving." });
            var validation = await ValidateRequestAsync(db, companyId, request, cancellationToken, orderId);
            if (validation.Errors.Count != 0) return Results.ValidationProblem(validation.Errors);

            order.SupplierId = request.SupplierId;
            order.WarehouseId = request.WarehouseId;
            order.OrderNumber = validation.OrderNumber;
            order.OrderDate = request.OrderDate;
            order.ExpectedDate = request.ExpectedDate;
            order.CurrencyCode = validation.CurrencyCode;
            order.ExchangeRate = request.ExchangeRate;
            order.Revision++;
            db.PurchaseOrderLines.RemoveRange(order.Lines);
            order.Lines = validation.Lines.Select(line => new PurchaseOrderLine
            {
                TenantId = order.TenantId,
                CompanyId = companyId,
                ItemId = line.ItemId,
                Description = line.Description,
                Quantity = line.Quantity,
                UnitCost = line.UnitCost
            }).ToList();
            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Results.Conflict(new { error = "This purchase order changed. Reload it before saving." });
            }
            catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
            {
                return Results.Conflict(new { error = $"Purchase order '{validation.OrderNumber}' already exists." });
            }
            return Results.Ok((await ToResponsesAsync(db, [order], cancellationToken))[0]);
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner", "Admin"))
        .WithName("UpdatePurchaseOrder");

        companies.MapPost("/{orderId:long}/order", async (
            long companyId,
            long orderId,
            PurchaseOrderRevisionRequest request,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var order = await db.PurchaseOrders.Include(item => item.Lines)
                .SingleOrDefaultAsync(item => item.Id == orderId && item.CompanyId == companyId, cancellationToken);
            if (order is null) return Results.NotFound(new { error = "Purchase order was not found." });
            if (order.Status != "Draft") return Results.Conflict(new { error = "Only a draft purchase order can be ordered." });
            if (request.Revision != order.Revision) return Results.Conflict(new { error = "This purchase order changed. Reload it before ordering." });
            if (order.Lines.Count == 0) return Results.Conflict(new { error = "Add at least one line before ordering." });
            order.Status = "Ordered";
            order.Revision++;
            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Results.Conflict(new { error = "This purchase order changed. Reload it before ordering." });
            }
            return Results.Ok((await ToResponsesAsync(db, [order], cancellationToken))[0]);
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner", "Admin"))
        .WithName("PlacePurchaseOrder");

        companies.MapPost("/{orderId:long}/cancel", async (
            long companyId,
            long orderId,
            PurchaseOrderRevisionRequest request,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var order = await db.PurchaseOrders.SingleOrDefaultAsync(
                item => item.Id == orderId && item.CompanyId == companyId, cancellationToken);
            if (order is null) return Results.NotFound(new { error = "Purchase order was not found." });
            if (order.Status is not ("Draft" or "Ordered"))
                return Results.Conflict(new { error = "Only a draft or unreceived purchase order can be cancelled." });
            if (request.Revision != order.Revision)
                return Results.Conflict(new { error = "This purchase order changed. Reload it before cancelling." });
            order.Status = "Cancelled";
            order.Revision++;
            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Results.Conflict(new { error = "This purchase order changed. Reload it before cancelling." });
            }
            return Results.Ok(new { order.Id, order.Status, order.Revision });
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner", "Admin"))
        .WithName("CancelPurchaseOrder");

        companies.MapPost("/{orderId:long}/lines/{lineId:long}/receive", async (
            long companyId,
            long orderId,
            long lineId,
            ReceivePurchaseOrderLineRequest request,
            AppDbContext db,
            IInventoryService inventory,
            CancellationToken cancellationToken) =>
        {
            var line = await db.PurchaseOrderLines.AsNoTracking()
                .Where(item => item.Id == lineId &&
                               item.PurchaseOrderId == orderId &&
                               item.CompanyId == companyId)
                .Select(item => new { item.ItemId, item.UnitCost })
                .SingleOrDefaultAsync(cancellationToken);
            if (line is null) return Results.NotFound(new { error = "Purchase order line was not found." });
            var order = await db.PurchaseOrders.AsNoTracking()
                .Where(item => item.Id == orderId && item.CompanyId == companyId)
                .Select(item => new { item.OrderDate, item.ExchangeRate, item.WarehouseId })
                .SingleOrDefaultAsync(cancellationToken);
            if (order is null) return Results.NotFound(new { error = "Purchase order was not found." });
            if (string.IsNullOrWhiteSpace(request.MovementNumber) ||
                request.MovementNumber.Trim().Length > 40)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["movementNumber"] = ["Movement number is required and must be at most 40 characters."]
                });
            try
            {
                var movement = await inventory.ReceiveAsync(companyId, new ReceiveStockRequest(
                    request.MovementNumber,
                    line.ItemId,
                    order.WarehouseId,
                    request.WarehouseLocationId,
                    request.JournalId,
                    request.AccountingPeriodId,
                    request.InventoryAccountId,
                    request.OffsetAccountId,
                    request.MovementDate,
                    request.Quantity,
                    decimal.Round(line.UnitCost * order.ExchangeRate, 6, MidpointRounding.AwayFromZero),
                    lineId), cancellationToken);
                var updated = await db.PurchaseOrders.AsNoTracking()
                    .Where(item => item.Id == orderId && item.CompanyId == companyId)
                    .Select(item => item.Status)
                    .SingleAsync(cancellationToken);
                return Results.Ok(new { movement, purchaseOrderStatus = updated });
            }
            catch (InventoryValidationException exception)
            {
                return Results.ValidationProblem(exception.Errors);
            }
            catch (InventoryConflictException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner", "Admin"))
        .WithName("ReceivePurchaseOrderLine");

        return app;
    }

    private static async Task<PurchaseOrderValidation> ValidateRequestAsync(
        AppDbContext db,
        long companyId,
        PurchaseOrderRequest request,
        CancellationToken cancellationToken,
        long? currentOrderId = null)
    {
        var orderNumber = request.OrderNumber?.Trim().ToUpperInvariant() ?? string.Empty;
        var currency = request.CurrencyCode?.Trim().ToUpperInvariant() ?? string.Empty;
        var errors = new Dictionary<string, string[]>();
        if (orderNumber.Length is 0 or > 40) errors["orderNumber"] = ["Order number is required and must be at most 40 characters."];
        if (request.OrderDate == default) errors["orderDate"] = ["Choose a valid order date."];
        if (request.ExpectedDate.HasValue && request.ExpectedDate.Value < request.OrderDate)
            errors["expectedDate"] = ["Expected date cannot be before the order date."];
        if (request.ExchangeRate <= 0 || request.ExchangeRate > 1_000_000_000m)
            errors["exchangeRate"] = ["Exchange rate must be greater than zero and within the supported range."];
        if (await db.PurchaseOrders.AnyAsync(
                order => order.CompanyId == companyId &&
                         order.OrderNumber == orderNumber &&
                         (!currentOrderId.HasValue || order.Id != currentOrderId.Value),
                cancellationToken))
            errors["orderNumber"] = ["This purchase order number is already in use."];
        if (!await db.Parties.AnyAsync(
                party => party.Id == request.SupplierId && party.IsSupplier,
                cancellationToken))
            errors["supplierId"] = ["Choose a supplier in this workspace."];
        if (!await db.Warehouses.AnyAsync(
                warehouse => warehouse.Id == request.WarehouseId && warehouse.CompanyId == companyId,
                cancellationToken))
            errors["warehouseId"] = ["Choose a warehouse in this company."];
        if (currency.Length != 3 || !await db.Currencies.AnyAsync(item => item.Code == currency, cancellationToken))
            errors["currencyCode"] = ["Choose a supported three-letter currency code."];
        var functionalCurrency = await db.Companies
            .Where(company => company.Id == companyId)
            .Select(company => company.FunctionalCurrencyCode)
            .SingleAsync(cancellationToken);
        if (currency == functionalCurrency && request.ExchangeRate != 1)
            errors["exchangeRate"] = ["Exchange rate must be 1 when the order uses the company's functional currency."];

        var requestedLines = request.Lines ?? [];
        if (requestedLines.Count is 0 or > 200)
            errors["lines"] = ["Add between 1 and 200 purchase order lines."];
        var validatedLines = new List<ValidatedLine>();
        for (var index = 0; index < requestedLines.Count; index++)
        {
            var line = requestedLines[index];
            var prefix = $"lines[{index}]";
            var description = line.Description?.Trim() ?? string.Empty;
            if (description.Length is 0 or > 500)
                errors[$"{prefix}.description"] = ["Description is required and must be at most 500 characters."];
            if (line.Quantity <= 0 || line.Quantity > 9_999_999_999.999999m || decimal.Round(line.Quantity, 6) != line.Quantity)
                errors[$"{prefix}.quantity"] = ["Quantity must be positive and support at most 6 decimal places."];
            if (line.UnitCost <= 0 || line.UnitCost > 99_999_999_999_999.999999m || decimal.Round(line.UnitCost, 6) != line.UnitCost)
                errors[$"{prefix}.unitCost"] = ["Unit cost must be positive and support at most 6 decimal places."];
            if (!await db.Items.AnyAsync(item => item.Id == line.ItemId && item.IsActive, cancellationToken))
                errors[$"{prefix}.itemId"] = ["Choose an active item in this workspace."];
            validatedLines.Add(new ValidatedLine(line.ItemId, description, line.Quantity, line.UnitCost));
        }
        return new PurchaseOrderValidation(orderNumber, currency, validatedLines, errors);
    }

    private static async Task<IReadOnlyList<PurchaseOrderResponse>> ToResponsesAsync(
        AppDbContext db,
        IReadOnlyList<PurchaseOrder> orders,
        CancellationToken cancellationToken)
    {
        var orderIds = orders.Select(order => order.Id).ToArray();
        var partyIds = orders.Select(order => order.SupplierId).Distinct().ToArray();
        var itemIds = orders.SelectMany(order => order.Lines).Select(line => line.ItemId).Distinct().ToArray();
        var parties = await db.Parties.AsNoTracking()
            .Where(party => partyIds.Contains(party.Id))
            .ToDictionaryAsync(party => party.Id, party => party.DisplayName, cancellationToken);
        var items = await db.Items.AsNoTracking()
            .Where(item => itemIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => new { item.Sku, item.Name, item.BaseUnitCode }, cancellationToken);
        var receipts = await db.PurchaseOrderLines.AsNoTracking()
            .Where(line => orderIds.Contains(line.PurchaseOrderId))
            .ToDictionaryAsync(line => line.Id, line => line.ReceivedQuantity, cancellationToken);
        return orders.Select(order => new PurchaseOrderResponse(
            order.Id,
            order.CompanyId,
            order.SupplierId,
            parties.GetValueOrDefault(order.SupplierId, "Supplier unavailable"),
            order.WarehouseId,
            order.OrderNumber,
            order.OrderDate,
            order.ExpectedDate,
            order.CurrencyCode,
            order.ExchangeRate,
            order.Status,
            order.Revision,
            order.Lines.OrderBy(line => line.Id).Select(line => new PurchaseOrderLineResponse(
                line.Id,
                line.ItemId,
                items.TryGetValue(line.ItemId, out var item) ? item.Sku : string.Empty,
                items.TryGetValue(line.ItemId, out item) ? item.Name : "Item unavailable",
                item?.BaseUnitCode ?? string.Empty,
                line.Description,
                line.Quantity,
                receipts.GetValueOrDefault(line.Id),
                line.UnitCost)).ToArray()))
            .ToArray();
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

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
    public sealed record PurchaseOrderLineResponse(
        long Id,
        long ItemId,
        string Sku,
        string ItemName,
        string UnitCode,
        string Description,
        decimal Quantity,
        decimal ReceivedQuantity,
        decimal UnitCost);
    public sealed record PurchaseOrderResponse(
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
        IReadOnlyList<PurchaseOrderLineResponse> Lines);

    private sealed record ValidatedLine(long ItemId, string Description, decimal Quantity, decimal UnitCost);
    private sealed record PurchaseOrderValidation(
        string OrderNumber,
        string CurrencyCode,
        IReadOnlyList<ValidatedLine> Lines,
        Dictionary<string, string[]> Errors);
}
