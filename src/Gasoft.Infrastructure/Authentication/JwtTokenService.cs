using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Gasoft.Application.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Gasoft.Infrastructure.Authentication;

public sealed class JwtTokenService(IOptions<JwtOptions> options) : ITokenService
{
    public string CreateToken(long userId, string email, long tenantId, string role)
    {
        var settings = options.Value;
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim("tenant_id", tenantId.ToString()),
            new Claim(ClaimTypes.Role, role)
        };
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            settings.Issuer,
            settings.Audience,
            claims,
            expires: DateTime.UtcNow.AddMinutes(settings.ExpiresInMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
