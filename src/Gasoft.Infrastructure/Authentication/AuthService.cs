using System.Net.Mail;
using Gasoft.Application.Abstractions;
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
        if (tenantMembership is null) return null;

        var companyId = await dbContext.Companies
            .IgnoreQueryFilters()
            .Where(company => company.TenantId == tenantId)
            .OrderBy(company => company.Id)
            .Select(company => (long?)company.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (companyId is null) return null;

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
        if (result.Succeeded) return null;

        return result.Errors
            .Select(error => error.Code == "InvalidToken"
                ? ("token", "This reset link is invalid or has expired. Request a new one.")
                : ("newPassword", error.Description))
            .GroupBy(error => error.Item1)
            .ToDictionary(group => group.Key, group => group.Select(error => error.Item2).ToArray());
    }
}
