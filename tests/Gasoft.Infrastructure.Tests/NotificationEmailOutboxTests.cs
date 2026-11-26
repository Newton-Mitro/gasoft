using Gasoft.Application.Abstractions;
using Gasoft.Domain.Entities;
using Gasoft.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Gasoft.Infrastructure.Tests;

public sealed class NotificationEmailOutboxTests
{
    [Fact]
    public async Task SavingNotificationQueuesEmailInSameContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var db = new AppDbContext(options, new CurrentTenantStub(12, 34));
        var notification = new AppNotification
        {
            TenantId = 12,
            UserId = 34,
            Title = "Invoice posted",
            Message = "An invoice was posted.",
            Category = "workflow",
            EventType = "invoice.posted",
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Notifications.Add(notification);

        await db.SaveChangesAsync();

        var outboxMessage = await db.NotificationEmailOutbox
            .IgnoreQueryFilters()
            .SingleAsync();
        Assert.Equal(notification.Id, outboxMessage.NotificationId);
        Assert.Equal(notification.TenantId, outboxMessage.TenantId);
        Assert.Equal(notification.UserId, outboxMessage.UserId);
        Assert.Equal(notification.EventType, outboxMessage.EventType);
        Assert.Equal(notification.Title, outboxMessage.Title);
        Assert.Equal(notification.Message, outboxMessage.Message);
        Assert.Equal("Pending", outboxMessage.Status);
    }

    private sealed class CurrentTenantStub(long? tenantId, long? userId) : ICurrentTenant
    {
        public long? TenantId { get; } = tenantId;
        public long? UserId { get; } = userId;
    }
}
