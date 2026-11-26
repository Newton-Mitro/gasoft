using Gasoft.Application.Inventory;

namespace Gasoft.Infrastructure.Persistence;

public sealed partial class InventoryService
{
    public Task<StockMovementDto> ReceiveAsync(
        long companyId,
        ReceiveStockRequest request,
        CancellationToken cancellationToken) =>
        stockMovements.ReceiveAsync(companyId, request, cancellationToken);

    public Task<StockMovementDto> IssueAsync(
        long companyId,
        IssueStockRequest request,
        CancellationToken cancellationToken) =>
        stockMovements.IssueAsync(companyId, request, cancellationToken);
}
