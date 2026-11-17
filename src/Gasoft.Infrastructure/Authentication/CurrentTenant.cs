using System.Security.Claims;
using Gasoft.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Gasoft.Infrastructure.Authentication;

public sealed class CurrentTenant(IHttpContextAccessor httpContextAccessor) : ICurrentTenant
{
    public long? TenantId
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User.FindFirstValue("tenant_id");
            return long.TryParse(value, out var tenantId) ? tenantId : null;
        }
    }

    public long? UserId
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return long.TryParse(value, out var userId) ? userId : null;
        }
    }
}
