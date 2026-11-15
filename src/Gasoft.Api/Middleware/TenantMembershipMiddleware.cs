using System.Security.Claims;
using Gasoft.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Gasoft.Api.Middleware;

public sealed class TenantMembershipMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IServiceScopeFactory scopeFactory)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            if (!long.TryParse(context.User.FindFirstValue("tenant_id"), out var tenantId) ||
                !long.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var role = await db.TenantUsers
                .Where(item => item.TenantId == tenantId && item.UserId == userId && item.IsActive)
                .Select(item => item.Role)
                .SingleOrDefaultAsync(context.RequestAborted);
            var tenantIsActive = await db.Tenants
                .IgnoreQueryFilters()
                .AnyAsync(item => item.Id == tenantId && item.Status == "active", context.RequestAborted);

            if (role is null || !tenantIsActive)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            if (context.User.Identity is ClaimsIdentity identity)
            {
                foreach (var roleClaim in identity.FindAll(ClaimTypes.Role).ToArray())
                    identity.RemoveClaim(roleClaim);
                identity.AddClaim(new Claim(ClaimTypes.Role, role));
            }
        }

        await next(context);
    }
}
