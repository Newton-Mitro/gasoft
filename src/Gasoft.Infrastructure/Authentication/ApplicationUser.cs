using Microsoft.AspNetCore.Identity;

namespace Gasoft.Infrastructure.Authentication;

public sealed class ApplicationUser : IdentityUser<long>
{
    public string? DisplayName { get; set; }
}
