using Gasoft.Application.Abstractions;
using Microsoft.AspNetCore.DataProtection;

namespace Gasoft.Infrastructure.Authentication;

public sealed class EmailPasswordProtector : IEmailPasswordProtector
{
    private readonly IDataProtector protector;

    public EmailPasswordProtector(IDataProtectionProvider provider) =>
        protector = provider.CreateProtector("Gasoft.TenantEmailConfiguration.SmtpPassword.v1");

    public string Protect(string password) => protector.Protect(password);

    public string Unprotect(string protectedPassword) => protector.Unprotect(protectedPassword);
}
