using System.Net.Mail;
using Gasoft.Application.Abstractions;
using Gasoft.Domain.Entities;
using Gasoft.Domain.Notifications;
using Gasoft.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Gasoft.Infrastructure.Authentication;

public sealed class AuthService(
    SignInManager<ApplicationUser> signInManager,
    AppDbContext dbContext,
    ITokenService tokenService,
    UserManager<ApplicationUser> userManager,
    IEmailSender emailSender,
    IOptions<EmailOptions> emailOptions,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<IReadOnlyList<AvailableWorkspace>?> GetAvailableWorkspacesAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        var user = await signInManager.UserManager.FindByEmailAsync(email.Trim());
        if (user is null ||
            !(await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true)).Succeeded)
        {
            if (user is not null)
            {
                var tenantIds = await dbContext.TenantUsers.IgnoreQueryFilters()
                    .Where(membership => membership.UserId == user.Id && membership.IsActive)
                    .Select(membership => membership.TenantId)
                    .ToListAsync(cancellationToken);
                foreach (var tenantId in tenantIds)
                    await RecordAuthenticationEventAsync(
                        tenantId, user.Id, "AuthenticationFailed", true, cancellationToken);
            }
            return null;
        }

        return await dbContext.TenantUsers
            .IgnoreQueryFilters()
            .Where(membership => membership.UserId == user.Id && membership.IsActive)
            .Join(
                dbContext.Tenants.IgnoreQueryFilters().Where(tenant => tenant.Status == "active"),
                membership => membership.TenantId,
                tenant => tenant.Id,
                (membership, tenant) => tenant)
            .Where(tenant => dbContext.Companies
                .IgnoreQueryFilters()
                .Any(company => company.TenantId == tenant.Id))
            .OrderBy(tenant => tenant.Name)
            .ThenBy(tenant => tenant.Id)
            .Select(tenant => new AvailableWorkspace(tenant.Id, tenant.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<AuthenticatedWorkspace?> AuthenticateAsync(
        string email,
        string password,
        long tenantId,
        CancellationToken cancellationToken)
    {
        var user = await signInManager.UserManager.FindByEmailAsync(email.Trim());
        if (user is null ||
            !(await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true)).Succeeded)
        {
            await RecordAuthenticationEventAsync(tenantId, user?.Id, "AuthenticationFailed", true, cancellationToken);
            return null;
        }

        var tenantMembership = await dbContext.TenantUsers
            .IgnoreQueryFilters()
            .Where(membership =>
                membership.UserId == user.Id &&
                membership.TenantId == tenantId &&
                membership.IsActive)
            .Join(
                dbContext.Tenants.IgnoreQueryFilters().Where(tenant => tenant.Status == "active"),
                membership => membership.TenantId,
                tenant => tenant.Id,
                (membership, tenant) => new { membership.Role, tenant.Slug })
            .SingleOrDefaultAsync(cancellationToken);
        if (tenantMembership is null)
        {
            await RecordAuthenticationEventAsync(tenantId, user.Id, "AuthenticationFailed", true, cancellationToken);
            return null;
        }

        var companyId = await dbContext.Companies
            .IgnoreQueryFilters()
            .Where(company => company.TenantId == tenantId)
            .OrderBy(company => company.Id)
            .Select(company => (long?)company.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (companyId is null)
        {
            await RecordAuthenticationEventAsync(tenantId, user.Id, "AuthenticationFailed", true, cancellationToken);
            return null;
        }

        await RecordAuthenticationEventAsync(tenantId, user.Id, "AuthenticationSucceeded", false, cancellationToken);

        var accessToken = tokenService.CreateToken(
            user.Id,
            user.Email ?? email,
            tenantId,
            tenantMembership.Role);
        return new AuthenticatedWorkspace(accessToken, tenantId, companyId.Value, tenantMembership.Slug);
    }

    public async Task<bool> RequestPasswordResetAsync(
        string? email,
        CancellationToken cancellationToken)
    {
        if (!emailSender.IsConfigured) return false;

        var normalizedEmail = email?.Trim() ?? string.Empty;
        var user = await userManager.FindByEmailAsync(normalizedEmail);
        if (user is not null)
        {
            var isActiveMember = await dbContext.TenantUsers
                .IgnoreQueryFilters()
                .Where(membership => membership.UserId == user.Id && membership.IsActive)
                .Join(
                    dbContext.Tenants.IgnoreQueryFilters().Where(tenant => tenant.Status == "active"),
                    membership => membership.TenantId,
                    tenant => tenant.Id,
                    (membership, tenant) => tenant.Id)
                .AnyAsync(
                    cancellationToken);

            if (isActiveMember)
            {
                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                var resetLink = $"{emailOptions.Value.FrontendBaseUrl.TrimEnd('/')}/reset-password" +
                    $"?email={Uri.EscapeDataString(user.Email ?? normalizedEmail)}&token={Uri.EscapeDataString(token)}";
                try
                {
                    await emailSender.SendPasswordResetAsync(
                        user.Email ?? normalizedEmail,
                        resetLink,
                        cancellationToken);
                }
                catch (SmtpException exception)
                {
                    logger.LogError(exception, "Could not deliver a password reset email.");
                }
            }
        }

        return true;
    }

    public async Task<IReadOnlyDictionary<string, string[]>?> ResetPasswordAsync(
        string? email,
        string? token,
        string? newPassword,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(email?.Trim() ?? string.Empty);
        if (user is null)
        {
            return new Dictionary<string, string[]>
            {
                ["token"] = ["This reset link is invalid or has expired. Request a new one."]
            };
        }

        var result = await userManager.ResetPasswordAsync(
            user,
            token ?? string.Empty,
            newPassword ?? string.Empty);
        if (result.Succeeded)
        {
            var memberships = await dbContext.TenantUsers.IgnoreQueryFilters()
                .Where(membership => membership.UserId == user.Id && membership.IsActive)
                .Select(membership => membership.TenantId)
                .ToListAsync(cancellationToken);
            foreach (var tenantId in memberships)
            {
                dbContext.AuditLogs.Add(new AuditLog
                {
                    TenantId = tenantId,
                    ActorUserId = user.Id,
                    Action = "PasswordReset",
                    EntityType = "Authentication",
                    DetailsJson = "{\"result\":\"succeeded\"}",
                    CreatedAt = DateTimeOffset.UtcNow
                });
                dbContext.Notifications.Add(new AppNotification
                {
                    TenantId = tenantId,
                    UserId = user.Id,
                    Title = "Password changed",
                    Message = "Your account password was changed.",
                    Category = "security",
                    EventType = NotificationEvents.PasswordChanged,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
            await dbContext.SaveChangesAsync(cancellationToken);
            return null;
        }

        return result.Errors
            .Select(error => error.Code == "InvalidToken"
                ? ("token", "This reset link is invalid or has expired. Request a new one.")
                : ("newPassword", error.Description))
            .GroupBy(error => error.Item1)
            .ToDictionary(group => group.Key, group => group.Select(error => error.Item2).ToArray());
    }

    private async Task RecordAuthenticationEventAsync(
        long tenantId,
        long? userId,
        string action,
        bool notifyAdministrators,
        CancellationToken cancellationToken)
    {
        var tenantExists = await dbContext.Tenants.IgnoreQueryFilters()
            .AnyAsync(tenant => tenant.Id == tenantId, cancellationToken);
        if (!tenantExists) return;
        if (action == "AuthenticationFailed" &&
            (!userId.HasValue || !await dbContext.TenantUsers.IgnoreQueryFilters()
                .AnyAsync(membership => membership.TenantId == tenantId &&
                    membership.UserId == userId.Value && membership.IsActive, cancellationToken)))
            return;

        dbContext.AuditLogs.Add(new AuditLog
        {
            TenantId = tenantId,
            ActorUserId = action == "AuthenticationSucceeded" ? userId : null,
            Action = action == "AuthenticationFailed"
                ? NotificationEvents.SignInAttemptRejected
                : action,
            EntityType = "Authentication",
            DetailsJson = action == "AuthenticationSucceeded"
                ? "{\"result\":\"succeeded\"}"
                : "{\"result\":\"rejected\"}",
            CreatedAt = DateTimeOffset.UtcNow
        });

        if (notifyAdministrators)
        {
            var administratorIds = await dbContext.TenantUsers.IgnoreQueryFilters()
                .Where(membership => membership.TenantId == tenantId &&
                    membership.IsActive &&
                    (membership.Role == "Owner" || membership.Role == "Admin"))
                .Select(membership => membership.UserId)
                .ToListAsync(cancellationToken);
            foreach (var administratorId in administratorIds)
            {
                dbContext.Notifications.Add(new AppNotification
                {
                    TenantId = tenantId,
                    UserId = administratorId,
                    Title = "Sign-in attempt rejected",
                    Message = "A sign-in attempt for this workspace was rejected.",
                    Category = "security",
                    EventType = NotificationEvents.SignInAttemptRejected,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
