using RestaurantAPI.Models.DTOs;

namespace RestaurantAPI.ServiceInterfaces;

public interface IInventoryService
{
    Task<InventoryManagementResponseDto> GetInventoryItems(string? search,bool lowStockOnly,int pageNumber,int pageSize);
    Task UpdateInventoryQuantity(int inventoryItemId,decimal quantity);
     Task UpdateInventoryThreshold(int inventoryItemId,decimal thresholdQuantity);
    Task AddInventoryItem(AddInventoryItemDto request);
    Task DeleteInventoryItem(int inventoryItemId);
}
