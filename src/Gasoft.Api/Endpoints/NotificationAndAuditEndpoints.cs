using Gasoft.Application.Common;
using Gasoft.Application.Abstractions;
using Gasoft.Domain.Auditing;
using Gasoft.Domain.Entities;
using Gasoft.Domain.Notifications;
using Gasoft.Infrastructure.Authentication;
using Gasoft.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

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
                .Where(item => item.TenantId == tenantId && item.UserId == userId &&
                    item.DismissedAt == null &&
                    !db.NotificationPreferences.IgnoreQueryFilters().Any(preference =>
                        preference.TenantId == tenantId &&
                        preference.UserId == userId &&
                        preference.EventType == item.EventType &&
                        !preference.InAppEnabled));
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

        notifications.MapGet("/preferences", async (
            AppDbContext db,
            ICurrentTenant currentTenant,
            IOptions<EmailOptions> emailOptions,
            CancellationToken cancellationToken) =>
        {
            if (currentTenant.TenantId is not long tenantId || currentTenant.UserId is not long userId)
                return Results.BadRequest(new { error = "A valid tenant and user are required." });

            var preferences = await db.NotificationPreferences.IgnoreQueryFilters()
                .Where(item => item.TenantId == tenantId && item.UserId == userId)
                .ToDictionaryAsync(item => item.EventType, cancellationToken);
            var tenantEmailConfiguration = await db.TenantEmailConfigurations.IgnoreQueryFilters()
                .SingleOrDefaultAsync(item => item.TenantId == tenantId, cancellationToken);
            var tenantEmailConfigured = tenantEmailConfiguration is not null &&
                !string.IsNullOrWhiteSpace(tenantEmailConfiguration.SmtpHost) &&
                tenantEmailConfiguration.SmtpPort is > 0 and <= 65535 &&
                !string.IsNullOrWhiteSpace(tenantEmailConfiguration.FromEmail) &&
                (string.IsNullOrWhiteSpace(tenantEmailConfiguration.SmtpUsername) ||
                 !string.IsNullOrEmpty(tenantEmailConfiguration.ProtectedSmtpPassword));
            var globalEmailConfigured = emailOptions.Value.IsSmtpConfigured;
            var emailConfigurationSource = tenantEmailConfigured
                ? "tenant"
                : globalEmailConfigured ? "global" : "none";
            return Results.Ok(new NotificationPreferencesDto(
                NotificationEvents.All.Select(item => new NotificationPreferenceDto(
                    item.EventType,
                    item.Category,
                    item.Name,
                    item.Description,
                    !preferences.TryGetValue(item.EventType, out var preference) || preference.InAppEnabled,
                    preferences.TryGetValue(item.EventType, out preference) && preference.EmailEnabled)).ToArray(),
                tenantEmailConfigured || globalEmailConfigured,
                emailConfigurationSource));
        })
        .WithName("GetNotificationPreferences");

        notifications.MapPut("/preferences", async (
            SaveNotificationPreferencesRequest request,
            AppDbContext db,
            ICurrentTenant currentTenant,
            CancellationToken cancellationToken) =>
        {
            if (currentTenant.TenantId is not long tenantId || currentTenant.UserId is not long userId ||
                request.Events is null ||
                request.Events.Count != NotificationEvents.All.Count ||
                request.Events.Any(item => item is null) ||
                request.Events.Select(item => item.EventType).Distinct().Count() != NotificationEvents.All.Count ||
                request.Events.Any(item => !NotificationEvents.Contains(item.EventType)))
                return Results.BadRequest(new { error = "Provide one preference for every supported notification event." });

            var existing = await db.NotificationPreferences.IgnoreQueryFilters()
                .Where(item => item.TenantId == tenantId && item.UserId == userId)
                .ToDictionaryAsync(item => item.EventType, cancellationToken);
            foreach (var item in request.Events)
            {
                if (!existing.TryGetValue(item.EventType, out var preference))
                {
                    preference = new NotificationPreference
                    {
                        TenantId = tenantId,
                        UserId = userId,
                        EventType = item.EventType,
                        InAppEnabled = item.InAppEnabled,
                        EmailEnabled = item.EmailEnabled
                    };
                    db.NotificationPreferences.Add(preference);
                }
                else
                {
                    preference.InAppEnabled = item.InAppEnabled;
                    preference.EmailEnabled = item.EmailEnabled;
                }
            }

            await db.SaveChangesAsync(cancellationToken);
            return Results.NoContent();
        })
        .WithName("SaveNotificationPreferences");

        notifications.MapGet("/unread-count", async (
            AppDbContext db,
            ICurrentTenant currentTenant,
            CancellationToken cancellationToken) =>
        {
            if (currentTenant.TenantId is not long tenantId || currentTenant.UserId is not long userId)
                return Results.BadRequest(new { error = "A valid tenant and user are required." });

            var count = await db.Notifications.CountAsync(
                item => item.TenantId == tenantId && item.UserId == userId &&
                        item.ReadAt == null && item.DismissedAt == null &&
                        !db.NotificationPreferences.IgnoreQueryFilters().Any(preference =>
                            preference.TenantId == tenantId &&
                            preference.UserId == userId &&
                            preference.EventType == item.EventType &&
                            !preference.InAppEnabled),
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

            return Results.Ok(new AuditHistoryDto(
                items,
                normalizedPage,
                pageSize,
                totalCount,
                totalPages,
                AuditActions.All));
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

    private sealed record NotificationPreferencesDto(
        IReadOnlyList<NotificationPreferenceDto> Events,
        bool EmailConfigured,
        string EmailConfigurationSource);

    private sealed record NotificationPreferenceDto(
        string EventType,
        string Category,
        string Name,
        string Description,
        bool InAppEnabled,
        bool EmailEnabled);

    private sealed record SaveNotificationPreferencesRequest(
        IReadOnlyList<SaveNotificationPreferenceRequest> Events);

    private sealed record SaveNotificationPreferenceRequest(
        string EventType,
        bool InAppEnabled,
        bool EmailEnabled);

    private sealed record AuditEntryDto(
        long Id,
        string Action,
        string EntityType,
        string? EntityId,
        long? ActorUserId,
        string ActorName,
        string DetailsJson,
        DateTimeOffset CreatedAt);

    private sealed record AuditHistoryDto(
        IReadOnlyList<AuditEntryDto> Items,
        int Page,
        int PageSize,
        int TotalCount,
        int TotalPages,
        IReadOnlyList<AuditActionDefinition> Actions);
}
