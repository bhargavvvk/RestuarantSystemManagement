using Microsoft.EntityFrameworkCore;
using RestaurantAPI.Contexts;
using RestaurantAPI.Models;
using RestaurantAPI.RepositoryInterfaces;

namespace RestaurantAPI.Repositories;

public class InventoryItemRepository:AbstractRepository<int,InventoryItem>,IInventoryItemRepository
{
    public InventoryItemRepository(RestaurantContext context) : base(context)
    {
    }

    public IQueryable<InventoryItem> GetInventoryQuery()
    {
        return _context.InventoryItems
            .Where(i => !i.IsDeleted)
            .AsQueryable();
    }
    public override Task<InventoryItem?> Get(int inventoryId)
    {
       return _context.InventoryItems
            .Where(i => !i.IsDeleted && i.Id == inventoryId)
            .FirstOrDefaultAsync();
    }
    public async Task<InventoryItem?> GetByName(string itemName)
    {
        var normalizedName =itemName.Trim().ToUpper();

        return await _context.InventoryItems.FirstOrDefaultAsync(i =>!i.IsDeleted &&i.Name.ToUpper() ==normalizedName);
    }
}
