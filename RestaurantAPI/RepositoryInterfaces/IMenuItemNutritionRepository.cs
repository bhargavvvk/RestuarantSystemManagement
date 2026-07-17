using RestaurantAPI.Models;

namespace RestaurantAPI.RepositoryInterfaces;

public interface IMenuItemNutritionRepository : IRepository<int, MenuItemNutrition>
{
    Task<MenuItemNutrition?> GetByMenuItemId(int menuItemId);
}
