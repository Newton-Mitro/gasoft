using Gasoft.Application.Consolidation;

namespace Gasoft.Application.Abstractions;

public interface IConsolidationService
{
    Task<IReadOnlyList<ConsolidationGroupResponse>> GetGroupsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<ConsolidationCompanyResponse>> GetCompaniesAsync(CancellationToken cancellationToken);
    Task<ConsolidationGroupResponse?> CreateGroupAsync(
        ConsolidationGroupRequest request,
        CancellationToken cancellationToken);
    Task<ConsolidationGroupResponse?> UpdateGroupAsync(
        long groupId,
        ConsolidationGroupRequest request,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<ConsolidationRunSummaryDto>?> GetRunsAsync(
        long groupId,
        CancellationToken cancellationToken);
    Task<ConsolidationRunResponse?> CreateRunAsync(
        long groupId,
        CreateConsolidationRunRequest request,
        CancellationToken cancellationToken);
    Task<ConsolidationRunResponse?> GetRunAsync(
        long groupId,
        long runId,
        CancellationToken cancellationToken);
    Task<bool> SaveEliminationsAsync(
        long groupId,
        long runId,
        SaveEliminationsRequest request,
        CancellationToken cancellationToken);
    Task<FinalizedConsolidationRunDto?> FinalizeRunAsync(
        long groupId,
        long runId,
        CancellationToken cancellationToken);
}
