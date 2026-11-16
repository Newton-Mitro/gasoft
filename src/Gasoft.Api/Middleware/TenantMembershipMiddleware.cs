using System.Security.Claims;
using Gasoft.Application.Abstractions;

namespace Gasoft.Api.Middleware;

public sealed class TenantMembershipMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ITenantUserService tenantUserService)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            if (!long.TryParse(context.User.FindFirstValue("tenant_id"), out var tenantId) ||
                !long.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            var role = await tenantUserService.GetActiveMembershipRoleAsync(
                tenantId, userId, context.RequestAborted);
            if (role is null)
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
