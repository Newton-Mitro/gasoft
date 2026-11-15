using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Gasoft.Infrastructure.Authentication;
using Gasoft.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Gasoft.Api.Endpoints;

public static class TenantUserEndpoints
{
    private static readonly string[] AssignableRoles = ["Admin", "Member"];

    public static IEndpointRouteBuilder MapTenantUserEndpoints(this IEndpointRouteBuilder app)
    {
        var users = app.MapGroup("/api/v1/tenant/users").RequireAuthorization();

        users.MapGet("", async (
            HttpContext context,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var manager = await GetManagerAsync(context, db, cancellationToken);
            if (manager is null) return ForbiddenManager();

            var tenantUsers = await db.TenantUsers
                .Where(membership => membership.TenantId == manager.TenantId)
                .Join(db.Users, membership => membership.UserId, user => user.Id, (membership, user) =>
                    new
                    {
                        user.Id,
                        Email = user.Email ?? string.Empty,
                        DisplayName = user.DisplayName ?? string.Empty,
                        membership.Role,
                        membership.IsActive,
                    })
                .ToListAsync(cancellationToken);

            return Results.Ok(tenantUsers
                .OrderBy(user => user.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(user => user.Email, StringComparer.OrdinalIgnoreCase)
                .Select(user => new TenantUserResponse(
                    user.Id,
                    user.Email,
                    user.DisplayName,
                    user.Role,
                    user.IsActive)));
        })
        .WithName("GetTenantUsers");

        users.MapGet("/{userId:long}", async (
            long userId,
            HttpContext context,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var manager = await GetManagerAsync(context, db, cancellationToken);
            if (manager is null) return ForbiddenManager();

            var tenantUser = await db.TenantUsers
                .Where(membership => membership.TenantId == manager.TenantId && membership.UserId == userId)
                .Join(db.Users, membership => membership.UserId, user => user.Id, (membership, user) =>
                    new TenantUserResponse(
                        user.Id,
                        user.Email ?? string.Empty,
                        user.DisplayName ?? string.Empty,
                        membership.Role,
                        membership.IsActive))
                .SingleOrDefaultAsync(cancellationToken);

            return tenantUser is null
                ? Results.NotFound(new { error = "Tenant user was not found." })
                : Results.Ok(tenantUser);
        })
        .WithName("GetTenantUser");

        users.MapPost("", async (
            CreateTenantUserRequest request,
            HttpContext context,
            AppDbContext db,
            UserManager<ApplicationUser> userManager,
            CancellationToken cancellationToken) =>
        {
            var manager = await GetManagerAsync(context, db, cancellationToken);
            if (manager is null) return ForbiddenManager();

            var email = request.Email?.Trim() ?? string.Empty;
            var displayName = request.DisplayName?.Trim() ?? string.Empty;
            var password = request.TemporaryPassword ?? string.Empty;
            var role = NormalizeRole(request.Role);
            var errors = new Dictionary<string, string[]>();
            if (email.Length > 256 || !new EmailAddressAttribute().IsValid(email))
                errors["email"] = ["Enter a valid email address (maximum 256 characters)."];
            if (displayName.Length is 0 or > 200)
                errors["displayName"] = ["Display name is required and must be at most 200 characters."];
            if (password.Length is 0 or > 128)
                errors["temporaryPassword"] = ["Temporary password is required and must be at most 128 characters."];
            if (role is null || (role == "Owner" && manager.Role != "Owner"))
                errors["role"] = ["Choose a role you are permitted to assign."];
            if (errors.Count != 0) return Results.ValidationProblem(errors);

            if (await userManager.FindByEmailAsync(email) is not null)
                return Results.Conflict(new { error = "An account with that email address already exists." });

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                DisplayName = displayName
            };
            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                var passwordErrors = createResult.Errors.Select(error => error.Description).ToArray();
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["temporaryPassword"] = passwordErrors
                });
            }

