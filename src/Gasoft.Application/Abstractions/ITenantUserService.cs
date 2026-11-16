using Gasoft.Application.TenantUsers;
using Gasoft.Application.Common;

namespace Gasoft.Application.Abstractions;

public interface ITenantUserService
{
    Task<string?> GetActiveMembershipRoleAsync(long tenantId, long userId, CancellationToken cancellationToken);
    Task<TenantManagerDto?> GetManagerAsync(long tenantId, long userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<TenantUserDto>> GetUsersAsync(long tenantId, CancellationToken cancellationToken);
    Task<PagedResultDto<TenantUserDto>> GetUsersPageAsync(
        long tenantId, int page, int pageSize, CancellationToken cancellationToken);
    Task<TenantUserDto?> GetUserAsync(long tenantId, long userId, CancellationToken cancellationToken);
    Task<TenantUserDto> CreateAsync(
        TenantManagerDto manager,
        CreateTenantUserRequest request,
        CancellationToken cancellationToken);
    Task<TenantUserDto> AddExistingAsync(
        TenantManagerDto manager,
        AddExistingTenantUserRequest request,
        CancellationToken cancellationToken);
    Task<TenantUserDto?> UpdateAsync(
        TenantManagerDto manager,
        long userId,
        UpdateTenantUserRequest request,
        CancellationToken cancellationToken);
}
