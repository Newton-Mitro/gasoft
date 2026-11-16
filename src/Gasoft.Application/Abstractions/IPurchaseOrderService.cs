using Gasoft.Application.PurchaseOrders;
using Gasoft.Application.Common;

namespace Gasoft.Application.Abstractions;

public interface IPurchaseOrderService
{
    Task<IReadOnlyList<PurchaseOrderDto>?> GetOrdersAsync(long companyId, CancellationToken cancellationToken);
    Task<PagedResultDto<PurchaseOrderDto>?> GetOrdersPageAsync(
        long companyId, int page, int pageSize, CancellationToken cancellationToken);
    Task<PurchaseOrderDto?> GetOrderAsync(long companyId, long orderId, CancellationToken cancellationToken);
    Task<PurchaseOrderDto?> CreateAsync(long companyId, PurchaseOrderRequest request, CancellationToken cancellationToken);
    Task<PurchaseOrderDto?> UpdateAsync(long companyId, long orderId, PurchaseOrderRequest request, CancellationToken cancellationToken);
    Task<PurchaseOrderDto?> PlaceOrderAsync(
        long companyId,
        long orderId,
        PurchaseOrderRevisionRequest request,
        CancellationToken cancellationToken);
    Task<PurchaseOrderStatusDto?> CancelAsync(
        long companyId,
        long orderId,
        PurchaseOrderRevisionRequest request,
        CancellationToken cancellationToken);
    Task<PurchaseOrderReceiptDto?> ReceiveLineAsync(
        long companyId,
        long orderId,
        long lineId,
        ReceivePurchaseOrderLineRequest request,
        CancellationToken cancellationToken);
}
