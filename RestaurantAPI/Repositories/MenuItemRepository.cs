using Microsoft.EntityFrameworkCore;
using RestaurantAPI.Contexts;
using RestaurantAPI.Models;
using RestaurantAPI.RepositoryInterfaces;

namespace RestaurantAPI.Repositories;

public class MenuItemRepository:AbstractRepository<int,MenuItem>,IMenuItemRepository
{
    public MenuItemRepository(RestaurantContext context):base(context)
    {

    }
    public override async Task<ICollection<MenuItem>> GetAll()
    {
        return await _context.MenuItems.Include(m => m.Category)
            .Where(m => !m.IsDeleted)
            .ToListAsync();
    }
    public async Task<MenuItem?> GetByName(
    string name)
    {
        var normalizedName =
            name.Trim().ToUpper();

        return await _context.MenuItems
            .FirstOrDefaultAsync(m =>
                !m.IsDeleted &&
                m.Name.Trim().ToUpper() ==
                normalizedName);
    }
    public override async Task<MenuItem?> Get(int menuId)
    {
        return await _context.MenuItems
            .FirstOrDefaultAsync(x => x.Id == menuId && !x.IsDeleted);
    }
    public IQueryable<MenuItem> GetMenuQuery()
{
    return _context.MenuItems
        .Include(m => m.Category)
        .Where(m => !m.IsDeleted);
}
}
