using Gasoft.Application.Abstractions;
using Gasoft.Application.Inventory;
using Gasoft.Application.PurchaseOrders;

namespace Gasoft.Api.Endpoints;

public static class PurchaseOrderEndpoints
{
    public static IEndpointRouteBuilder MapPurchaseOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var companies = app.MapGroup("/api/v1/companies/{companyId:long}/purchase-orders")
            .RequireAuthorization();

        companies.MapGet("", async (
            long companyId,
            IPurchaseOrderService service,
            CancellationToken cancellationToken) =>
        {
            var orders = await service.GetOrdersAsync(companyId, cancellationToken);
            return orders is null
                ? Results.NotFound(new { error = "Company was not found in this workspace." })
                : Results.Ok(orders);
        })
        .WithName("GetPurchaseOrders");
        companies.MapGet("/page", async (
            long companyId,
            int page,
            int pageSize,
            IPurchaseOrderService service,
            CancellationToken cancellationToken) =>
        {
            if (page < 1 || pageSize is < 1 or > 100)
                return Results.BadRequest(new { error = "Page must be positive and page size must be between 1 and 100." });
            var orders = await service.GetOrdersPageAsync(companyId, page, pageSize, cancellationToken);
            return orders is null
                ? Results.NotFound(new { error = "Company was not found in this workspace." })
                : Results.Ok(orders);
        });

        companies.MapGet("/{orderId:long}", async (
            long companyId,
            long orderId,
            IPurchaseOrderService service,
            CancellationToken cancellationToken) =>
        {
            var order = await service.GetOrderAsync(companyId, orderId, cancellationToken);
            return order is null
                ? Results.NotFound(new { error = "Purchase order was not found." })
                : Results.Ok(order);
        })
        .WithName("GetPurchaseOrder");

        companies.MapPost("", async (
            long companyId,
            PurchaseOrderRequest request,
            IPurchaseOrderService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var order = await service.CreateAsync(companyId, request, cancellationToken);
                return order is null
                    ? Results.NotFound(new { error = "Company was not found in this workspace." })
                    : Results.Created($"/api/v1/companies/{companyId}/purchase-orders/{order.Id}", order);
            }
            catch (PurchaseOrderValidationException exception)
            {
                return Results.ValidationProblem(exception.Errors);
            }
            catch (PurchaseOrderConflictException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner", "Admin"))
        .WithName("CreatePurchaseOrder");

        companies.MapPut("/{orderId:long}", async (
            long companyId,
            long orderId,
            PurchaseOrderRequest request,
            IPurchaseOrderService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var order = await service.UpdateAsync(companyId, orderId, request, cancellationToken);
                return order is null
                    ? Results.NotFound(new { error = "Purchase order was not found." })
                    : Results.Ok(order);
            }
            catch (PurchaseOrderValidationException exception)
            {
                return Results.ValidationProblem(exception.Errors);
            }
            catch (PurchaseOrderConflictException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner", "Admin"))
        .WithName("UpdatePurchaseOrder");

        companies.MapPost("/{orderId:long}/order", async (
            long companyId,
            long orderId,
            PurchaseOrderRevisionRequest request,
            IPurchaseOrderService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var order = await service.PlaceOrderAsync(companyId, orderId, request, cancellationToken);
                return order is null
                    ? Results.NotFound(new { error = "Purchase order was not found." })
                    : Results.Ok(order);
            }
            catch (PurchaseOrderConflictException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner", "Admin"))
        .WithName("PlacePurchaseOrder");

        companies.MapPost("/{orderId:long}/cancel", async (
            long companyId,
            long orderId,
            PurchaseOrderRevisionRequest request,
            IPurchaseOrderService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var order = await service.CancelAsync(companyId, orderId, request, cancellationToken);
                return order is null
                    ? Results.NotFound(new { error = "Purchase order was not found." })
                    : Results.Ok(order);
            }
            catch (PurchaseOrderConflictException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner", "Admin"))
        .WithName("CancelPurchaseOrder");

        companies.MapPost("/{orderId:long}/lines/{lineId:long}/receive", async (
            long companyId,
            long orderId,
            long lineId,
            ReceivePurchaseOrderLineRequest request,
            IPurchaseOrderService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var receipt = await service.ReceiveLineAsync(
                    companyId,
                    orderId,
                    lineId,
                    request,
                    cancellationToken);
                return receipt is null
                    ? Results.NotFound(new { error = "Purchase order line was not found." })
                    : Results.Ok(receipt);
            }
            catch (PurchaseOrderValidationException exception)
            {
                return Results.ValidationProblem(exception.Errors);
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
}
