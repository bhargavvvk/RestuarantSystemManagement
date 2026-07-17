using Microsoft.EntityFrameworkCore;
using RestaurantAPI.Contexts;
using RestaurantAPI.Models;
using RestaurantAPI.RepositoryInterfaces;

namespace RestaurantAPI.Repositories;

public class MenuItemIngredientRepository : AbstractRepository<int, MenuItemIngredient, RestaurantContext>, IMenuItemIngredientRepository
{
    public MenuItemIngredientRepository(RestaurantContext context) : base(context) { }

    public async Task<ICollection<MenuItemIngredient>> GetByMenuItemId(int menuItemId)
    {
        return await _context.MenuItemIngredients
            .Include(mi => mi.Ingredient)
            .Where(mi => mi.MenuItemId == menuItemId)
            .ToListAsync();
    }

    public async Task DeleteAllForMenuItem(int menuItemId)
    {
        var existing = await _context.MenuItemIngredients
            .Where(mi => mi.MenuItemId == menuItemId)
            .ToListAsync();

        _context.MenuItemIngredients.RemoveRange(existing);
    }
}
