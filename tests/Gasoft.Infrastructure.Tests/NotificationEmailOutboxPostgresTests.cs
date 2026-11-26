using Gasoft.Application.Abstractions;
using Gasoft.Domain.Entities;
using Gasoft.Infrastructure.Authentication;
using Gasoft.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Gasoft.Infrastructure.Tests;

public sealed class NotificationEmailOutboxPostgresTests
{
    [PostgreSqlFact]
    public async Task ProcessBatchClaimsPendingRowsAndRecoversExpiredLeases()
    {
        await using var database = await PostgresTestDatabase.CreateAsync();
        await using (var seed = database.CreateContext(new CurrentTenantStub(12, 34)))
        {
            seed.Notifications.AddRange(
                CreateNotification(34, "invoice.posted"),
                CreateNotification(34, "payment.received"));
            await seed.SaveChangesAsync();

            var expiredLeaseId = await seed.NotificationEmailOutbox
                .IgnoreQueryFilters()
                .OrderBy(item => item.Id)
                .Select(item => item.Id)
                .FirstAsync();
            await seed.NotificationEmailOutbox.IgnoreQueryFilters()
                .Where(item => item.Id == expiredLeaseId)
                .ExecuteUpdateAsync(updates => updates
                    .SetProperty(item => item.Status, "Processing")
                    .SetProperty(item => item.AttemptCount, 1)
                    .SetProperty(item => item.LeaseExpiresAt, DateTimeOffset.UtcNow.AddMinutes(-1)));
        }

        var transport = new RecordingSmtpTransport();
        await using var firstDb = database.CreateContext(new CurrentTenantStub(null, null));
        await using var secondDb = database.CreateContext(new CurrentTenantStub(null, null));
        var firstProcessor = CreateProcessor(firstDb, transport, new EmailOptions());
        var secondProcessor = CreateProcessor(secondDb, transport, new EmailOptions());

        await Task.WhenAll(
            firstProcessor.ProcessBatchAsync(CancellationToken.None),
            secondProcessor.ProcessBatchAsync(CancellationToken.None));

        await using var verification = database.CreateContext(new CurrentTenantStub(12, 34));
        var messages = await verification.NotificationEmailOutbox
            .IgnoreQueryFilters()
            .OrderBy(item => item.Id)
            .ToListAsync();
        Assert.Equal(2, messages.Count);
        Assert.All(messages, message => Assert.Equal("Sent", message.Status));
        Assert.Equal([2, 1], messages.Select(message => message.AttemptCount));
        Assert.All(messages, message => Assert.Null(message.LeaseExpiresAt));
        Assert.Equal(0, transport.CallCount);
    }

    [PostgreSqlFact]
    public async Task ProcessBatchPersistsRetryStateWhenSmtpDeliveryFails()
    {
        await using var database = await PostgresTestDatabase.CreateAsync();
        await using (var seed = database.CreateContext(new CurrentTenantStub(12, 34)))
        {
            seed.Users.Add(new ApplicationUser
            {
                Id = 34,
                UserName = "outbox-test@example.test",
                NormalizedUserName = "OUTBOX-TEST@EXAMPLE.TEST",
                Email = "outbox-test@example.test",
                NormalizedEmail = "OUTBOX-TEST@EXAMPLE.TEST",
                EmailConfirmed = true
            });
            seed.NotificationPreferences.Add(new NotificationPreference
            {
                TenantId = 12,
                UserId = 34,
                EventType = "invoice.posted",
                InAppEnabled = true,
                EmailEnabled = true
            });
            seed.Notifications.Add(CreateNotification(34, "invoice.posted"));
            await seed.SaveChangesAsync();
        }

        var transport = new RecordingSmtpTransport(new InvalidOperationException("Temporary SMTP failure."));
        await using var processingContext = database.CreateContext(new CurrentTenantStub(null, null));
        var startedAt = DateTimeOffset.UtcNow;
        var options = new EmailOptions
        {
            SmtpHost = "smtp.example.test",
            SmtpPort = 2525,
            FromAddress = "noreply@example.test",
            FromName = "Gasoft test"
        };
        await CreateProcessor(processingContext, transport, options)
            .ProcessBatchAsync(CancellationToken.None);

        await using var verification = database.CreateContext(new CurrentTenantStub(12, 34));
        var message = await verification.NotificationEmailOutbox
            .IgnoreQueryFilters()
            .SingleAsync();
        Assert.Equal("Pending", message.Status);
        Assert.Equal(1, message.AttemptCount);
        Assert.Equal("InvalidOperationException: Temporary SMTP failure.", message.LastError);
        Assert.InRange(
            message.NextAttemptAt,
            startedAt.AddSeconds(29),
            DateTimeOffset.UtcNow.AddSeconds(35));
        Assert.Null(message.LeaseExpiresAt);
        Assert.Equal(1, transport.CallCount);
    }

