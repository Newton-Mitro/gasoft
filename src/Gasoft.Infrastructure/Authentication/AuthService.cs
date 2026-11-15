using Gasoft.Application.Abstractions;
using Gasoft.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Gasoft.Infrastructure.Authentication;

public sealed class AuthService(
    SignInManager<ApplicationUser> signInManager,
    AppDbContext dbContext,
    ITokenService tokenService) : IAuthService
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
}
