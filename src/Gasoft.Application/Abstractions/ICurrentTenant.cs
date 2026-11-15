namespace Gasoft.Application.Abstractions;

public interface ICurrentTenant
{
    long? TenantId { get; }
}
