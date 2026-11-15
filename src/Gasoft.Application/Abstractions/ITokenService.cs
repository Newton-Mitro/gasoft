namespace Gasoft.Application.Abstractions;

public interface ITokenService
{
    string CreateToken(long userId, string email, long tenantId, string role);
}
