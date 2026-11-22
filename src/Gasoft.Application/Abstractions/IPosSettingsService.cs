using Gasoft.Application.Pos;

namespace Gasoft.Application.Abstractions;

public interface IPosSettingsService
{
    Task<PosSettingsDto?> GetAsync(long companyId, CancellationToken cancellationToken);
    Task<PosSettingsDto?> SaveAsync(
        long companyId,
        SavePosSettingsRequest request,
        CancellationToken cancellationToken);
}
