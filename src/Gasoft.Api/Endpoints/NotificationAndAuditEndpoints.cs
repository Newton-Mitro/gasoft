using Gasoft.Application.Common;
using Gasoft.Application.Abstractions;
using Gasoft.Domain.Entities;
using Gasoft.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Gasoft.Api.Endpoints;

public static class NotificationAndAuditEndpoints
{
    public static IEndpointRouteBuilder MapNotificationAndAuditEndpoints(this IEndpointRouteBuilder app)
    {
        var notifications = app.MapGroup("/api/v1/notifications").RequireAuthorization();

        notifications.MapGet("", async (
            int page,
            int pageSize,
            AppDbContext db,
            ICurrentTenant currentTenant,
            CancellationToken cancellationToken) =>
        {
            if (!ValidPage(page, pageSize) || currentTenant.TenantId is not long tenantId ||
                currentTenant.UserId is not long userId)
                return Results.BadRequest(new { error = "A valid tenant, user, page, and page size are required." });

            var query = db.Notifications.AsNoTracking()
                .Where(item => item.TenantId == tenantId && item.UserId == userId && item.DismissedAt == null);
            var totalCount = await query.CountAsync(cancellationToken);
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            var normalizedPage = totalPages == 0 ? 1 : Math.Min(page, totalPages);
            var items = await query
                .OrderByDescending(item => item.CreatedAt)
                .ThenByDescending(item => item.Id)
                .Skip((normalizedPage - 1) * pageSize)
                .Take(pageSize)
                .Select(item => new NotificationDto(
                    item.Id,
                    item.Title,
                    item.Message,
                    item.Category,
                    item.Link,
                    item.CreatedAt,
                    item.ReadAt))
                .ToListAsync(cancellationToken);

            return Results.Ok(new PagedResultDto<NotificationDto>(
                items,
                normalizedPage,
                pageSize,
                totalCount,
                totalPages));
        })
        .WithName("GetNotifications");

        notifications.MapGet("/unread-count", async (
            AppDbContext db,
            ICurrentTenant currentTenant,
            CancellationToken cancellationToken) =>
        {
            if (currentTenant.TenantId is not long tenantId || currentTenant.UserId is not long userId)
                return Results.BadRequest(new { error = "A valid tenant and user are required." });

            var count = await db.Notifications.CountAsync(
                item => item.TenantId == tenantId && item.UserId == userId &&
                        item.ReadAt == null && item.DismissedAt == null,
                cancellationToken);
            return Results.Ok(new UnreadNotificationCount(count));
        })
        .WithName("GetNotificationUnreadCount");

        notifications.MapPost("/{notificationId:long}/read", async (
            long notificationId,
            AppDbContext db,
            ICurrentTenant currentTenant,
            CancellationToken cancellationToken) =>
        {
            var item = await FindNotificationAsync(db, currentTenant, notificationId, cancellationToken);
            if (item is null) return Results.NotFound();
            item.ReadAt ??= DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return Results.NoContent();
        })
        .WithName("MarkNotificationRead");

        notifications.MapDelete("/{notificationId:long}", async (
            long notificationId,
            AppDbContext db,
            ICurrentTenant currentTenant,
            CancellationToken cancellationToken) =>
        {
            var item = await FindNotificationAsync(db, currentTenant, notificationId, cancellationToken);
            if (item is null) return Results.NotFound();
            item.DismissedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return Results.NoContent();
        })
        .WithName("DismissNotification");

        app.MapGet("/api/v1/audit", async (
            int page,
            int pageSize,
            string? action,
            string? entity,
            DateTimeOffset? from,
            DateTimeOffset? to,
            AppDbContext db,
            ICurrentTenant currentTenant,
            CancellationToken cancellationToken) =>
        {
            if (!ValidPage(page, pageSize) || currentTenant.TenantId is not long tenantId ||
                (action?.Length ?? 0) > 40 || (entity?.Length ?? 0) > 200 ||
                (from.HasValue && to.HasValue && from > to))
                return Results.BadRequest(new { error = "The audit filters or pagination values are invalid." });

            var query = db.AuditLogs.AsNoTracking().Where(item => item.TenantId == tenantId);
            if (!string.IsNullOrWhiteSpace(action))
                query = query.Where(item => item.Action == action.Trim());
            if (!string.IsNullOrWhiteSpace(entity))
                query = query.Where(item => item.EntityType.Contains(entity.Trim()));
            if (from.HasValue)
                query = query.Where(item => item.CreatedAt >= from.Value);
            if (to.HasValue)
                query = query.Where(item => item.CreatedAt <= to.Value);

            var totalCount = await query.CountAsync(cancellationToken);
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            var normalizedPage = totalPages == 0 ? 1 : Math.Min(page, totalPages);
            var items = await query
                .OrderByDescending(item => item.CreatedAt)
                .ThenByDescending(item => item.Id)
                .Skip((normalizedPage - 1) * pageSize)
                .Take(pageSize)
                .Select(item => new AuditEntryDto(
                    item.Id,
                    item.Action,
                    item.EntityType,
                    item.EntityId,
                    item.ActorUserId,
                    db.Users.Where(user => user.Id == item.ActorUserId)
                        .Select(user => user.DisplayName ?? user.Email)
                        .FirstOrDefault() ?? "System",
                    item.DetailsJson,
                    item.CreatedAt))
                .ToListAsync(cancellationToken);

            return Results.Ok(new PagedResultDto<AuditEntryDto>(
                items,
                normalizedPage,
                pageSize,
                totalCount,
                totalPages));
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner", "Admin"))
        .WithName("GetAuditHistory");

        return app;
    }

    private static Task<AppNotification?> FindNotificationAsync(
        AppDbContext db,
        ICurrentTenant currentTenant,
        long notificationId,
        CancellationToken cancellationToken) =>
        currentTenant.TenantId is long tenantId && currentTenant.UserId is long userId
            ? db.Notifications.SingleOrDefaultAsync(
                item => item.Id == notificationId && item.TenantId == tenantId && item.UserId == userId,
                cancellationToken)
            : Task.FromResult<AppNotification?>(null);

    private static bool ValidPage(int page, int pageSize) => page > 0 && pageSize is >= 1 and <= 100;

    private sealed record NotificationDto(
        long Id,
        string Title,
        string Message,
        string Category,
        string? Link,
        DateTimeOffset CreatedAt,
        DateTimeOffset? ReadAt);

    private sealed record UnreadNotificationCount(int Count);

    private sealed record AuditEntryDto(
        long Id,
        string Action,
        string EntityType,
        string? EntityId,
        long? ActorUserId,
        string ActorName,
        string DetailsJson,
        DateTimeOffset CreatedAt);
}
