using RestaurantAPI.Models;

namespace RestaurantAPI.RepositoryInterfaces;

public interface IInventoryItemRepository:IRepository<int,InventoryItem>
{
    IQueryable<InventoryItem> GetInventoryQuery();
    Task<InventoryItem?> GetByName(string itemName);
}
