using Gasoft.Application.FixedAssets;
namespace Gasoft.Application.Abstractions;

public interface IFixedAssetService
{
    Task<FixedAssetWorkspaceDto?> GetWorkspaceAsync(long companyId, CancellationToken cancellationToken);
    Task<PagedFixedAssetWorkspaceDto?> GetWorkspacePageAsync(
        long companyId, int page, int pageSize, CancellationToken cancellationToken);
    Task<CreatedFixedAssetDto?> CreateAsync(
        long companyId,
        CreateFixedAssetRequest request,
        CancellationToken cancellationToken);
    Task<FixedAssetDepreciationResultDto> DepreciateAsync(
        long companyId,
        long assetId,
        DepreciateFixedAssetRequest request,
        CancellationToken cancellationToken);
    Task<FixedAssetDisposalResultDto> DisposeAsync(
        long companyId,
        long assetId,
        DisposeFixedAssetRequest request,
        CancellationToken cancellationToken);
}
