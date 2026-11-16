using System.Security.Claims;
using Gasoft.Application.Abstractions;
using Gasoft.Application.TenantUsers;

namespace Gasoft.Api.Endpoints;

public static class TenantUserEndpoints
{
    public static IEndpointRouteBuilder MapTenantUserEndpoints(this IEndpointRouteBuilder app)
    {
        var users = app.MapGroup("/api/v1/tenant/users").RequireAuthorization();

        users.MapGet("", async (
            HttpContext context,
            ITenantUserService service,
            CancellationToken cancellationToken) =>
        {
            var manager = await GetManagerAsync(context, service, cancellationToken);
            return manager is null
                ? ForbiddenManager()
                : Results.Ok(await service.GetUsersAsync(manager.TenantId, cancellationToken));
        })
        .WithName("GetTenantUsers");

        users.MapGet("/page", async (
            int page,
            int pageSize,
            HttpContext context,
            ITenantUserService service,
            CancellationToken cancellationToken) =>
        {
            if (page < 1 || pageSize is < 1 or > 100)
                return Results.BadRequest(new { error = "Page must be positive and page size must be between 1 and 100." });
            var manager = await GetManagerAsync(context, service, cancellationToken);
            return manager is null
                ? ForbiddenManager()
                : Results.Ok(await service.GetUsersPageAsync(manager.TenantId, page, pageSize, cancellationToken));
        })
        .WithName("GetTenantUsersPage");

        users.MapGet("/{userId:long}", async (
            long userId,
            HttpContext context,
            ITenantUserService service,
            CancellationToken cancellationToken) =>
        {
            var manager = await GetManagerAsync(context, service, cancellationToken);
            if (manager is null) return ForbiddenManager();
            var user = await service.GetUserAsync(manager.TenantId, userId, cancellationToken);
            return user is null
                ? Results.NotFound(new { error = "Tenant user was not found." })
                : Results.Ok(user);
        })
        .WithName("GetTenantUser");

        users.MapPost("", async (
            CreateTenantUserRequest request,
            HttpContext context,
            ITenantUserService service,
            CancellationToken cancellationToken) =>
        {
            var manager = await GetManagerAsync(context, service, cancellationToken);
            if (manager is null) return ForbiddenManager();
            try
            {
                var user = await service.CreateAsync(manager, request, cancellationToken);
                return Results.Created($"/api/v1/tenant/users/{user.Id}", user);
            }
            catch (TenantUserValidationException exception)
            {
                return Results.ValidationProblem(exception.Errors);
            }
            catch (TenantUserConflictException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
        })
        .WithName("CreateTenantUser");

        users.MapPost("/existing", async (
            AddExistingTenantUserRequest request,
            HttpContext context,
            ITenantUserService service,
            CancellationToken cancellationToken) =>
        {
            var manager = await GetManagerAsync(context, service, cancellationToken);
            if (manager is null) return ForbiddenManager();
            try
            {
                var user = await service.AddExistingAsync(manager, request, cancellationToken);
                return Results.Created($"/api/v1/tenant/users/{user.Id}", user);
            }
            catch (TenantUserValidationException exception)
            {
                return Results.ValidationProblem(exception.Errors);
            }
            catch (TenantUserConflictException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
            catch (TenantUserNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        })
        .WithName("AddExistingTenantUser");

        users.MapPut("/{userId:long}", async (
            long userId,
            UpdateTenantUserRequest request,
            HttpContext context,
            ITenantUserService service,
            CancellationToken cancellationToken) =>
        {
            var manager = await GetManagerAsync(context, service, cancellationToken);
            if (manager is null) return Results.Forbid();
            try
            {
                var user = await service.UpdateAsync(manager, userId, request, cancellationToken);
                return user is null
                    ? Results.NotFound(new { error = "Tenant user was not found." })
                    : Results.Ok(user);
            }
            catch (TenantUserValidationException exception)
            {
                return Results.ValidationProblem(exception.Errors);
            }
            catch (TenantUserConflictException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
            catch (TenantUserForbiddenException exception)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Forbidden",
                    detail: exception.Message);
            }
        })
        .WithName("UpdateTenantUser");

        return app;
    }

    private static async Task<TenantManagerDto?> GetManagerAsync(
        HttpContext context,
        ITenantUserService service,
        CancellationToken cancellationToken)
    {
        if (!long.TryParse(context.User.FindFirstValue("tenant_id"), out var tenantId) ||
            !long.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return null;
        return await service.GetManagerAsync(tenantId, userId, cancellationToken);
    }

    private static IResult ForbiddenManager() =>
        Results.Problem(
            statusCode: StatusCodes.Status403Forbidden,
            title: "Forbidden",
            detail: "Only an active tenant Owner or Admin can manage users.");
}
