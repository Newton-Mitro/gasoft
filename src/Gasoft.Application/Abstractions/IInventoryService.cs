using Gasoft.Application.Inventory;
using Gasoft.Application.Common;

namespace Gasoft.Application.Abstractions;

public interface IInventoryService
{
    Task<IReadOnlyList<UnitOfMeasureDto>> GetUnitsOfMeasureAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<ItemCategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken);
    Task<PagedResultDto<ItemCategoryDto>> GetCategoriesPageAsync(int page, int pageSize, CancellationToken cancellationToken);
    Task<ItemCategoryDto?> GetCategoryAsync(long categoryId, CancellationToken cancellationToken);
    Task<ItemCategoryDto> CreateCategoryAsync(CreateItemCategoryRequest request, CancellationToken cancellationToken);
    Task<ItemCategoryDto?> UpdateCategoryAsync(long categoryId, UpdateItemCategoryRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<ItemDto>> GetItemsAsync(CancellationToken cancellationToken);
    Task<PagedResultDto<ItemDto>> GetItemsPageAsync(int page, int pageSize, CancellationToken cancellationToken);
    Task<ItemDto?> GetItemAsync(long itemId, CancellationToken cancellationToken);
    Task<ItemDto> CreateItemAsync(CreateItemRequest request, CancellationToken cancellationToken);
    Task<ItemDto?> UpdateItemAsync(long itemId, UpdateItemRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<WarehouseDto>> GetWarehousesAsync(long companyId, CancellationToken cancellationToken);
    Task<PagedResultDto<WarehouseDto>> GetWarehousesPageAsync(long companyId, int page, int pageSize, CancellationToken cancellationToken);
    Task<WarehouseDto?> GetWarehouseAsync(long companyId, long warehouseId, CancellationToken cancellationToken);
    Task<WarehouseDto> CreateWarehouseAsync(long companyId, CreateWarehouseRequest request, CancellationToken cancellationToken);
    Task<WarehouseDto?> UpdateWarehouseAsync(long companyId, long warehouseId, UpdateWarehouseRequest request, CancellationToken cancellationToken);
    Task<WarehouseLocationDto> CreateLocationAsync(long companyId, long warehouseId, CreateWarehouseLocationRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<StockBalanceDto>> GetStockBalancesAsync(long companyId, CancellationToken cancellationToken);
    Task<PagedResultDto<StockBalanceDto>> GetStockBalancesPageAsync(
        long companyId, int page, int pageSize, CancellationToken cancellationToken);
    Task<IReadOnlyList<StockMovementDto>> GetStockMovementsAsync(long companyId, CancellationToken cancellationToken);
    Task<PagedResultDto<StockMovementDto>> GetStockMovementsPageAsync(
        long companyId, int page, int pageSize, CancellationToken cancellationToken);
    Task<StockMovementDto> ReceiveAsync(long companyId, ReceiveStockRequest request, CancellationToken cancellationToken);
    Task<StockMovementDto> IssueAsync(long companyId, IssueStockRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<InventoryCountDto>> GetCountsAsync(long companyId, CancellationToken cancellationToken);
    Task<PagedResultDto<InventoryCountDto>> GetCountsPageAsync(
        long companyId, int page, int pageSize, CancellationToken cancellationToken);
    Task<InventoryCountDto?> GetCountAsync(long companyId, long countId, CancellationToken cancellationToken);
    Task<InventoryCountDto> CreateCountAsync(long companyId, CreateInventoryCountRequest request, CancellationToken cancellationToken);
    Task<InventoryCountDto?> UpdateCountAsync(long companyId, long countId, UpdateInventoryCountRequest request, CancellationToken cancellationToken);
    Task<InventoryCountDto?> PostCountAsync(long companyId, long countId, PostInventoryCountRequest request, CancellationToken cancellationToken);
}
