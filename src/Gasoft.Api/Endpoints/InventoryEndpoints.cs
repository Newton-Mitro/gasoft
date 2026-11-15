using Gasoft.Application.Abstractions;
using Gasoft.Application.Inventory;

namespace Gasoft.Api.Endpoints;

public static class InventoryEndpoints
{
    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/units-of-measure", async (
            IInventoryService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.GetUnitsOfMeasureAsync(cancellationToken)))
            .AllowAnonymous();

        var tenant = app.MapGroup("/api/v1").RequireAuthorization();
        tenant.MapGet("/item-categories", async (
            IInventoryService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.GetCategoriesAsync(cancellationToken)));
        tenant.MapGet("/item-categories/{categoryId:long}", async (
            long categoryId,
            IInventoryService service,
            CancellationToken cancellationToken) =>
        {
            var category = await service.GetCategoryAsync(categoryId, cancellationToken);
            return category is null ? Results.NotFound(new { error = "Item category was not found." }) : Results.Ok(category);
        });
        tenant.MapPost("/item-categories", async (
            CreateItemCategoryRequest request,
            IInventoryService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var category = await service.CreateCategoryAsync(request, cancellationToken);
                return Results.Created($"/api/v1/item-categories/{category.Id}", category);
            }
            catch (InventoryValidationException exception)
            {
                return ValidationProblem(exception);
            }
            catch (InventoryConflictException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
        });
        tenant.MapPut("/item-categories/{categoryId:long}", async (
            long categoryId,
            UpdateItemCategoryRequest request,
            IInventoryService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var category = await service.UpdateCategoryAsync(categoryId, request, cancellationToken);
                return category is null ? Results.NotFound(new { error = "Item category was not found." }) : Results.Ok(category);
            }
            catch (InventoryValidationException exception)
            {
                return ValidationProblem(exception);
            }
            catch (InventoryConflictException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
        });
        tenant.MapGet("/items", async (
            IInventoryService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.GetItemsAsync(cancellationToken)));
        tenant.MapGet("/items/{itemId:long}", async (
            long itemId,
            IInventoryService service,
            CancellationToken cancellationToken) =>
        {
            var item = await service.GetItemAsync(itemId, cancellationToken);
            return item is null ? Results.NotFound(new { error = "Item was not found." }) : Results.Ok(item);
        });
        tenant.MapPost("/items", async (
            CreateItemRequest request,
            IInventoryService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var item = await service.CreateItemAsync(request, cancellationToken);
                return Results.Created($"/api/v1/items/{item.Id}", item);
            }
            catch (InventoryValidationException exception)
            {
                return ValidationProblem(exception);
            }
            catch (InventoryConflictException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
        });
        tenant.MapPut("/items/{itemId:long}", async (
            long itemId,
            UpdateItemRequest request,
            IInventoryService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var item = await service.UpdateItemAsync(itemId, request, cancellationToken);
                return item is null ? Results.NotFound(new { error = "Item was not found." }) : Results.Ok(item);
            }
            catch (InventoryValidationException exception)
            {
                return ValidationProblem(exception);
            }
            catch (InventoryConflictException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
        });

        var companies = app.MapGroup("/api/v1/companies/{companyId:long}").RequireAuthorization();
        companies.MapGet("/warehouses", async (
            long companyId,
            IInventoryService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                return Results.Ok(await service.GetWarehousesAsync(companyId, cancellationToken));
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });
        companies.MapGet("/warehouses/{warehouseId:long}", async (
            long companyId,
            long warehouseId,
            IInventoryService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var warehouse = await service.GetWarehouseAsync(companyId, warehouseId, cancellationToken);
                return warehouse is null ? Results.NotFound(new { error = "Warehouse was not found in this company." }) : Results.Ok(warehouse);
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });
        companies.MapPost("/warehouses", async (
            long companyId,
            CreateWarehouseRequest request,
            IInventoryService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var warehouse = await service.CreateWarehouseAsync(companyId, request, cancellationToken);
                return Results.Created($"/api/v1/companies/{companyId}/warehouses/{warehouse.Id}", warehouse);
            }
            catch (InventoryValidationException exception)
            {
                return ValidationProblem(exception);
            }
            catch (InventoryConflictException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });
        companies.MapPut("/warehouses/{warehouseId:long}", async (
            long companyId,
            long warehouseId,
            UpdateWarehouseRequest request,
            IInventoryService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var warehouse = await service.UpdateWarehouseAsync(companyId, warehouseId, request, cancellationToken);
                return warehouse is null ? Results.NotFound(new { error = "Warehouse was not found in this company." }) : Results.Ok(warehouse);
            }
            catch (InventoryValidationException exception)
            {
                return ValidationProblem(exception);
            }
            catch (InventoryConflictException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });
        companies.MapPost("/warehouses/{warehouseId:long}/locations", async (
            long companyId,
            long warehouseId,
            CreateWarehouseLocationRequest request,
            IInventoryService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var location = await service.CreateLocationAsync(
                    companyId, warehouseId, request, cancellationToken);
                return Results.Created(
                    $"/api/v1/companies/{companyId}/warehouses/{warehouseId}/locations/{location.Id}",
                    location);
            }
            catch (InventoryValidationException exception)
            {
                return ValidationProblem(exception);
            }
            catch (InventoryConflictException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });
        companies.MapGet("/inventory/balances", async (
            long companyId,
            IInventoryService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                return Results.Ok(await service.GetStockBalancesAsync(companyId, cancellationToken));
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });
        companies.MapGet("/inventory/movements", async (
            long companyId,
            IInventoryService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                return Results.Ok(await service.GetStockMovementsAsync(companyId, cancellationToken));
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });
        companies.MapGet("/inventory/counts", async (
            long companyId,
            IInventoryService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                return Results.Ok(await service.GetCountsAsync(companyId, cancellationToken));
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });
        companies.MapGet("/inventory/counts/{countId:long}", async (
            long companyId,
            long countId,
            IInventoryService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var count = await service.GetCountAsync(companyId, countId, cancellationToken);
                return count is null ? Results.NotFound(new { error = "Inventory count was not found in this company." }) : Results.Ok(count);
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });
        companies.MapPost("/inventory/counts", async (
            long companyId,
            CreateInventoryCountRequest request,
            IInventoryService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var count = await service.CreateCountAsync(companyId, request, cancellationToken);
                return Results.Created($"/api/v1/companies/{companyId}/inventory/counts/{count.Id}", count);
            }
            catch (InventoryValidationException exception)
            {
                return ValidationProblem(exception);
            }
            catch (InventoryConflictException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });
        companies.MapPut("/inventory/counts/{countId:long}", async (
            long companyId,
            long countId,
            UpdateInventoryCountRequest request,
            IInventoryService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var count = await service.UpdateCountAsync(companyId, countId, request, cancellationToken);
                return count is null ? Results.NotFound(new { error = "Inventory count was not found in this company." }) : Results.Ok(count);
            }
            catch (InventoryValidationException exception)
            {
                return ValidationProblem(exception);
            }
            catch (InventoryConflictException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });
        companies.MapPost("/inventory/counts/{countId:long}/post", async (
            long companyId,
            long countId,
            PostInventoryCountRequest request,
            IInventoryService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var count = await service.PostCountAsync(companyId, countId, request, cancellationToken);
                return count is null ? Results.NotFound() : Results.Ok(count);
            }
            catch (InventoryValidationException exception)
            {
                return ValidationProblem(exception);
            }
            catch (InventoryConflictException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });
        companies.MapPost("/inventory/receipts", async (
            long companyId,
            ReceiveStockRequest request,
            IInventoryService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var movement = await service.ReceiveAsync(companyId, request, cancellationToken);
                return Results.Created(
                    $"/api/v1/companies/{companyId}/inventory/movements/{movement.Id}",
                    movement);
            }
            catch (InventoryValidationException exception)
            {
                return ValidationProblem(exception);
            }
            catch (InventoryConflictException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });
        companies.MapPost("/inventory/issues", async (
            long companyId,
            IssueStockRequest request,
            IInventoryService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var movement = await service.IssueAsync(companyId, request, cancellationToken);
                return Results.Created(
                    $"/api/v1/companies/{companyId}/inventory/movements/{movement.Id}",
                    movement);
            }
            catch (InventoryValidationException exception)
            {
                return ValidationProblem(exception);
            }
            catch (InventoryConflictException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });

        return app;
    }

    private static IResult ValidationProblem(InventoryValidationException exception) =>
        Results.ValidationProblem(exception.Errors.ToDictionary(error => error.Key, error => error.Value));
}
