using System.Net;
using Gasoft.Application.Abstractions;
using Gasoft.Domain.Entities;
using Gasoft.Infrastructure.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Gasoft.Infrastructure.Persistence;

public sealed class NotificationEmailOutboxProcessor(
    AppDbContext db,
    IEmailPasswordProtector emailPasswordProtector,
    ISmtpEmailTransport smtpEmailTransport,
    IOptions<EmailOptions> emailOptions,
    ILogger<NotificationEmailOutboxProcessor> logger)
{
    private const int BatchSize = 25;
    private const int MaximumAttempts = 8;
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(5);

    public async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        for (var index = 0; index < BatchSize; index++)
        {
            var message = await ClaimNextAsync(cancellationToken);
            if (message is null) return;

            try
            {
                await SendIfEnabledAsync(message, cancellationToken);
                message.Status = "Sent";
                message.SentAt = DateTimeOffset.UtcNow;
                message.LastError = null;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                var now = DateTimeOffset.UtcNow;
                message.LastError = TruncateError(exception);
                if (message.AttemptCount >= MaximumAttempts)
                {
                    message.Status = "Failed";
                    logger.LogError(
                        exception,
                        "Email notification outbox message {OutboxId} failed after {AttemptCount} attempts.",
                        message.Id,
                        message.AttemptCount);
                }
                else
                {
                    message.Status = "Pending";
                    message.NextAttemptAt = now + GetRetryDelay(message.AttemptCount);
                    logger.LogWarning(
                        exception,
                        "Email notification outbox message {OutboxId} failed on attempt {AttemptCount}; retry scheduled for {NextAttemptAt}.",
                        message.Id,
                        message.AttemptCount,
                        message.NextAttemptAt);
                }
            }

            message.LeaseExpiresAt = null;
            await db.SaveChangesAsync(cancellationToken);
            db.ChangeTracker.Clear();
        }
    }

    private async Task<NotificationEmailOutbox?> ClaimNextAsync(CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var messages = await db.NotificationEmailOutbox
            .FromSqlInterpolated($"""
                SELECT * FROM "NotificationEmailOutbox"
                WHERE ("Status" = 'Pending' AND "NextAttemptAt" <= {now})
                   OR ("Status" = 'Processing' AND "LeaseExpiresAt" <= {now})
                ORDER BY "NextAttemptAt", "Id"
                LIMIT 1
                FOR UPDATE SKIP LOCKED
                """)
            .IgnoreQueryFilters()
            .ToListAsync(cancellationToken);
        var message = messages.SingleOrDefault();

        if (message is not null)
        {
            message.Status = "Processing";
            message.AttemptCount++;
            message.LeaseExpiresAt = now + LeaseDuration;
            await db.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return message;
    }

    private async Task SendIfEnabledAsync(
        NotificationEmailOutbox message,
        CancellationToken cancellationToken)
    {
        var preference = await db.NotificationPreferences.IgnoreQueryFilters()
            .SingleOrDefaultAsync(item =>
                item.TenantId == message.TenantId &&
                item.UserId == message.UserId &&
                item.EventType == message.EventType,
                cancellationToken);
        if (preference?.EmailEnabled != true) return;

        var configuration = await db.TenantEmailConfigurations.IgnoreQueryFilters()
            .SingleOrDefaultAsync(item => item.TenantId == message.TenantId, cancellationToken);
        var tenantSmtpConfigured = configuration is not null &&
            !string.IsNullOrWhiteSpace(configuration.SmtpHost) &&
            configuration.SmtpPort is > 0 and <= 65535 &&
            !string.IsNullOrWhiteSpace(configuration.FromEmail) &&
            (string.IsNullOrWhiteSpace(configuration.SmtpUsername) ||
             !string.IsNullOrEmpty(configuration.ProtectedSmtpPassword));
        var useGlobalSmtp = !tenantSmtpConfigured && emailOptions.Value.IsSmtpConfigured;
        if (!tenantSmtpConfigured && !useGlobalSmtp)
        {
            logger.LogWarning(
                "Email notification for event {EventType} was not sent because neither tenant nor global SMTP is configured for tenant {TenantId}.",
                message.EventType,
                message.TenantId);
            return;
        }

        var email = await db.Users.IgnoreQueryFilters()
            .Where(user => user.Id == message.UserId)
            .Select(user => user.Email)
            .SingleOrDefaultAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(email))
        {
            logger.LogWarning(
                "Email notification for user {UserId} and event {EventType} was not sent because no email address is available.",
                message.UserId,
                message.EventType);
            return;
        }

        var deliverySettings = tenantSmtpConfigured && configuration is not null
            ? new SmtpDeliverySettings(
                configuration.SmtpHost,
                configuration.SmtpPort,
                configuration.SmtpUsername,
                configuration.ProtectedSmtpPassword is null
                    ? null
                    : emailPasswordProtector.Unprotect(configuration.ProtectedSmtpPassword),
                configuration.FromEmail,
                configuration.FromName)
            : new SmtpDeliverySettings(
                emailOptions.Value.SmtpHost,
                emailOptions.Value.SmtpPort,
                emailOptions.Value.SmtpUsername,
                emailOptions.Value.SmtpPassword,
                emailOptions.Value.FromAddress,
                emailOptions.Value.FromName);

        await smtpEmailTransport.SendAsync(
            deliverySettings,
            email,
            message.Title,
            $"<p>{WebUtility.HtmlEncode(message.Message)}</p>",
            message.Message,
            cancellationToken);
    }

    private static TimeSpan GetRetryDelay(int attemptCount) =>
        TimeSpan.FromSeconds(Math.Min(3600, 15 * (1 << Math.Min(attemptCount, 8))));

    private static string TruncateError(Exception exception)
    {
        var value = $"{exception.GetType().Name}: {exception.Message}";
        return value.Length > 2000 ? value[..2000] : value;
    }
}
