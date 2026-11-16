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
    public async Task<AuthenticatedWorkspace?> AuthenticateAsync(
        string email,
        string password,
        string workspaceSlug,
        CancellationToken cancellationToken)
    {
        var user = await signInManager.UserManager.FindByEmailAsync(email);
        if (user is null ||
            !(await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true)).Succeeded)
        {
            return null;
        }

        var normalizedSlug = workspaceSlug.Trim().ToLowerInvariant();
        var tenantId = await dbContext.Tenants
            .IgnoreQueryFilters()
            .Where(tenant => tenant.Slug == normalizedSlug && tenant.Status == "active")
            .Select(tenant => (long?)tenant.Id)
            .SingleOrDefaultAsync(cancellationToken);
        if (tenantId is null) return null;

        var membershipRole = await dbContext.TenantUsers
            .IgnoreQueryFilters()
            .Where(
                membership => membership.UserId == user.Id &&
                              membership.TenantId == tenantId.Value &&
                              membership.IsActive)
            .Select(membership => membership.Role)
            .SingleOrDefaultAsync(cancellationToken);
        if (membershipRole is null) return null;

        var companyId = await dbContext.Companies
            .IgnoreQueryFilters()
            .Where(company => company.TenantId == tenantId.Value)
            .OrderBy(company => company.Id)
            .Select(company => (long?)company.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (companyId is null) return null;

        var accessToken = tokenService.CreateToken(
            user.Id,
            user.Email ?? email,
            tenantId.Value,
            membershipRole);
        return new AuthenticatedWorkspace(accessToken, tenantId.Value, companyId.Value);
    }

    public async Task<bool> RequestPasswordResetAsync(
        string? email,
        string? workspaceSlug,
        CancellationToken cancellationToken)
    {
        if (!emailSender.IsConfigured) return false;

        var normalizedEmail = email?.Trim() ?? string.Empty;
        var slug = workspaceSlug?.Trim().ToLowerInvariant() ?? string.Empty;
        var tenantId = await dbContext.Tenants
            .IgnoreQueryFilters()
            .Where(tenant => tenant.Slug == slug && tenant.Status == "active")
            .Select(tenant => (long?)tenant.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (tenantId is not null)
        {
            var user = await userManager.FindByEmailAsync(normalizedEmail);
            var isActiveMember = user is not null && await dbContext.TenantUsers
                .IgnoreQueryFilters()
                .AnyAsync(membership =>
                        membership.TenantId == tenantId &&
                        membership.UserId == user.Id &&
                        membership.IsActive,
                    cancellationToken);

            if (user is not null && isActiveMember)
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