    private static AppNotification CreateNotification(long userId, string eventType) =>
        new()
        {
            TenantId = 12,
            UserId = userId,
            Title = "Outbox integration test",
            Message = "Notification message",
            Category = "workflow",
            EventType = eventType,
            CreatedAt = DateTimeOffset.UtcNow
        };

    private static NotificationEmailOutboxProcessor CreateProcessor(
        AppDbContext db,
        RecordingSmtpTransport transport,
        EmailOptions options) =>
        new(
            db,
            new PassThroughPasswordProtector(),
            transport,
            Options.Create(options),
            NullLogger<NotificationEmailOutboxProcessor>.Instance);

    private sealed class RecordingSmtpTransport(Exception? failure = null) : ISmtpEmailTransport
    {
        public int CallCount { get; private set; }

        public Task SendAsync(
            SmtpDeliverySettings settings,
            string email,
            string subject,
            string htmlBody,
            string textBody,
            CancellationToken cancellationToken)
        {
            CallCount++;
            if (failure is not null) throw failure;
            return Task.CompletedTask;
        }
    }

    private sealed class PassThroughPasswordProtector : IEmailPasswordProtector
    {
        public string Protect(string password) => password;
        public string Unprotect(string protectedPassword) => protectedPassword;
    }

    private sealed class CurrentTenantStub(long? tenantId, long? userId) : ICurrentTenant
    {
        public long? TenantId { get; } = tenantId;
        public long? UserId { get; } = userId;
    }

    private sealed class PostgresTestDatabase(string adminConnectionString, string schema)
        : IAsyncDisposable
    {
        private readonly string schemaConnectionString = new NpgsqlConnectionStringBuilder(adminConnectionString)
        {
            SearchPath = schema
        }.ConnectionString;

        public static async Task<PostgresTestDatabase> CreateAsync()
        {
            var connectionString = Environment.GetEnvironmentVariable("GASOFT_TEST_POSTGRES")
                ?? throw new InvalidOperationException(
                    "PostgreSQL integration tests require the GASOFT_TEST_POSTGRES connection string.");
            var schema = $"gasoft_test_{Guid.NewGuid():N}";
            var database = new PostgresTestDatabase(connectionString, schema);
            await database.CreateSchemaAsync();
            try
            {
                await using var context = database.CreateContext(new CurrentTenantStub(null, null));
                await context.Database.MigrateAsync();
                return database;
            }
            catch
            {
                await database.DropSchemaAsync();
                throw;
            }
        }

        public AppDbContext CreateContext(ICurrentTenant currentTenant)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(schemaConnectionString)
                .Options;
            return new AppDbContext(options, currentTenant);
        }

        public async ValueTask DisposeAsync() => await DropSchemaAsync();

        private async Task CreateSchemaAsync()
        {
            await using var connection = new NpgsqlConnection(adminConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"CREATE SCHEMA \"{schema}\"";
            await command.ExecuteNonQueryAsync();
        }

        private async Task DropSchemaAsync()
        {
            await using var connection = new NpgsqlConnection(adminConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"DROP SCHEMA IF EXISTS \"{schema}\" CASCADE";
            await command.ExecuteNonQueryAsync();
        }
    }

    private sealed class PostgreSqlFactAttribute : FactAttribute
    {
        public PostgreSqlFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GASOFT_TEST_POSTGRES")))
            {
                Skip = "Set GASOFT_TEST_POSTGRES to run PostgreSQL integration tests.";
            }
        }
    }
}