            var membership = new TenantUser
            {
                TenantId = manager.TenantId,
                UserId = user.Id,
                Role = role!,
                IsActive = true
            };
            db.TenantUsers.Add(membership);
            try
            {
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
            {
                await transaction.RollbackAsync(cancellationToken);
                return Results.Conflict(new { error = "An account with that email address already exists." });
            }

            return Results.Created(
                $"/api/v1/tenant/users/{user.Id}",
                new TenantUserResponse(user.Id, user.Email!, user.DisplayName!, membership.Role, membership.IsActive));
        })
        .WithName("CreateTenantUser");

        users.MapPut("/{userId:long}", async (
            long userId,
            UpdateTenantUserRequest request,
            HttpContext context,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var manager = await GetManagerAsync(context, db, cancellationToken);
            if (manager is null) return Results.Forbid();

            var membership = await db.TenantUsers.SingleOrDefaultAsync(
                item => item.TenantId == manager.TenantId && item.UserId == userId,
                cancellationToken);
            if (membership is null) return Results.NotFound(new { error = "Tenant user was not found." });

            var role = NormalizeRole(request.Role);
            if (role is null || (role == "Owner" && manager.Role != "Owner"))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["role"] = ["Choose a role you are permitted to assign."]
                });
            if (membership.Role == "Owner" && manager.Role != "Owner")
                return Results.Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Forbidden",
                    detail: "Only an Owner can change another Owner's access.");

            var isChangingOwnerStatus = membership.Role == "Owner" &&
                                       membership.IsActive &&
                                       (!request.IsActive || role != "Owner");
            if (isChangingOwnerStatus &&
                !await db.TenantUsers.AnyAsync(
                    item => item.TenantId == manager.TenantId &&
                            item.Role == "Owner" &&
                            item.IsActive &&
                            item.UserId != userId,
                    cancellationToken))
            {
                return Results.Conflict(new { error = "The tenant must have at least one active owner." });
            }

            membership.Role = role;
            membership.IsActive = request.IsActive;
            await db.SaveChangesAsync(cancellationToken);

            var user = await db.Users.AsNoTracking()
                .Where(item => item.Id == userId)
                .Select(item => new { item.Email, item.DisplayName })
                .SingleAsync(cancellationToken);
            return Results.Ok(new TenantUserResponse(
                userId,
                user.Email ?? string.Empty,
                user.DisplayName ?? string.Empty,
                membership.Role,
                membership.IsActive));
        })
        .WithName("UpdateTenantUser");

        return app;
    }

    private static async Task<ManagerContext?> GetManagerAsync(
        HttpContext context,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        if (!long.TryParse(context.User.FindFirstValue("tenant_id"), out var tenantId) ||
            !long.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return null;

        var role = await db.TenantUsers
            .Where(membership =>
                membership.TenantId == tenantId &&
                membership.UserId == userId &&
                membership.IsActive &&
                (membership.Role == "Owner" || membership.Role == "Admin"))
            .Select(membership => membership.Role)
            .SingleOrDefaultAsync(cancellationToken);

        return role is null ? null : new ManagerContext(tenantId, role);
    }

    private static string? NormalizeRole(string? role)
    {
        var normalized = role?.Trim();
        return normalized == "Owner" || AssignableRoles.Contains(normalized, StringComparer.OrdinalIgnoreCase)
            ? normalized == "Owner" ? "Owner" : AssignableRoles.Single(item => item.Equals(normalized, StringComparison.OrdinalIgnoreCase))
            : null;
    }

    private static IResult ForbiddenManager() =>
        Results.Problem(
            statusCode: StatusCodes.Status403Forbidden,
            title: "Forbidden",
            detail: "Only an active tenant Owner or Admin can manage users.");

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    private sealed record ManagerContext(long TenantId, string Role);
    public sealed record CreateTenantUserRequest(string? Email, string? DisplayName, string? TemporaryPassword, string? Role);
    public sealed record UpdateTenantUserRequest(string? Role, bool IsActive);
    public sealed record TenantUserResponse(long Id, string Email, string DisplayName, string Role, bool IsActive);
}
