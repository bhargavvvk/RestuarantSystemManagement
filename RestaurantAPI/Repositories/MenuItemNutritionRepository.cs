using Microsoft.EntityFrameworkCore;
using RestaurantAPI.Contexts;
using RestaurantAPI.Models;
using RestaurantAPI.RepositoryInterfaces;

namespace RestaurantAPI.Repositories;

public class MenuItemNutritionRepository : AbstractRepository<int, MenuItemNutrition, RestaurantContext>, IMenuItemNutritionRepository
{
    public MenuItemNutritionRepository(RestaurantContext context) : base(context) { }

    public async Task<MenuItemNutrition?> GetByMenuItemId(int menuItemId)
    {
        return await _context.MenuItemNutritions
            .FirstOrDefaultAsync(n => n.MenuItemId == menuItemId);
    }
}
